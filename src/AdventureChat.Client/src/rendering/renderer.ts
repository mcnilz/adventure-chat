import { actorHeightAt } from './depth-scale.js';
import { ObjectRenderer, compareObjectDepth } from './object-renderer.js';
import { HorizontalCamera, resizeCanvas, viewSize } from './camera.js';
import { CLICK_MARKER_DURATION } from '../constants.js';
import type { Character, ObjectActorAnimation, Point, RenderedRoom, RoomExit, RoomObject, SpeechBubble } from '../types.js';
import { CharacterRenderer } from './character-renderer.js';
import { OverlayRenderer, type ClickMarker } from './overlay-renderer.js';
import { SpeechBubbleRenderer, type SpeechLayout } from './speech-bubble-renderer.js';
import { continueObjectMovements, objectAtTime } from '../simulation/object-movement.js';

export { type ClickMarker } from './overlay-renderer.js';

export class RoomRenderer {
  private readonly objects = new ObjectRenderer();
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
  private dialogueBubble: { speakerId: string; bubble: SpeechBubble; layout: SpeechLayout } | null = null;
  public showDebugOverlay = false;

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private room: RenderedRoom,
    private readonly actorImageUrl = (path: string) => path
  ) {
    const context = this.canvas.getContext('2d');
    if (!context) {
      throw new Error('Canvas 2D context is not supported');
    }
    this.ctx = context;
    this.ctx.imageSmoothingEnabled = false;

    this.characterRenderer = new CharacterRenderer();
    this.preloadActorAnimations(room);
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

  public setRoom(newRoom: RenderedRoom): void {
    this.room = newRoom;
    this.dialogueBubble = null;
    this.preloadActorAnimations(newRoom);
    this.cameraInitialized = false;
    if (!this.backgroundImage.src.endsWith(newRoom.backgroundUrl)) {
      this.backgroundLoaded = false;
      this.backgroundImage.src = newRoom.backgroundUrl;
    }
  }

  private preloadActorAnimations(room: RenderedRoom): void {
    for (const object of room.objects ?? [])
      for (const action of object.interaction?.actions ?? [])
        for (const path of Object.values(action.actorAnimation?.sprites ?? {}))
          this.characterRenderer.preload(this.actorImageUrl(path));
  }

  public async prepareActorAnimation(animation: ObjectActorAnimation): Promise<void> {
    await Promise.all(Object.values(animation.sprites ?? {}).map(path =>
      this.characterRenderer.prepare(this.actorImageUrl(path))));
  }

  public setObjects(objects: ReadonlyArray<RoomObject>): void {
    this.room = { ...this.room, objects };
  }

  public updateRoomState(room: RenderedRoom): void {
    this.room = room.objects
      ? { ...room, objects: continueObjectMovements(room.objects, this.room.objects ?? []) }
      : room;
    this.hoveredExit = null;
  }

  public objectAt(point: Point): RoomObject | undefined {
    return this.objectsAtTime(performance.now()).reverse()
      .find(object => this.objects.containsVisiblePixel(object, point));
  }

  public setDialogueBubble(speakerId: string, bubble: SpeechBubble | null): boolean {
    const { width, height } = viewSize(this.room);
    const layout = bubble && this.speechBubbleRenderer.prepareDialogue(this.ctx, bubble.text, width, height);
    this.dialogueBubble = bubble && layout ? { speakerId, bubble, layout } : null;
    return !bubble || !!layout;
  }

  private objectsAtTime(now: number): RoomObject[] {
    return (this.room.objects ?? []).map(object => objectAtTime(object, now)).sort(compareObjectDepth);
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

    // Objects win over actors behind their line; actors win at equality.
    allCharacters.sort((a, b) => a.position.y - b.position.y);
    const objects = this.objectsAtTime(now);
    let index = 0;
    for (const char of allCharacters) {
      while (index < objects.length && objects[index]!.depthY <= char.position.y) {
        this.objects.draw(ctx, objects[index++]!);
      }
      this.characterRenderer.drawCharacter(ctx, char, char.id === localCharacter.id, room, objects.length === 0);
    }
    while (index < objects.length) this.objects.draw(ctx, objects[index++]!);
    if (objects.length > 0) for (const char of allCharacters) this.characterRenderer.drawName(ctx, char, char.id === localCharacter.id, room);

    // 6. Draw Speech Bubbles
    const viewportRight = this.camera.offset + canvas.width;
    let dialogueSpeaker: Character | undefined;
    for (const char of allCharacters) {
      const dialogue = this.dialogueBubble?.speakerId === char.id ? this.dialogueBubble.bubble : null;
      if (dialogue) dialogueSpeaker = { ...char, speechBubble: dialogue };
      else this.speechBubbleRenderer.drawSpeechBubble(ctx, char, viewportRight, now,
        actorHeightAt(room, char.position.y), this.camera.offset, !!room.depthScale);
    }
    if (dialogueSpeaker && this.dialogueBubble) {
      this.speechBubbleRenderer.drawSpeechBubble(ctx, dialogueSpeaker, viewportRight, now,
        actorHeightAt(room, dialogueSpeaker.position.y), this.camera.offset, !!room.depthScale, this.dialogueBubble.layout);
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
