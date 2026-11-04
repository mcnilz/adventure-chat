import { DEFAULT_ROOM, DEFAULT_SPEED, SPEECH_BUBBLE_DURATION } from '../constants.js';
import { assignPathToCharacter } from '../simulation/movement.js';
import type {
  Character,
  ChatMessageDto,
  PlayerDto,
  PlayerMovedDto,
  Point,
  RoomDefinition,
  RoomExit,
  RoomSnapshotDto,
} from '../types.js';

export class RoomState {
  public room: RoomDefinition;
  public sessionId = 'local-player';
  public playerName: string;
  public readonly character: Character;
  public readonly remotePlayers: Map<string, Character> = new Map();
  public readonly chatMessages: ChatMessageDto[] = [];
  public pendingExit: RoomExit | null = null;
  private readonly movementRevisions = new Map<string, number>();

  public onRoomChanged?: (room: RoomDefinition) => void;
  public onChatMessage?: (message: ChatMessageDto) => void;

  public constructor(initialRoom: RoomDefinition = DEFAULT_ROOM, playerName?: string) {
    this.room = initialRoom;

    const urlParams = typeof window !== 'undefined' ? new URLSearchParams(window.location.search) : null;
    this.playerName = playerName || urlParams?.get('name') || `Player-${Math.floor(1000 + Math.random() * 9000)}`;
    const roomParam = urlParams?.get('room');
    if (roomParam) {
      this.room = { ...this.room, id: roomParam };
    }

    this.character = {
      id: this.sessionId,
      name: this.playerName,
      position: { ...this.room.spawnPoint },
      target: null,
      waypoints: [],
      state: 'idle',
      facing: 'right',
      speed: this.room.walkSpeed ?? DEFAULT_SPEED,
      walkFrame: 0,
      frameTimer: 0,
    };
  }

  public setSessionId(id: string): void {
    this.sessionId = id;
    this.character.id = id;
  }

  public applySnapshot(snapshot: RoomSnapshotDto): void {
    if (snapshot.room) {
      this.room = snapshot.room;
    }

    if (snapshot.yourSessionId) {
      this.setSessionId(snapshot.yourSessionId);
    }

    this.character.speed = this.room.walkSpeed ?? DEFAULT_SPEED;
    this.remotePlayers.clear();
    this.movementRevisions.clear();
    for (const player of snapshot.players) {
      this.movementRevisions.set(player.sessionId, player.movementRevision);
      if (player.sessionId === this.sessionId) {
        this.character.name = player.displayName;
        this.playerName = player.displayName;
        this.character.position = { ...player.position };
        this.character.facing = player.direction;
        this.character.state = player.state;
        this.character.target = null;
        this.character.waypoints = [];
        if (player.waypoints.length > 1) assignPathToCharacter(this.character, player.waypoints as Point[]);
      } else {
        this.addOrUpdateRemotePlayer(player);
      }
    }

    this.pendingExit = null;
    this.onRoomChanged?.(this.room);
  }

  public addOrUpdateRemotePlayer(player: PlayerDto): void {
    if (player.sessionId === this.sessionId) return;
    if (this.remotePlayers.has(player.sessionId) && (this.movementRevisions.get(player.sessionId) ?? -1) >= player.movementRevision) return;
    this.movementRevisions.set(player.sessionId, player.movementRevision);

    const char: Character = {
      id: player.sessionId,
      name: player.displayName,
      position: { ...player.position },
      target: player.target ? { ...player.target } : null,
      waypoints: [],
      state: player.state,
      facing: player.direction,
      speed: this.room.walkSpeed ?? DEFAULT_SPEED,
      walkFrame: 0,
      frameTimer: 0,
    };

    if (player.waypoints && player.waypoints.length > 0) {
      assignPathToCharacter(char, player.waypoints as Point[]);
    }

    this.remotePlayers.set(player.sessionId, char);
  }

  public removeRemotePlayer(sessionId: string): void {
    this.remotePlayers.delete(sessionId);
  }

  public handlePlayerMoved(dto: PlayerMovedDto): void {
    if (dto.roomId !== this.room.id || dto.revision <= (this.movementRevisions.get(dto.sessionId) ?? -1)) return;
    this.movementRevisions.set(dto.sessionId, dto.revision);
    const waypoints = dto.waypoints as Point[];
    if (dto.sessionId === this.sessionId) {
      if (waypoints[0]) this.character.position = { ...waypoints[0] };
      assignPathToCharacter(this.character, waypoints);
    } else {
      const remote = this.remotePlayers.get(dto.sessionId);
      if (remote) {
        if (waypoints[0]) remote.position = { ...waypoints[0] };
        assignPathToCharacter(remote, waypoints);
      }
    }
  }

  public handleChatMessage(msg: ChatMessageDto): void {
    if (msg.roomId !== this.room.id) return;
    if (this.chatMessages.some(message => message.messageId === msg.messageId)) return;
    this.chatMessages.push(msg);

    const bubble = {
      text: msg.text,
      startTime: performance.now(),
      duration: SPEECH_BUBBLE_DURATION,
    };

    if (msg.sessionId === this.sessionId) {
      this.character.speechBubble = bubble;
    } else {
      const remote = this.remotePlayers.get(msg.sessionId);
      if (remote) {
        remote.speechBubble = bubble;
      }
    }

    this.onChatMessage?.(msg);
  }

  public getAllCharacters(): Character[] {
    return [this.character, ...this.remotePlayers.values()];
  }
}
