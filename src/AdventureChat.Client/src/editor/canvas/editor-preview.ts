import { EditorApi } from '../core/editor-api.js';
import type { RoomYamlDto } from '../core/editor-types.js';
import { RoomRenderer } from '../../rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from '../../simulation/movement.js';
import type { Character, RoomDefinition } from '../../types.js';

export class EditorPreview {
  private previewRenderer: RoomRenderer | null = null;
  private previewCharacter: Character | null = null;
  private previewAnimationId: number | null = null;
  private lastFrameTime = performance.now();
  private isRunning = false;
  private requestRevision = 0;

  public constructor(private readonly canvas: HTMLCanvasElement) {}

  public isActive(): boolean {
    return this.isRunning;
  }

  public start(roomDef: RoomDefinition): void {
    this.isRunning = true;
    this.previewRenderer = new RoomRenderer(this.canvas, roomDef);

    // Initial character spawn
    const defaultSpawn = roomDef.spawnPoint ?? { x: 320, y: 300 };
    this.previewCharacter = {
      id: 'preview-player',
      name: 'Tester',
      position: { ...defaultSpawn },
      facing: 'down',
      state: 'idle',
      target: null,
      waypoints: [],
      speed: roomDef.walkSpeed ?? 130,
      walkFrame: 0,
      frameTimer: 0,
      speechBubble: {
        text: 'Vorschaumodus aktiv! Klicke, um zu laufen.',
        startTime: performance.now(),
        duration: 4000
      }
    };

    this.lastFrameTime = performance.now();
    this.loop();
  }

  public stop(): void {
    this.isRunning = false;
    this.requestRevision++;
    if (this.previewAnimationId !== null) {
      cancelAnimationFrame(this.previewAnimationId);
      this.previewAnimationId = null;
    }
    this.previewRenderer = null;
    this.previewCharacter = null;
  }

  public async handleCanvasClick(logicalX: number, logicalY: number, room: RoomYamlDto): Promise<void> {
    if (!this.previewCharacter || !this.isRunning) return;
    const revision = ++this.requestRevision;
    // Freeze at the request start so the returned route has the same footpoint.
    const start = { ...this.previewCharacter.position };
    this.previewCharacter.target = null;
    this.previewCharacter.waypoints = [];
    this.previewCharacter.state = 'idle';
    try {
      const result = await EditorApi.previewPath(room, start, { x: logicalX, y: logicalY });
      if (!this.isRunning || revision !== this.requestRevision) return;
      this.previewRenderer?.addClickMarker(logicalX, logicalY, result.success ? 'valid' : 'invalid');
      if (result.success && result.waypoints) assignPathToCharacter(this.previewCharacter, result.waypoints);
    } catch {
      if (this.isRunning && revision === this.requestRevision) this.previewRenderer?.addClickMarker(logicalX, logicalY, 'invalid');
    }
  }

  private loop = (): void => {
    if (!this.isRunning) return;

    const now = performance.now();
    const dt = Math.min((now - this.lastFrameTime) / 1000, 0.1);
    this.lastFrameTime = now;

    if (this.previewCharacter) {
      updateCharacterMovement(this.previewCharacter, dt);
    }

    if (this.previewRenderer && this.previewCharacter) {
      this.previewRenderer.render(this.previewCharacter, []);
    }

    this.previewAnimationId = requestAnimationFrame(this.loop);
  };
}
