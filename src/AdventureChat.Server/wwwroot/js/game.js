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
    spawns: [
        { id: 'entry', position: { x: 120, y: 300 } },
        { id: 'from-garden', position: { x: 540, y: 300 } },
    ],
    exits: [
        { id: 'to-garden', position: { x: 580, y: 300 }, radius: 30, targetRoomId: 'garden', targetSpawnId: 'from-lounge' },
    ],
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
    chatMessages = [];
    renderer;
    connectionStatus = 'disconnected';
    sessionId = 'local-player';
    playerName;
    pendingExit = null;
    onChatMessage;
    onConnectionStatusChanged;
    onRoomChanged;
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
            this.hubConnection.on('ChatMessageReceived', (dto) => {
                this.handleChatMessageReceived(dto);
            });
            this.hubConnection.onreconnecting(() => {
                this.setConnectionStatus('connecting');
            });
            this.hubConnection.onreconnected(async () => {
                await this.joinRoom();
            });
            this.hubConnection.onclose(() => {
                this.setConnectionStatus('disconnected');
            });
            await this.hubConnection.start();
            await this.joinRoom();
        }
        catch (e) {
            console.error('Failed to initialize SignalR connection:', e);
            this.setConnectionStatus('disconnected');
        }
    }
    setConnectionStatus(status) {
        this.connectionStatus = status;
        this.onConnectionStatusChanged?.(status);
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
                this.applyRoomSnapshot(result.snapshot);
                this.chatMessages.length = 0;
                if (result.snapshot.recentMessages) {
                    for (const msg of result.snapshot.recentMessages) {
                        this.chatMessages.push(msg);
                        this.onChatMessage?.(msg);
                    }
                }
                this.setConnectionStatus('connected');
            }
            else {
                console.error('JoinRoom rejected:', result.error);
                this.setConnectionStatus('disconnected');
            }
        }
        catch (e) {
            console.error('Error invoking JoinRoom:', e);
            this.setConnectionStatus('disconnected');
        }
    }
    async changeRoom(targetRoomId, targetSpawnId) {
        if (!this.hubConnection || this.connectionStatus !== 'connected') {
            return false;
        }
        try {
            const result = await this.hubConnection.invoke('ChangeRoom', {
                targetRoomId,
                targetSpawnId: targetSpawnId || null,
            });
            if (result.success && result.snapshot) {
                this.applyRoomSnapshot(result.snapshot);
                return true;
            }
            else {
                console.error('ChangeRoom failed:', result.error);
                return false;
            }
        }
        catch (e) {
            console.error('Error changing room:', e);
            return false;
        }
    }
    applyRoomSnapshot(snapshot) {
        if (snapshot.room) {
            this.room = snapshot.room;
            this.renderer.setRoom(snapshot.room);
        }
        this.remotePlayers.clear();
        for (const p of snapshot.players) {
            if (p.sessionId === this.sessionId) {
                this.character.name = p.displayName;
                this.playerName = p.displayName;
                this.character.position = { ...p.position };
                this.character.facing = p.direction;
                this.character.state = p.state;
                this.character.target = null;
                this.character.waypoints = [];
            }
            else {
                this.handleUserJoined(p);
            }
        }
        this.pendingExit = null;
        this.onRoomChanged?.(this.room);
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
    handleChatMessageReceived(msg) {
        this.chatMessages.push(msg);
        const bubble = {
            text: msg.text,
            startTime: performance.now(),
            duration: 6000,
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
    async sendChatMessage(text) {
        if (!this.hubConnection || this.connectionStatus !== 'connected') {
            return { success: false, error: 'Nicht mit dem Server verbunden.', message: null };
        }
        try {
            const result = await this.hubConnection.invoke('SendChatMessage', {
                text,
            });
            return result;
        }
        catch (e) {
            return { success: false, error: String(e), message: null };
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
    findClickedExit(point) {
        if (!this.room.exits || this.room.exits.length === 0)
            return null;
        for (const exit of this.room.exits) {
            const dist = Math.hypot(point.x - exit.position.x, point.y - exit.position.y);
            if (dist <= (exit.radius || 30)) {
                return exit;
            }
        }
        return null;
    }
    async handleCanvasClick(clientX, clientY) {
        const logical = this.screenToLogical(clientX, clientY);
        const clickedExit = this.findClickedExit(logical);
        if (clickedExit) {
            this.pendingExit = clickedExit;
            const result = await this.setTarget(clickedExit.position);
            if (result.waypoints) {
                return { accepted: result.accepted, point: logical, waypoints: result.waypoints };
            }
            return { accepted: result.accepted, point: logical };
        }
        else {
            this.pendingExit = null;
            const result = await this.setTarget(logical);
            if (result.waypoints) {
                return { accepted: result.accepted, point: logical, waypoints: result.waypoints };
            }
            return { accepted: result.accepted, point: logical };
        }
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
        if (char.id === this.sessionId && this.pendingExit && char.state === 'idle') {
            const dist = Math.hypot(char.position.x - this.pendingExit.position.x, char.position.y - this.pendingExit.position.y);
            if (dist <= 45) {
                const exitToTrigger = this.pendingExit;
                this.pendingExit = null;
                void this.changeRoom(exitToTrigger.targetRoomId, exitToTrigger.targetSpawnId);
            }
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