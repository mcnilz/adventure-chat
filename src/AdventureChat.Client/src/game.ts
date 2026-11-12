import { DEFAULT_ROOM, EXIT_TRIGGER_DISTANCE, SPEECH_BUBBLE_DURATION } from './constants.js';
import { InputHandler } from './input/input-handler.js';
import { actionsForVerb } from './input/object-action.js';
import { ObjectActionFlow } from './input/object-action-flow.js';
import { nearestUsePoints, objectUsePoints } from './input/object-use-points.js';
import { RoomNetworkClient } from './network/room-client.js';
import { RoomRenderer } from './rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from './simulation/movement.js';
import { RoomState } from './state/room-state.js';
import type {
  Character,
  CharacterCatalog,
  ChatMessageDto,
  Point,
  RoomDefinition,
  RoomExit,
  RoomSnapshotDto,
  SendChatMessageResultDto,
  InteractionVerb,
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
  public onWorldUnavailable?: (message: string) => void;
  public onObjectsChanged?: () => void;
  public onActionChoices?: (actions: ReadonlyArray<{ readonly id: string; readonly name: string }>, choose: (id: string) => void) => void;
  public onClearActionChoices?: () => void;
  public onVerbSelected?: (verb: InteractionVerb) => void;
  private verb: InteractionVerb = 'walk';
  private readonly objectActionFlow: ObjectActionFlow;

  private lastTimestamp = 0;
  private isRunning = false;
  private animationFrameId: number | null = null;
  private currentRequestId = 0;
  private objectWalkToken = 0;
  private exitRequestInFlight = false;
  private exitRetries = 0;
  private nextExitAttempt = 0;
  private changingRoom = false;
  private pendingEvents: Array<() => void> = [];

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    initialRoom: RoomDefinition = DEFAULT_ROOM,
    playerName?: string,
    private readonly worldId?: string,
    private readonly characterId?: string,
    private readonly characterCatalog?: CharacterCatalog
  ) {
    this.state = new RoomState(initialRoom, playerName);
    this.renderer = new RoomRenderer(this.canvas, this.state.room);

    this.state.onRoomChanged = (room) => {
      this.objectWalkToken++;
      this.objectActionFlow.cancel();
      this.renderer.setRoom(room);
      this.onRoomChanged?.(room);
    };

    this.state.onChatMessage = (msg) => {
      this.onChatMessage?.(msg);
    };
    this.state.onObjectsChanged = () => { this.renderer.updateRoomState(this.state.room); this.onClearActionChoices?.(); this.onObjectsChanged?.(); };
    this.state.onNavigationStopped = () => {
      this.objectWalkToken++;
      this.objectActionFlow.cancel();
      this.selectVerb('walk');
    };

    this.networkClient = new RoomNetworkClient({
      onUserJoined: (dto) => this.dispatchRoomEvent(() => { if (dto.roomId === this.state.room.id) { this.state.addOrUpdateRemotePlayer(dto.player); const character = this.state.remotePlayers.get(dto.player.sessionId); if (character) this.resolveAppearance(character); } }),
      onUserLeft: (dto) => this.dispatchRoomEvent(() => { if (dto.roomId === this.state.room.id) this.state.removeRemotePlayer(dto.sessionId); }),
      onPlayerMoved: (dto) => this.dispatchRoomEvent(() => this.state.handlePlayerMoved(dto)),
      onChatMessageReceived: (dto) => this.dispatchRoomEvent(() => this.state.handleChatMessage(dto)),
      onObjectStatesChanged: (dto) => this.dispatchRoomEvent(() => this.state.handleObjectStates(dto)),
      onWorldReset: () => this.onWorldUnavailable?.('Die Welt wurde geändert und neu gestartet. Bitte erneut beitreten.'),
      onConnectionStatusChanged: (status) => {
        if (status !== 'connected' && this.objectActionFlow.cancel()) {
          this.speakLocally('Die Verbindung wurde unterbrochen.');
          this.selectVerb('walk');
        }
        this.onConnectionStatusChanged?.(status);
      },
      onReconnected: async () => {
        try { await this.joinRoom(); }
        catch (error) { this.networkClient.setStatus('disconnected'); this.onWorldUnavailable?.(String(error)); }
      },
    });

    this.objectActionFlow = new ObjectActionFlow(
      () => ({ roomId: this.room.id, position: this.character.position, idle: this.character.state === 'idle',
        revision: this.state.objectRevision, stateOf: id => this.state.objectStates.get(id) }),
      point => this.setTarget(point),
      (room, object, action, revision) => this.networkClient.requestObjectAction(room, object, action, revision),
      error => { if (error) this.speakLocally(error); this.selectVerb('walk'); }
    );

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
        onNavigateToObject: async object => {
          this.state.pendingExit = null;
          const token = this.objectWalkToken;
          for (const point of nearestUsePoints(objectUsePoints(object), this.character.position)) {
            const result = await this.setTarget(point);
            if (token !== this.objectWalkToken || result.accepted) return;
          }
          this.speakLocally('Der Weg zum Objekt ist nicht erreichbar.');
          this.selectVerb('walk');
        },
        onToggleDebug: () => {
          this.renderer.toggleDebugOverlay();
        },
        onObjectAction: async object => {
          this.onClearActionChoices?.();
          const actions = actionsForVerb(object, this.state.objectStates.get(object.id), this.verb);
          const roomId = this.room.id, expectedState = this.state.objectStates.get(object.id);
          this.state.pendingExit = null;
          const execute = (actionId: string | null) => this.objectActionFlow.start(roomId, object, actionId, expectedState);
          if (actions.length === 0) await execute(null);
          else if (actions.length === 1) await execute(actions[0]!.id);
          else this.onActionChoices?.(actions, id => { void execute(id); });
        },
        onUnavailableTarget: () => { this.speakLocally('Das geht nicht.'); this.selectVerb('walk'); },
        onCanvasAction: () => { this.objectWalkToken++; this.objectActionFlow.cancel(); this.onClearActionChoices?.(); },
      },
      () => this.state.room,
      () => this.state.character.position,
      () => this.renderer.camera.offset,
      () => this.verb,
      point => this.renderer.objectAt(point)
    );
  }

  public selectVerb(verb: InteractionVerb): void {
    this.objectWalkToken++;
    this.objectActionFlow.cancel();
    this.verb = verb;
    this.onVerbSelected?.(verb);
    this.inputHandler.updateVerb();
  }

  private speakLocally(text: string): void {
    this.character.speechBubble = { text, startTime: performance.now(), duration: SPEECH_BUBBLE_DURATION };
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
    if (!connected && this.worldId) throw new Error('Verbindung zur gewählten Welt fehlgeschlagen.');
    if (connected) {
      await this.joinRoom();
    }
  }

  public stop(): void {
    this.isRunning = false;
    this.objectActionFlow.cancel();
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
    this.networkClient.disconnect();
  }

  private async joinRoom(): Promise<void> {
    this.changingRoom = true;
    try {
      const response = await fetch(this.worldId ? `/api/worlds/${encodeURIComponent(this.worldId)}/start` : '/api/world/start');
      if (!response.ok) throw new Error('Weltstart konnte nicht geladen werden.');
      const start = await response.json() as { roomId: string; external: boolean };
      const roomId = this.worldId || start.external ? start.roomId : this.state.room.id;
      const result = await this.networkClient.joinRoom(roomId, this.state.playerName, this.worldId, this.characterId);
      if (!result.success) throw new Error(result.error ?? 'Weltbeitritt fehlgeschlagen.');
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
    } finally {
      this.changingRoom = false;
      for (const event of this.pendingEvents.splice(0)) event();
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
    for (const character of this.state.getAllCharacters()) this.resolveAppearance(character);
  }

  private resolveAppearance(character: Character): void {
    character.appearance = this.characterCatalog?.characters.find(c => c.id === character.characterId);
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
    if (this.worldId) return { accepted: false, error: 'Nicht mit der Welt verbunden.' };
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
    this.objectActionFlow.update();

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
    this.inputHandler.refreshHover();

    this.animationFrameId = requestAnimationFrame(this.loop);
  }
}
