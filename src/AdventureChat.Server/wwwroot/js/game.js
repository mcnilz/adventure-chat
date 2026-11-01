import { DEFAULT_ROOM, EXIT_TRIGGER_DISTANCE } from './constants.js';
import { InputHandler } from './input/input-handler.js';
import { RoomNetworkClient } from './network/room-client.js';
import { RoomRenderer } from './rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from './simulation/movement.js';
import { RoomState } from './state/room-state.js';
export { DEFAULT_ROOM } from './constants.js';
export class AdventureGame {
    canvas;
    state;
    networkClient;
    renderer;
    inputHandler;
    onChatMessage;
    onConnectionStatusChanged;
    onRoomChanged;
    onActionChanged;
    lastTimestamp = 0;
    isRunning = false;
    animationFrameId = null;
    currentRequestId = 0;
    constructor(canvas, initialRoom = DEFAULT_ROOM, playerName) {
        this.canvas = canvas;
        this.state = new RoomState(initialRoom, playerName);
        this.renderer = new RoomRenderer(this.canvas, this.state.room);
        this.state.onRoomChanged = (room) => {
            this.renderer.setRoom(room);
            this.onRoomChanged?.(room);
        };
        this.state.onChatMessage = (msg) => {
            this.onChatMessage?.(msg);
        };
        this.networkClient = new RoomNetworkClient({
            onUserJoined: (dto) => this.state.addOrUpdateRemotePlayer(dto.player),
            onUserLeft: (dto) => this.state.removeRemotePlayer(dto.sessionId),
            onPlayerMoved: (dto) => this.state.handlePlayerMoved(dto),
            onChatMessageReceived: (dto) => this.state.handleChatMessage(dto),
            onConnectionStatusChanged: (status) => this.onConnectionStatusChanged?.(status),
            onReconnected: async () => {
                await this.joinRoom();
            },
        });
        this.inputHandler = new InputHandler(this.canvas, {
            onHoverExitChanged: (exit, actionText) => {
                this.renderer.setHoveredExit(exit);
                this.onActionChanged?.(actionText);
            },
            onNavigateToExit: async (exit, isImmediate) => {
                if (isImmediate) {
                    this.state.pendingExit = null;
                    void this.changeRoom(exit.targetRoomId, exit.targetSpawnId);
                }
                else {
                    this.state.pendingExit = exit;
                    await this.setTarget(exit.position);
                }
            },
            onNavigateToPoint: async (point) => {
                this.state.pendingExit = null;
                await this.setTarget(point);
            },
            onToggleDebug: () => {
                this.renderer.toggleDebugOverlay();
            },
        }, () => this.state.room, () => this.state.character.position);
    }
    // --- Properties for backwards compatibility with tests and callers ---
    get room() {
        return this.state.room;
    }
    set room(val) {
        this.state.room = val;
        this.renderer.setRoom(val);
    }
    get character() {
        return this.state.character;
    }
    get remotePlayers() {
        return this.state.remotePlayers;
    }
    get chatMessages() {
        return this.state.chatMessages;
    }
    get sessionId() {
        return this.state.sessionId;
    }
    set sessionId(val) {
        this.state.setSessionId(val);
    }
    get playerName() {
        return this.state.playerName;
    }
    set playerName(val) {
        this.state.playerName = val;
        this.state.character.name = val;
    }
    get pendingExit() {
        return this.state.pendingExit;
    }
    set pendingExit(val) {
        this.state.pendingExit = val;
    }
    get connectionStatus() {
        return this.networkClient.connectionStatus;
    }
    setConnectionStatus(status) {
        this.networkClient.setStatus(status);
    }
    // --- Lifecycle Methods ---
    async start() {
        if (this.isRunning)
            return;
        this.isRunning = true;
        this.lastTimestamp = performance.now();
        this.loop = this.loop.bind(this);
        this.animationFrameId = requestAnimationFrame(this.loop);
        const connected = await this.networkClient.connect();
        if (connected) {
            await this.joinRoom();
        }
    }
    stop() {
        this.isRunning = false;
        if (this.animationFrameId !== null) {
            cancelAnimationFrame(this.animationFrameId);
            this.animationFrameId = null;
        }
        this.networkClient.disconnect();
    }
    async joinRoom() {
        const result = await this.networkClient.joinRoom(this.state.room.id, this.state.playerName);
        if (result.success && result.snapshot) {
            this.applyRoomSnapshot(result.snapshot);
            this.state.chatMessages.length = 0;
            if (result.snapshot.recentMessages) {
                for (const msg of result.snapshot.recentMessages) {
                    this.state.chatMessages.push(msg);
                    this.onChatMessage?.(msg);
                }
            }
        }
    }
    async changeRoom(targetRoomId, targetSpawnId) {
        const result = await this.networkClient.changeRoom(targetRoomId, targetSpawnId);
        if (result.success && result.snapshot) {
            this.applyRoomSnapshot(result.snapshot);
            return true;
        }
        return false;
    }
    applyRoomSnapshot(snapshot) {
        this.state.applySnapshot(snapshot);
    }
    async sendChatMessage(text) {
        return this.networkClient.sendChatMessage(text);
    }
    // --- Movement & Target Navigation ---
    async setTarget(target) {
        const requestId = ++this.currentRequestId;
        const startPos = { ...this.state.character.position };
        // 1. Real-time SignalR movement if connected
        if (this.networkClient.connectionStatus === 'connected') {
            const result = await this.networkClient.requestMove(startPos, target);
            if (result.success) {
                this.renderer.addClickMarker(target.x, target.y, 'valid');
                return { accepted: true };
            }
            else {
                this.renderer.addClickMarker(target.x, target.y, 'invalid');
                return { accepted: false, error: result.error || 'Server rejected target' };
            }
        }
        // 2. HTTP Fallback Endpoint
        const fallback = await this.networkClient.calculatePathFallback(this.state.room.id, startPos, target);
        if (!fallback.success || !fallback.waypoints) {
            this.renderer.addClickMarker(target.x, target.y, 'invalid');
            return { accepted: false, error: fallback.error || 'No path found' };
        }
        if (requestId !== this.currentRequestId) {
            return { accepted: false, error: 'Superseded by newer target' };
        }
        this.renderer.addClickMarker(target.x, target.y, 'valid');
        this.applyPath(fallback.waypoints);
        return { accepted: true, waypoints: fallback.waypoints };
    }
    applyPath(waypoints) {
        assignPathToCharacter(this.state.character, waypoints);
    }
    // --- Input & Hit Testing Delegation ---
    screenToLogical(clientX, clientY) {
        return this.inputHandler.screenToLogical(clientX, clientY);
    }
    findClickedExit(point) {
        return this.inputHandler.findClickedExit(point);
    }
    handleMouseMove(clientX, clientY) {
        this.inputHandler.handleMouseMove(clientX, clientY);
    }
    handleMouseLeave() {
        this.inputHandler.handleMouseLeave();
    }
    async handleCanvasClick(clientX, clientY) {
        const res = await this.inputHandler.handleCanvasClick(clientX, clientY);
        return { accepted: true, point: res.point };
    }
    // --- Game Loop & Simulation Update ---
    update(deltaSeconds) {
        if (deltaSeconds <= 0)
            return;
        const dt = Math.min(deltaSeconds, 0.1);
        updateCharacterMovement(this.state.character, dt);
        for (const remote of this.state.remotePlayers.values()) {
            updateCharacterMovement(remote, dt);
        }
        // Check if local character finished walking to an exit
        if (this.state.pendingExit && this.state.character.state === 'idle') {
            const dist = Math.hypot(this.state.character.position.x - this.state.pendingExit.position.x, this.state.character.position.y - this.state.pendingExit.position.y);
            if (dist <= EXIT_TRIGGER_DISTANCE) {
                const exitToTrigger = this.state.pendingExit;
                this.state.pendingExit = null;
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
        this.renderer.render(this.state.character, Array.from(this.state.remotePlayers.values()), this.networkClient.connectionStatus);
        this.animationFrameId = requestAnimationFrame(this.loop);
    }
}
//# sourceMappingURL=game.js.map