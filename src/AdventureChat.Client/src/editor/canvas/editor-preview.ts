import { loadCharacterCatalog } from '../../network/character-catalog.js';
import { EditorApi } from '../core/editor-api.js';
import type { RoomView } from '../core/editor-types.js';
import { RoomRenderer } from '../../rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from '../../simulation/movement.js';
import type { Character, PreviewObjectStates, RoomDefinition } from '../../types.js';
import { applyObjectAppearances } from '../../state/object-appearance.js';

export class EditorPreview {
  private previewRenderer: RoomRenderer | null = null;
  private previewCharacter: Character | null = null;
  private previewAnimationId: number | null = null;
  private lastFrameTime = performance.now();
  private isRunning = false;
  private requestRevision = 0;
  private objectStates: ReadonlyArray<PreviewObjectStates> = [];

  public constructor(private readonly canvas: HTMLCanvasElement) {}

  public get cameraOffset(): number { return this.previewRenderer?.camera.offset ?? 0; }

  public isActive(): boolean {
    return this.isRunning;
  }

  public start(roomDef: RoomDefinition): void {
    this.isRunning = true;
    this.objectStates = [{ roomId: roomDef.id, states: (roomDef.objects ?? []).flatMap(o =>
      o.interaction ? [{ objectId: o.id, stateId: o.interaction.initialState }] : []) }];
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
        text: 'Klicke zum Laufen oder auf ein interaktives Objekt zum Ausprobieren.',
        startTime: performance.now(),
        duration: 4000
      }
    };

    const character = this.previewCharacter;
    if (EditorApi.worldId) void loadCharacterCatalog(EditorApi.worldId).then(catalog => {
      if (this.previewCharacter === character && character) {
        character.appearance = catalog.characters.find(c => c.id === catalog.defaultCharacterId);
      }
    }).catch(() => { /* Preview keeps its loading fallback if content is temporarily unavailable. */ });
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

  public async handleCanvasClick(logicalX: number, logicalY: number, room: RoomView): Promise<void> {
    if (!this.previewCharacter || !this.isRunning) return;
    const revision = ++this.requestRevision;
    // Freeze at the request start so the returned route has the same footpoint.
    const start = { ...this.previewCharacter.position };
    this.previewCharacter.target = null;
    this.previewCharacter.waypoints = [];
    this.previewCharacter.state = 'idle';
    try {
      const states = this.objectStates.find(s => s.roomId === room.id)?.states ?? [];
      const object = this.previewRenderer?.objectAt({ x: logicalX, y: logicalY });
      const stateId = states.find(s => s.objectId === object?.id)?.stateId;
      const action = object?.interaction?.actions.find(a => a.from === stateId);
      if (object && action) {
        const result = await EditorApi.previewAction(room, start, object.id, action.id, this.objectStates);
        if (!this.isRunning || revision !== this.requestRevision) return;
        this.objectStates = result.states;
        const definition = result.snapshot.room;
        if (definition) this.previewRenderer?.updateRoomState({ ...definition,
          backgroundUrl: EditorApi.backgroundUrl(definition.backgroundUrl),
          objects: applyObjectAppearances(definition.objects ?? [], result.snapshot.objectStates ?? []).map(o => ({ ...o, image: EditorApi.backgroundUrl(o.image) })) });
        this.previewCharacter.speechBubble = { text: result.error ?? 'Aktion ausgeführt.', startTime: performance.now(), duration: 4000 };
        return;
      }
      const result = await EditorApi.previewPath(room, start, { x: logicalX, y: logicalY }, states);
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
