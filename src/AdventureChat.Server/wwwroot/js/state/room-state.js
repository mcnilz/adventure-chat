import { DEFAULT_ROOM, DEFAULT_SPEED, SPEECH_BUBBLE_DURATION } from '../constants.js';
import { assignPathToCharacter } from '../simulation/movement.js';
export class RoomState {
    room;
    sessionId = 'local-player';
    playerName;
    character;
    remotePlayers = new Map();
    chatMessages = [];
    pendingExit = null;
    onRoomChanged;
    onChatMessage;
    constructor(initialRoom = DEFAULT_ROOM, playerName) {
        this.room = initialRoom;
        const urlParams = typeof window !== 'undefined' ? new URLSearchParams(window.location.search) : null;
        this.playerName = playerName || urlParams?.get('name') || `Player-${Math.floor(1000 + Math.random() * 9000)}`;
        this.character = {
            id: this.sessionId,
            name: this.playerName,
            position: { ...this.room.spawnPoint },
            target: null,
            waypoints: [],
            state: 'idle',
            facing: 'right',
            speed: DEFAULT_SPEED,
            walkFrame: 0,
            frameTimer: 0,
        };
    }
    setSessionId(id) {
        this.sessionId = id;
        this.character.id = id;
    }
    applySnapshot(snapshot) {
        if (snapshot.room) {
            this.room = snapshot.room;
        }
        if (snapshot.yourSessionId) {
            this.setSessionId(snapshot.yourSessionId);
        }
        this.remotePlayers.clear();
        for (const player of snapshot.players) {
            if (player.sessionId === this.sessionId) {
                this.character.name = player.displayName;
                this.playerName = player.displayName;
                this.character.position = { ...player.position };
                this.character.facing = player.direction;
                this.character.state = player.state;
                this.character.target = null;
                this.character.waypoints = [];
            }
            else {
                this.addOrUpdateRemotePlayer(player);
            }
        }
        this.pendingExit = null;
        this.onRoomChanged?.(this.room);
    }
    addOrUpdateRemotePlayer(player) {
        if (player.sessionId === this.sessionId)
            return;
        const char = {
            id: player.sessionId,
            name: player.displayName,
            position: { ...player.position },
            target: player.target ? { ...player.target } : null,
            waypoints: [],
            state: player.state,
            facing: player.direction,
            speed: DEFAULT_SPEED,
            walkFrame: 0,
            frameTimer: 0,
        };
        if (player.waypoints && player.waypoints.length > 0) {
            assignPathToCharacter(char, player.waypoints);
        }
        this.remotePlayers.set(player.sessionId, char);
    }
    removeRemotePlayer(sessionId) {
        this.remotePlayers.delete(sessionId);
    }
    handlePlayerMoved(dto) {
        const waypoints = dto.waypoints;
        if (dto.sessionId === this.sessionId) {
            assignPathToCharacter(this.character, waypoints);
        }
        else {
            const remote = this.remotePlayers.get(dto.sessionId);
            if (remote) {
                assignPathToCharacter(remote, waypoints);
            }
        }
    }
    handleChatMessage(msg) {
        this.chatMessages.push(msg);
        const bubble = {
            text: msg.text,
            startTime: performance.now(),
            duration: SPEECH_BUBBLE_DURATION,
        };
        if (msg.sessionId === this.sessionId) {
            this.character.speechBubble = bubble;
        }
        else {
            const remote = this.remotePlayers.get(msg.sessionId);
            if (remote) {
                remote.speechBubble = bubble;
            }
        }
        this.onChatMessage?.(msg);
    }
    getAllCharacters() {
        return [this.character, ...this.remotePlayers.values()];
    }
}
//# sourceMappingURL=room-state.js.map