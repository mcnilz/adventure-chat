import { HorizontalCamera, resizeCanvas } from './camera.js';
import { CLICK_MARKER_DURATION } from '../constants.js';
import type { Character, RoomDefinition, RoomExit } from '../types.js';
import { CharacterRenderer } from './character-renderer.js';
import { OverlayRenderer, type ClickMarker } from './overlay-renderer.js';
import { SpeechBubbleRenderer } from './speech-bubble-renderer.js';

export { type ClickMarker } from './overlay-renderer.js';

export class RoomRenderer {
  private readonly ctx: CanvasRenderingContext2D;
  private readonly backgroundImage: HTMLImageElement;
  private readonly characterRenderer: CharacterRenderer;
  private readonly speechBubbleRenderer: SpeechBubbleRenderer;
  private readonly overlayRenderer: OverlayRenderer;

  public readonly camera = new HorizontalCamera();
  private cameraInitialized = false;
  private backgroundLoaded = false;
  private clickMarkers: ClickMarker[] = [];
  private hoveredExit: RoomExit | null = null;
  public showDebugOverlay = false;

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private room: RoomDefinition
  ) {
    const context = this.canvas.getContext('2d');
    if (!context) {
      throw new Error('Canvas 2D context is not supported');
    }
    this.ctx = context;
    this.ctx.imageSmoothingEnabled = false;

    this.characterRenderer = new CharacterRenderer();
    this.speechBubbleRenderer = new SpeechBubbleRenderer();
    this.overlayRenderer = new OverlayRenderer();

    this.backgroundImage = new Image();
    this.backgroundImage.src = this.room.backgroundUrl;
    this.backgroundImage.onload = () => {
      this.backgroundLoaded = true;
    };
  }

  public setHoveredExit(exit: RoomExit | null): void {
    this.hoveredExit = exit;
  }

  public setRoom(newRoom: RoomDefinition): void {
    this.room = newRoom;
    this.cameraInitialized = false;
    if (!this.backgroundImage.src.endsWith(newRoom.backgroundUrl)) {
      this.backgroundLoaded = false;
      this.backgroundImage.src = newRoom.backgroundUrl;
    }
  }

  public addClickMarker(x: number, y: number, type: 'valid' | 'invalid'): void {
    this.clickMarkers.push({
      x,
      y,
      type,
      startTime: performance.now(),
      duration: CLICK_MARKER_DURATION,
    });
  }

  public toggleDebugOverlay(): boolean {
    this.showDebugOverlay = !this.showDebugOverlay;
    return this.showDebugOverlay;
  }

  public render(
    localCharacter: Character,
    remoteCharacters: Character[] = [],
    connectionStatus: 'connected' | 'connecting' | 'disconnected' = 'connected'
  ): void {
    const { ctx, canvas, room } = this;

    resizeCanvas(canvas, room);
    ctx.imageSmoothingEnabled = false;
    if (!this.cameraInitialized) { this.camera.reset(room, localCharacter.position.x); this.cameraInitialized = true; }
    this.camera.follow(room, localCharacter.position.x);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.save();
    ctx.translate(-this.camera.offset, 0);

    // 1. Background Image
    if (this.backgroundLoaded) {
      ctx.drawImage(this.backgroundImage, 0, 0, room.width, room.height);
    } else {
      ctx.fillStyle = '#2d3748';
      ctx.fillRect(0, 0, room.width, room.height);
    }

    // 1b. Exit doorway debug rendering (only in debug mode)
    if (this.showDebugOverlay) {
      this.overlayRenderer.drawExits(ctx, room);
    }

    const allCharacters = [localCharacter, ...remoteCharacters];

    // 2. Debug Overlay
    if (this.showDebugOverlay) {
      this.overlayRenderer.drawDebugOverlay(ctx, room, allCharacters);
    }

    // 3. Target Position Marker
    if (localCharacter.target && localCharacter.state === 'walk') {
      this.overlayRenderer.drawTargetMarker(ctx, localCharacter.target.x, localCharacter.target.y);
    }

    // 4. Click Feedback Ripples
    const now = performance.now();
    this.clickMarkers = this.overlayRenderer.drawClickMarkers(ctx, this.clickMarkers, now);

    // 5. Draw Characters sorted by Y position (painter's algorithm)
    allCharacters.sort((a, b) => a.position.y - b.position.y);
    for (const char of allCharacters) {
      const isLocal = char.id === localCharacter.id;
      this.characterRenderer.drawCharacter(ctx, char, isLocal, room);
    }

    // 6. Draw Speech Bubbles
    for (const char of allCharacters) {
      this.speechBubbleRenderer.drawSpeechBubble(ctx, char, this.camera.offset + canvas.width, now, room.actorHeight ?? 64, this.camera.offset);
    }

    // 7. Hovered Exit Highlight (only in debug mode to keep screen clean)
    if (this.hoveredExit && this.showDebugOverlay) {
      this.overlayRenderer.drawHoveredExitAction(ctx, this.hoveredExit);
    }

    ctx.restore();

    // 8. Connection Status Indicator
    if (connectionStatus !== 'connected') {
      this.overlayRenderer.drawConnectionStatus(ctx, connectionStatus);
    }
  }
}
