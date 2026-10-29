import { moveTowards } from './math.js';
import { RoomRenderer } from './renderer.js';
export const DEFAULT_ROOM = {
    id: 'lounge',
    name: 'Lounge',
    width: 640,
    height: 360,
    backgroundUrl: 'assets/images/lounge.png',
    spawnPoint: {
        x: 120,
        y: 300,
    },
    geometry: {
        outerBoundary: [
            { x: 40, y: 240 },
            { x: 300, y: 220 },
            { x: 600, y: 240 },
            { x: 600, y: 345 },
            { x: 40, y: 345 },
        ],
        obstacles: [
            [
                { x: 250, y: 255 },
                { x: 390, y: 255 },
                { x: 390, y: 310 },
                { x: 250, y: 310 },
            ],
        ],
    },
};
export class AdventureGame {
    canvas;
    room;
    character;
    remotePlayers = new Map();
    renderer;
    connectionStatus = 'disconnected';
    sessionId = 'local-player';
    playerName;
    lastTimestamp = 0;
    isRunning = false;
    animationFrameId = null;
    currentRequestId = 0;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    hubConnection = null;
    constructor(canvas, room = DEFAULT_ROOM, playerName) {
        this.canvas = canvas;
        this.room = room;
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
            speed: 130, // logical pixels per second
            walkFrame: 0,
            frameTimer: 0,
        };
        this.renderer = new RoomRenderer(this.canvas, this.room);
        this.setupEvents();
    }
    async start() {
        if (this.isRunning)
            return;
        this.isRunning = true;
        this.lastTimestamp = performance.now();
        this.loop = this.loop.bind(this);
        this.animationFrameId = requestAnimationFrame(this.loop);
        await this.initSignalR();
    }
    stop() {
        this.isRunning = false;
        if (this.animationFrameId !== null) {
            cancelAnimationFrame(this.animationFrameId);
            this.animationFrameId = null;
        }
        if (this.hubConnection) {
            try {
                this.hubConnection.stop();
            }
            catch {
                // ignore disconnect errors
            }
        }
    }
    async initSignalR() {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const signalR = typeof window !== 'undefined' ? window.signalR : null;
        if (!signalR) {
            console.warn('SignalR library not found on window. Running in standalone mode.');
            return;
        }
        try {
            this.connectionStatus = 'connecting';
            this.hubConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hub/room')
                .withAutomaticReconnect()
                .build();
            this.hubConnection.on('UserJoined', (dto) => {
                this.handleUserJoined(dto.player);
            });
            this.hubConnection.on('UserLeft', (dto) => {
                this.handleUserLeft(dto.sessionId);
            });
            this.hubConnection.on('PlayerMoved', (dto) => {
                this.handlePlayerMoved(dto);
            });
            this.hubConnection.onreconnecting(() => {
                this.connectionStatus = 'connecting';
            });
            this.hubConnection.onreconnected(async () => {
                await this.joinRoom();
            });
            this.hubConnection.onclose(() => {
                this.connectionStatus = 'disconnected';
            });
            await this.hubConnection.start();
            await this.joinRoom();
        }
        catch (e) {
            console.error('Failed to initialize SignalR connection:', e);
            this.connectionStatus = 'disconnected';
        }
    }
    async joinRoom() {
        if (!this.hubConnection)
            return;
        try {
            const result = await this.hubConnection.invoke('JoinRoom', {
                roomId: this.room.id,
                displayName: this.playerName,
            });
            if (result.success && result.snapshot) {
                this.sessionId = result.snapshot.yourSessionId;
                this.character.id = this.sessionId;
                this.remotePlayers.clear();
                for (const p of result.snapshot.players) {
                    if (p.sessionId === this.sessionId) {
                        this.character.name = p.displayName;
                        this.playerName = p.displayName;
                        this.character.position = { ...p.position };
                        this.character.facing = p.direction;
                        this.character.state = p.state;
                        if (p.waypoints && p.waypoints.length > 0) {
                            this.applyPath(p.waypoints);
                        }
                    }
                    else {
                        this.handleUserJoined(p);
                    }
                }
                this.connectionStatus = 'connected';
            }
            else {
                console.error('JoinRoom rejected:', result.error);
                this.connectionStatus = 'disconnected';
            }
        }
        catch (e) {
            console.error('Error invoking JoinRoom:', e);
            this.connectionStatus = 'disconnected';
        }
    }
    handleUserJoined(player) {
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
            speed: 130,
            walkFrame: 0,
            frameTimer: 0,
        };
        if (player.waypoints && player.waypoints.length > 0) {
            this.applyPathToCharacter(char, player.waypoints);
        }
        this.remotePlayers.set(player.sessionId, char);
    }
    handleUserLeft(sessionId) {
        this.remotePlayers.delete(sessionId);
    }
    handlePlayerMoved(dto) {
        const waypoints = dto.waypoints;
        if (dto.sessionId === this.sessionId) {
            this.applyPath(waypoints);
        }
        else {
            const remote = this.remotePlayers.get(dto.sessionId);
            if (remote) {
                this.applyPathToCharacter(remote, waypoints);
            }
        }
    }
    screenToLogical(clientX, clientY) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.room.width / rect.width;
        const scaleY = this.room.height / rect.height;
        return {
            x: (clientX - rect.left) * scaleX,
            y: (clientY - rect.top) * scaleY,
        };
    }
    async handleCanvasClick(clientX, clientY) {
        const logical = this.screenToLogical(clientX, clientY);
        const result = await this.setTarget(logical);
        if (result.waypoints) {
            return { accepted: result.accepted, point: logical, waypoints: result.waypoints };
        }
        return { accepted: result.accepted, point: logical };
    }
    async setTarget(target) {
        const requestId = ++this.currentRequestId;
        const startPos = { ...this.character.position };
        // 1. Try real-time SignalR movement if connected
        if (this.hubConnection && this.connectionStatus === 'connected') {
            try {
                const result = await this.hubConnection.invoke('RequestMove', {
                    start: { x: startPos.x, y: startPos.y },
                    target: { x: target.x, y: target.y },
                });
                if (result.success) {
                    this.renderer.addClickMarker(target.x, target.y, 'valid');
                    return { accepted: true };
                }
                else {
                    this.renderer.addClickMarker(target.x, target.y, 'invalid');
                    return { accepted: false, error: result.error || 'Server rejected target' };
                }
            }
            catch (e) {
                this.renderer.addClickMarker(target.x, target.y, 'invalid');
                return { accepted: false, error: String(e) };
            }
        }
        // 2. Fallback to HTTP endpoint
        try {
            const response = await fetch(`/api/rooms/${this.room.id}/path`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    start: startPos,
                    target: { x: target.x, y: target.y },
                }),
            });
            if (!response.ok) {
                const errData = await response.json().catch(() => ({}));
                this.renderer.addClickMarker(target.x, target.y, 'invalid');
                return { accepted: false, error: errData.error || 'Server rejected target' };
            }
            const data = await response.json();
            if (!data.success || !Array.isArray(data.waypoints) || data.waypoints.length === 0) {
                this.renderer.addClickMarker(target.x, target.y, 'invalid');
                return { accepted: false, error: data.error || 'No path found' };
            }
            if (requestId !== this.currentRequestId) {
                return { accepted: false, error: 'Superseded by newer target' };
            }
            const waypoints = data.waypoints;
            this.renderer.addClickMarker(target.x, target.y, 'valid');
            this.applyPath(waypoints);
            return { accepted: true, waypoints };
        }
        catch (e) {
            this.renderer.addClickMarker(target.x, target.y, 'invalid');
            return { accepted: false, error: String(e) };
        }
    }
    applyPath(waypoints) {
        this.applyPathToCharacter(this.character, waypoints);
    }
    applyPathToCharacter(char, waypoints) {
        if (waypoints.length <= 1) {
            char.waypoints = [];
            char.target = null;
            char.state = 'idle';
            return;
        }
        char.waypoints = waypoints.slice(1);
        this.moveToNextWaypoint(char);
    }
    moveToNextWaypoint(char) {
        if (char.waypoints.length === 0) {
            char.target = null;
            char.state = 'idle';
            char.walkFrame = 0;
            char.frameTimer = 0;
            return;
        }
        const nextTarget = char.waypoints.shift();
        char.target = nextTarget;
        char.state = 'walk';
        const dx = nextTarget.x - char.position.x;
        const dy = nextTarget.y - char.position.y;
        if (Math.abs(dx) >= Math.abs(dy)) {
            if (dx < -1) {
                char.facing = 'left';
            }
            else if (dx > 1) {
                char.facing = 'right';
            }
        }
        else {
            if (dy < -1) {
                char.facing = 'up';
            }
            else if (dy > 1) {
                char.facing = 'down';
            }
        }
    }
    update(deltaSeconds) {
        if (deltaSeconds <= 0)
            return;
        const dt = Math.min(deltaSeconds, 0.1);
        this.updateCharacter(this.character, dt);
        for (const remote of this.remotePlayers.values()) {
            this.updateCharacter(remote, dt);
        }
    }
    updateCharacter(char, dt) {
        if (char.target && char.state === 'walk') {
            const maxDistance = char.speed * dt;
            const { position, reached } = moveTowards(char.position, char.target, maxDistance);
            char.position = position;
            if (reached) {
                if (char.waypoints.length > 0) {
                    this.moveToNextWaypoint(char);
                }
                else {
                    char.target = null;
                    char.state = 'idle';
                    char.walkFrame = 0;
                    char.frameTimer = 0;
                }
            }
            else {
                char.frameTimer += dt;
                if (char.frameTimer >= 0.12) {
                    char.frameTimer = 0;
                    char.walkFrame = (char.walkFrame + 1) % 4;
                }
            }
        }
        else {
            char.state = 'idle';
            char.walkFrame = 0;
        }
    }
    loop(timestamp) {
        if (!this.isRunning)
            return;
        const deltaSeconds = (timestamp - this.lastTimestamp) / 1000;
        this.lastTimestamp = timestamp;
        this.update(deltaSeconds);
        this.renderer.render(this.character, Array.from(this.remotePlayers.values()), this.connectionStatus);
        this.animationFrameId = requestAnimationFrame(this.loop);
    }
    setupEvents() {
        this.canvas.addEventListener('click', (e) => {
            this.handleCanvasClick(e.clientX, e.clientY);
        });
        window.addEventListener('keydown', (e) => {
            if (e.key === 'd' || e.key === 'D') {
                this.renderer.toggleDebugOverlay();
            }
        });
    }
}
//# sourceMappingURL=game.js.map