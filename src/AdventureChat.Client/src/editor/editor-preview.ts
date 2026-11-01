import { isPointWalkable } from '../math.js';
import { RoomRenderer } from '../rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from '../simulation/movement.js';
import type { Character, RoomDefinition } from '../types.js';

export class EditorPreview {
  private previewRenderer: RoomRenderer | null = null;
  private previewCharacter: Character | null = null;
  private previewAnimationId: number | null = null;
  private lastFrameTime = performance.now();
  private isRunning = false;

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
      speed: 120,
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
    if (this.previewAnimationId !== null) {
      cancelAnimationFrame(this.previewAnimationId);
      this.previewAnimationId = null;
    }
    this.previewRenderer = null;
    this.previewCharacter = null;
  }

  public handleCanvasClick(logicalX: number, logicalY: number, roomDef: RoomDefinition): void {
    if (!this.previewCharacter || !this.isRunning) return;

    const target = { x: logicalX, y: logicalY };
    if (isPointWalkable(target, roomDef.geometry.outerBoundary, roomDef.geometry.obstacles)) {
      this.previewRenderer?.addClickMarker(logicalX, logicalY, 'valid');
      assignPathToCharacter(this.previewCharacter, [this.previewCharacter.position, target]);
    } else {
      this.previewRenderer?.addClickMarker(logicalX, logicalY, 'invalid');
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
