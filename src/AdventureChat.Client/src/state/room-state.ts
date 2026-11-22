import { DEFAULT_ROOM, DEFAULT_SPEED, SPEECH_BUBBLE_DURATION } from '../constants.js';
import { applyActorAnimation, assignPathToCharacter } from '../simulation/movement.js';
import { applyObjectAppearances } from './object-appearance.js';
import { NpcCast } from './npc-cast.js';
import { expireSpeechBubble } from './speech-bubbles.js';
import type {
  Character,
  ChatMessageDto,
  PlayerDto,
  PlayerMovedDto,
  Point,
  RoomDefinition,
  RoomExit,
  RoomSnapshotDto,
  ObjectStateDto,
  ObjectStatesChangedDto,
  NpcMovementDto,
} from '../types.js';

export class RoomState {
  public room: RoomDefinition;
  public sessionId = 'local-player';
  public playerName: string;
  public readonly character: Character;
  public readonly remotePlayers: Map<string, Character> = new Map();
  public readonly npcCast = new NpcCast();
  public readonly chatMessages: ChatMessageDto[] = [];
  public pendingExit: RoomExit | null = null;
  private readonly movementRevisions = new Map<string, number>();
  public objectRevision = 0;
  public readonly objectStates = new Map<string, string>();
  private lastNavigationStopRevision = -1;

  public onRoomChanged?: (room: RoomDefinition) => void;
  public onChatMessage?: (message: ChatMessageDto) => void;
  public onObjectsChanged?: () => void;
  public onNavigationStopped?: () => void;

  public constructor(initialRoom: RoomDefinition = DEFAULT_ROOM, playerName?: string) {
    this.room = initialRoom;

    const urlParams = typeof window !== 'undefined' ? new URLSearchParams(window.location.search) : null;
    this.playerName = playerName || urlParams?.get('name') || `Player-${Math.floor(1000 + Math.random() * 9000)}`;
    const roomParam = urlParams?.get('room');
    if (roomParam) {
      this.room = { ...this.room, id: roomParam };
    }
    this.npcCast.setRoom(this.room);

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

  public expireSpeechBubbles(now: number): void {
    expireSpeechBubble(this.character, now);
    for (const player of this.remotePlayers.values()) expireSpeechBubble(player, now);
  }

  public setSessionId(id: string): void {
    if (id !== this.sessionId) this.lastNavigationStopRevision = -1;
    this.sessionId = id;
    this.character.id = id;
  }

  public applySnapshot(snapshot: RoomSnapshotDto): void {
    if (snapshot.roomId !== this.room.id || snapshot.yourSessionId !== this.sessionId)
      this.character.speechBubble = null;
    if (snapshot.room) {
      this.room = snapshot.room;
      this.npcCast.setRoom(this.room);
    }
    this.objectRevision = snapshot.objectRevision ?? 0;
    this.applyObjectStates(snapshot.objectStates ?? []);

    if (snapshot.yourSessionId) {
      this.setSessionId(snapshot.yourSessionId);
    }

    this.character.speed = this.room.walkSpeed ?? DEFAULT_SPEED;
    this.remotePlayers.clear();
    this.movementRevisions.clear();
    for (const player of snapshot.players) {
      this.movementRevisions.set(player.sessionId, player.movementRevision);
      if (player.sessionId === this.sessionId) {
        this.character.characterId = player.characterId;
        this.character.name = player.displayName;
        this.playerName = player.displayName;
        this.character.position = { ...player.position };
        this.character.facing = player.direction;
        this.character.state = player.state;
        this.character.target = null;
        this.character.waypoints = [];
        applyActorAnimation(this.character, player);
        if (player.waypoints.length > 1) assignPathToCharacter(this.character, player.waypoints as Point[]);
      } else {
        this.addOrUpdateRemotePlayer(player);
      }
    }

    this.pendingExit = null;
    this.onRoomChanged?.(this.room);
  }

  public handleObjectStates(dto: ObjectStatesChangedDto): void {
    if (dto.roomId !== this.room.id || dto.revision <= this.objectRevision) return;
    this.objectRevision = dto.revision;
    this.applyObjectStates(dto.states);
    if (dto.geometry) this.room = { ...this.room, geometry: dto.geometry };
    if (dto.exits) this.room = { ...this.room, exits: dto.exits };
    if (dto.npcMovement) this.handleNpcMovement(dto.npcMovement);
    for (const player of dto.players ?? []) {
      this.handlePlayerMoved({ roomId: dto.roomId, sessionId: player.sessionId, revision: player.movementRevision,
        target: player.target ?? player.position, waypoints: player.waypoints.length > 0 ? player.waypoints : [player.position] }, player);
    }
    const stoppedPlayer = dto.players?.find(p => p.sessionId === this.sessionId);
    if (dto.stoppedSessions?.includes(this.sessionId) && stoppedPlayer &&
      stoppedPlayer.movementRevision === this.movementRevisions.get(this.sessionId) &&
      stoppedPlayer.movementRevision > this.lastNavigationStopRevision) {
      this.lastNavigationStopRevision = stoppedPlayer.movementRevision;
      this.onNavigationStopped?.();
      this.pendingExit = null;
      this.character.speechBubble = { text: 'Der Weg ist jetzt versperrt. Ich bleibe hier stehen.',
        startTime: performance.now(), duration: SPEECH_BUBBLE_DURATION };
    }
    this.onObjectsChanged?.();
  }

  public handleNpcMovement({ roomId, npcs, sample }: NpcMovementDto): void {
    if (roomId !== this.room.id || sample <= (this.room.npcSample ?? 0)) return;
    this.room = { ...this.room, npcs, npcSample: sample };
    this.npcCast.update(npcs);
  }

  private applyObjectStates(states: ReadonlyArray<ObjectStateDto>): void {
    this.objectStates.clear();
    for (const state of states) this.objectStates.set(state.objectId, state.stateId);
    this.room = { ...this.room, objects: applyObjectAppearances(this.room.objects ?? [], states) };
  }

  public addOrUpdateRemotePlayer(player: PlayerDto): void {
    if (player.sessionId === this.sessionId) return;
    if (this.remotePlayers.has(player.sessionId) && (this.movementRevisions.get(player.sessionId) ?? -1) >= player.movementRevision) return;
    this.movementRevisions.set(player.sessionId, player.movementRevision);

    const char: Character = {
      id: player.sessionId,
      characterId: player.characterId,
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
    applyActorAnimation(char, player);

    this.remotePlayers.set(player.sessionId, char);
  }

  public removeRemotePlayer(sessionId: string): void {
    this.remotePlayers.delete(sessionId);
  }

  public handlePlayerMoved(dto: PlayerMovedDto, player?: PlayerDto): void {
    const currentRevision = this.movementRevisions.get(dto.sessionId) ?? -1;
    if (dto.roomId !== this.room.id || dto.revision < currentRevision || (!player && dto.revision === currentRevision)) return;
    const character = dto.sessionId === this.sessionId ? this.character : this.remotePlayers.get(dto.sessionId);
    if (!character) return;
    this.movementRevisions.set(dto.sessionId, dto.revision);
    const waypoints = dto.waypoints as Point[];
    if (waypoints[0]) character.position = { ...waypoints[0] };
    assignPathToCharacter(character, waypoints);
    if (player) applyActorAnimation(character, player);
    else character.actorAnimation = null;
  }

  public handleChatMessage(msg: ChatMessageDto): void {
    if (msg.roomId !== this.room.id) return;
    if (this.chatMessages.some(message => message.messageId === msg.messageId)) return;
    this.chatMessages.push(msg);

    const bubble = {
      text: msg.text,
      spoken: true,
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
