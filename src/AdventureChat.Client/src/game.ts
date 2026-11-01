import { DEFAULT_ROOM, EXIT_TRIGGER_DISTANCE } from './constants.js';
import { InputHandler } from './input/input-handler.js';
import { RoomNetworkClient } from './network/room-client.js';
import { RoomRenderer } from './rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from './simulation/movement.js';
import { RoomState } from './state/room-state.js';
import type {
  Character,
  ChatMessageDto,
  Point,
  RoomDefinition,
  RoomExit,
  RoomSnapshotDto,
  SendChatMessageResultDto,
} from './types.js';

export { DEFAULT_ROOM } from './constants.js';

export class AdventureGame {
  public readonly state: RoomState;
  public readonly networkClient: RoomNetworkClient;
  public readonly renderer: RoomRenderer;
  public readonly inputHandler: InputHandler;

  public onChatMessage?: (message: ChatMessageDto) => void;
  public onConnectionStatusChanged?: (status: 'connected' | 'connecting' | 'disconnected') => void;
  public onRoomChanged?: (room: RoomDefinition) => void;
  public onActionChanged?: (actionText: string) => void;

  private lastTimestamp = 0;
  private isRunning = false;
  private animationFrameId: number | null = null;
  private currentRequestId = 0;
  private exitRequestInFlight = false;
  private exitRetries = 0;
  private nextExitAttempt = 0;
  private changingRoom = false;
  private pendingEvents: Array<() => void> = [];

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    initialRoom: RoomDefinition = DEFAULT_ROOM,
    playerName?: string
  ) {
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
      onUserJoined: (dto) => this.dispatchRoomEvent(() => { if (dto.roomId === this.state.room.id) this.state.addOrUpdateRemotePlayer(dto.player); }),
      onUserLeft: (dto) => this.dispatchRoomEvent(() => { if (dto.roomId === this.state.room.id) this.state.removeRemotePlayer(dto.sessionId); }),
      onPlayerMoved: (dto) => this.dispatchRoomEvent(() => this.state.handlePlayerMoved(dto)),
      onChatMessageReceived: (dto) => this.dispatchRoomEvent(() => this.state.handleChatMessage(dto)),
      onConnectionStatusChanged: (status) => this.onConnectionStatusChanged?.(status),
      onReconnected: async () => {
        await this.joinRoom();
      },
    });

    this.inputHandler = new InputHandler(
      this.canvas,
      {
        onHoverExitChanged: (exit, actionText) => {
          this.renderer.setHoveredExit(exit);
          this.onActionChanged?.(actionText);
        },
        onNavigateToExit: async (exit, isImmediate) => {
          if (isImmediate) {
            this.exitRetries = 0;
            this.nextExitAttempt = 0;
            this.state.pendingExit = exit;
          } else {
            this.exitRetries = 0;
            this.nextExitAttempt = 0;
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
      },
      () => this.state.room,
      () => this.state.character.position
    );
  }

  // --- Properties for backwards compatibility with tests and callers ---
  public get room(): RoomDefinition {
    return this.state.room;
  }
  public set room(val: RoomDefinition) {
    this.state.room = val;
    this.renderer.setRoom(val);
  }

  public get character(): Character {
    return this.state.character;
  }

  public get remotePlayers(): Map<string, Character> {
    return this.state.remotePlayers;
  }

  public get chatMessages(): ChatMessageDto[] {
    return this.state.chatMessages;
  }

  public get sessionId(): string {
    return this.state.sessionId;
  }
  public set sessionId(val: string) {
    this.state.setSessionId(val);
  }

  public get playerName(): string {
    return this.state.playerName;
  }
  public set playerName(val: string) {
    this.state.playerName = val;
    this.state.character.name = val;
  }

  public get pendingExit(): RoomExit | null {
    return this.state.pendingExit;
  }
  public set pendingExit(val: RoomExit | null) {
    this.state.pendingExit = val;
  }

  public get connectionStatus(): 'connected' | 'connecting' | 'disconnected' {
    return this.networkClient.connectionStatus;
  }

  public setConnectionStatus(status: 'connected' | 'connecting' | 'disconnected'): void {
    this.networkClient.setStatus(status);
  }

  // --- Lifecycle Methods ---
  public async start(): Promise<void> {
    if (this.isRunning) return;
    this.isRunning = true;
    this.lastTimestamp = performance.now();
    this.loop = this.loop.bind(this);
    this.animationFrameId = requestAnimationFrame(this.loop);

    const connected = await this.networkClient.connect();
    if (connected) {
      await this.joinRoom();
    }
  }

  public stop(): void {
    this.isRunning = false;
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
    this.networkClient.disconnect();
  }

  private async joinRoom(): Promise<void> {
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

  public async changeRoom(exitId: string): Promise<boolean> {
    if (this.changingRoom) return false;
    this.changingRoom = true;
    try {
      const result = await this.networkClient.changeRoom(exitId);
      if (result.success && result.snapshot) {
        this.applyRoomSnapshot(result.snapshot);
        return true;
      }
      return false;
    } finally {
      this.changingRoom = false;
      for (const event of this.pendingEvents.splice(0)) event();
    }
  }

  private dispatchRoomEvent(event: () => void): void {
    if (this.changingRoom) this.pendingEvents.push(event);
    else event();
  }

  public applyRoomSnapshot(snapshot: RoomSnapshotDto): void {
    this.state.applySnapshot(snapshot);
  }

  public async sendChatMessage(text: string): Promise<SendChatMessageResultDto> {
    return this.networkClient.sendChatMessage(text);
  }

  // --- Movement & Target Navigation ---
  public async setTarget(
    target: Point
  ): Promise<{ accepted: boolean; waypoints?: Point[]; error?: string }> {
    const requestId = ++this.currentRequestId;
    const startPos = { ...this.state.character.position };

    // 1. Real-time SignalR movement if connected
    if (this.networkClient.connectionStatus === 'connected') {
      const result = await this.networkClient.requestMove(startPos, target);
      if (result.success) {
        this.renderer.addClickMarker(target.x, target.y, 'valid');
        return { accepted: true };
      } else {
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

  public applyPath(waypoints: Point[]): void {
    assignPathToCharacter(this.state.character, waypoints);
  }

  // --- Input & Hit Testing Delegation ---
  public screenToLogical(clientX: number, clientY: number): Point {
    return this.inputHandler.screenToLogical(clientX, clientY);
  }

  public findClickedExit(point: Point): RoomExit | null {
    return this.inputHandler.findClickedExit(point);
  }

  public handleMouseMove(clientX: number, clientY: number): void {
    this.inputHandler.handleMouseMove(clientX, clientY);
  }

  public handleMouseLeave(): void {
    this.inputHandler.handleMouseLeave();
  }

  public async handleCanvasClick(
    clientX: number,
    clientY: number
  ): Promise<{ accepted: boolean; point: Point; waypoints?: Point[] }> {
    const res = await this.inputHandler.handleCanvasClick(clientX, clientY);
    return { accepted: true, point: res.point };
  }

  // --- Game Loop & Simulation Update ---
  public update(deltaSeconds: number): void {
    if (deltaSeconds <= 0) return;

    const dt = deltaSeconds;

    updateCharacterMovement(this.state.character, dt);
    for (const remote of this.state.remotePlayers.values()) {
      updateCharacterMovement(remote, dt);
    }

    // Check if local character finished walking to an exit
    if (this.state.pendingExit && this.state.character.state === 'idle') {
      const dist = Math.hypot(
        this.state.character.position.x - this.state.pendingExit.position.x,
        this.state.character.position.y - this.state.pendingExit.position.y
      );
      if (dist <= (this.state.pendingExit.radius ?? EXIT_TRIGGER_DISTANCE) + 5 && !this.exitRequestInFlight && performance.now() >= this.nextExitAttempt) {
        const exitToTrigger = this.state.pendingExit;
        this.exitRequestInFlight = true;
        void this.changeRoom(exitToTrigger.id).then((success) => {
          if (!success && this.state.pendingExit === exitToTrigger) {
            this.exitRetries++;
            this.nextExitAttempt = performance.now() + 150;
            if (this.exitRetries >= 5) this.state.pendingExit = null;
          }
        }).finally(() => { this.exitRequestInFlight = false; });
      }
    }
  }

  private loop(timestamp: number): void {
    if (!this.isRunning) return;

    const deltaSeconds = (timestamp - this.lastTimestamp) / 1000;
    this.lastTimestamp = timestamp;

    this.update(deltaSeconds);
    this.renderer.render(
      this.state.character,
      Array.from(this.state.remotePlayers.values()),
      this.networkClient.connectionStatus
    );

    this.animationFrameId = requestAnimationFrame(this.loop);
  }
}
