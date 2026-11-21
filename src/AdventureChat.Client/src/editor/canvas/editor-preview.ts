import { loadCharacterCatalog } from '../../network/character-catalog.js';
import { EditorApi, type PreviewResult } from '../core/editor-api.js';
import type { RoomView } from '../core/editor-types.js';
import { RoomRenderer } from '../../rendering/renderer.js';
import { applyActorAnimation, assignPathToCharacter, updateCharacterMovement } from '../../simulation/movement.js';
import type { Character, Point, PreviewObjectStates, RoomDefinition, RoomExit } from '../../types.js';
import { applyObjectAppearances } from '../../state/object-appearance.js';
import { actionForVerb } from '../../input/object-action.js';
import { ObjectActionFlow } from '../../input/object-action-flow.js';
import { exitAt, withinExitReach } from '../../input/exit-hit.js';
import { objectUsePoints, nearestUsePoints } from '../../input/object-use-points.js';
import { EditorPreviewControls } from '../ui/editor-preview-controls.js';
import { NpcDialogueFlow } from '../../input/npc-dialogue-flow.js';
import { npcActorId } from '../../rendering/npc-cast.js';
import { NpcDialoguePanel } from '../../ui/npc-dialogue-panel.js';

export class EditorPreview {
  private readonly controls = new EditorPreviewControls(() => this.cancelCommand());
  private readonly actionFlow: ObjectActionFlow;
  private readonly dialogue: NpcDialoguePanel;
  private readonly dialogueFlow: NpcDialogueFlow;
  private previewRenderer: RoomRenderer | null = null;
  private previewCharacter: Character | null = null;
  private previewAnimationId: number | null = null;
  private lastFrameTime = performance.now();
  private isRunning = false;
  private requestRevision = 0;
  private objectStates: ReadonlyArray<PreviewObjectStates> = [];
  private room: RoomView | null = null;
  private draft: RoomView | null = null;
  private targetFormat: string | undefined;
  private pendingExit: RoomExit | null = null;
  private exits: readonly RoomExit[] = [];
  private initialSnapshot: Promise<void> | null = null;
  private activeAnimation: { objectId: string; actionId: string } | null = null;

  public constructor(private readonly canvas: HTMLCanvasElement) {
    this.actionFlow = new ObjectActionFlow(
      () => ({
        roomId: this.room?.id ?? '',
        position: this.previewCharacter?.position ?? { x: 0, y: 0 },
        idle: this.previewCharacter?.state === 'idle',
        revision: 0,
        stateOf: id => this.currentStates().find(state => state.objectId === id)?.stateId
      }),
      point => this.walk(point),
      (_, objectId, actionId) => this.executeAction(objectId, actionId),
      (error, pending) => { if (!pending) this.tell(error ?? 'Aktion ausgeführt.'); },
      async animation => this.previewRenderer?.prepareActorAnimation(animation)
    );
    this.dialogue = new NpcDialoguePanel(canvas, { verbs: this.controls.element, returnFocus: this.controls.verb,
      advanceEvent: 'mousedown' }, (bubble, npcId) =>
      this.previewRenderer?.setDialogueBubble(npcId ? npcActorId(npcId) : 'preview-player', bubble) ?? false);
    this.dialogueFlow = new NpcDialogueFlow(
      () => ({ roomId: this.room?.id ?? '', position: this.previewCharacter?.position ?? { x: 0, y: 0 },
        idle: this.previewCharacter?.state === 'idle', changingRoom: false }),
      point => this.walk(point),
      (_, npcId) => EditorApi.previewDialogue(this.location(), npcId),
      (npc, options) => this.dialogue.open(npc, options, option => EditorApi.previewDialogue(this.location(), npc.id, option)),
      error => this.tell(error)
    );
  }

  public get cameraOffset(): number { return this.previewRenderer?.camera.offset ?? 0; }
  public get activeRoom(): RoomView | null { return this.room; }

  public isActive(): boolean {
    return this.isRunning;
  }

  public start(roomDef: RoomDefinition, draft: RoomView, targetFormat?: string): void {
    this.isRunning = true;
    this.room = this.draft = structuredClone(draft);
    this.targetFormat = targetFormat;
    this.controls.open(draft.name);
    const blockedExitIds = new Set(roomDef.objects?.flatMap(object => {
      const interaction = object.interaction;
      return interaction?.states.find(state => state.id === interaction.initialState)?.blockedExits?.map(id => id.toLowerCase()) ?? [];
    }));
    this.exits = roomDef.exits?.filter(exit => !blockedExitIds.has(exit.id.toLowerCase())) ?? [];
    this.objectStates = [{ roomId: roomDef.id, states: (roomDef.objects ?? []).flatMap(o =>
      o.interaction ? [{ objectId: o.id, stateId: o.interaction.initialState }] : []) }];
    this.previewRenderer = new RoomRenderer(this.canvas, { ...roomDef, exits: this.exits }, path => EditorApi.backgroundUrl(path));

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
        this.previewRenderer?.setCharacterCatalog(catalog);
      }
    }).catch(() => { /* Preview keeps its loading fallback if content is temporarily unavailable. */ });
    this.lastFrameTime = performance.now();
    this.loop();
    const room = this.room;
    const states = this.objectStates;
    this.initialSnapshot = EditorApi.previewSnapshot(this.location())
      .then(result => { if (this.room === room && this.objectStates === states) this.renderSnapshot(result); })
      .catch(error => { if (this.room === room && this.objectStates === states) this.tell(error.message); });
  }

  public stop(): void {
    this.cancelCommand();
    this.clearAnimation();
    this.isRunning = false;
    this.controls.close();
    if (this.previewAnimationId !== null) {
      cancelAnimationFrame(this.previewAnimationId);
      this.previewAnimationId = null;
    }
    this.previewRenderer = null;
    this.previewCharacter = null;
    this.room = this.draft = null;
    this.initialSnapshot = null;
  }

  public async handleCanvasClick(logicalX: number, logicalY: number): Promise<void> {
    if (!this.previewCharacter || !this.room) return;
    this.cancelCommand();
    const revision = this.requestRevision;
    await this.initialSnapshot;
    if (revision !== this.requestRevision) return;
    const point = { x: logicalX, y: logicalY };
    const verb = this.controls.selectedVerb();
    if (verb === 'talk') {
      const npc = this.previewRenderer?.npcAt(point);
      if (npc) await this.dialogueFlow.start(npc);
      else this.tell('Wähle eine Figur zum Ansprechen.');
      return;
    }
    const exit = exitAt(this.exits, point);
    const object = this.previewRenderer?.objectAt(point);
    if (object && verb !== 'walk' && (verb || object.interaction)) {
      const state = this.currentStates().find(item => item.objectId === object.id)?.stateId;
      await this.actionFlow.start(this.room.id, object, actionForVerb(object, state, verb || 'use'), state);
      return;
    }
    if (verb && verb !== 'walk') { this.tell('Das geht nicht.'); return; }
    if (exit) {
      if (withinExitReach(exit, this.previewCharacter.position)) {
        await this.enterExit(exit);
        return;
      }
      const result = await this.walk(exit.position);
      if (revision !== this.requestRevision) return;
      if (result.accepted) this.pendingExit = exit;
      else this.tell('Der Ausgang ist derzeit nicht erreichbar.');
      return;
    }
    if (verb === 'walk' && object && (object.interaction || object.usePoints?.length)) {
      const origin = this.previewCharacter.position;
      for (const target of nearestUsePoints(objectUsePoints(object, origin), origin)) {
        const result = await this.walk(target);
        if (result.accepted || revision !== this.requestRevision) return;
      }
      this.tell('Der Weg zum Objekt ist nicht erreichbar.');
      return;
    }
    await this.walk(point);
  }

  private cancelCommand(): void {
    this.requestRevision++;
    this.pendingExit = null;
    this.actionFlow.cancel();
    this.dialogueFlow.cancel();
    this.dialogue.close();
    if (!this.previewCharacter) return;
    this.previewCharacter.target = null;
    this.previewCharacter.waypoints = [];
    this.previewCharacter.state = 'idle';
  }

  private async walk(target: Point): Promise<{ accepted: boolean }> {
    const character = this.previewCharacter;
    const room = this.room;
    const revision = this.requestRevision;
    if (!character || !room) return { accepted: false };
    this.clearAnimation();
    try {
      const result = await EditorApi.previewPath(room, character.position, target, this.currentStates(), this.targetFormat);
      if (revision !== this.requestRevision) return { accepted: false };
      this.previewRenderer?.addClickMarker(target.x, target.y, result.success ? 'valid' : 'invalid');
      if (result.success && result.waypoints) assignPathToCharacter(character, result.waypoints);
      return { accepted: result.success };
    } catch {
      if (revision === this.requestRevision) this.previewRenderer?.addClickMarker(target.x, target.y, 'invalid');
      return { accepted: false };
    }
  }

  private currentStates() {
    return this.objectStates.find(state => state.roomId === this.room?.id)?.states ?? [];
  }

  private location() {
    return {
      room: this.draft!, roomId: this.room!.id, position: this.previewCharacter!.position, states: this.objectStates,
      ...(this.previewCharacter?.appearance ? { characterId: this.previewCharacter.appearance.id } : {}),
      ...(this.targetFormat ? { targetFormat: this.targetFormat } : {})
    };
  }

  private async executeAction(objectId: string, actionId: string) {
    if (this.activeAnimation) {
      const same = this.activeAnimation.objectId === objectId && this.activeAnimation.actionId === actionId;
      const pending = same && this.previewCharacter?.actorAnimation?.durationMs != null;
      return { success: same, error: same ? null : 'Beende zuerst die aktuelle Benutzung.', pending };
    }
    const revision = this.requestRevision;
    try {
      const result = await EditorApi.previewAction(this.location(), objectId, actionId);
      if (revision !== this.requestRevision) return result;
      this.renderSnapshot(result);
      if (result.success && this.previewCharacter?.actorAnimation) {
        const command = { objectId, actionId };
        this.activeAnimation = command;
        const { durationMs, elapsedMs } = this.previewCharacter.actorAnimation;
        if (durationMs !== null) void this.finishGesture(command, durationMs - elapsedMs);
      }
      return { ...result, pending: this.previewCharacter?.actorAnimation?.durationMs != null };
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : String(error) };
    }
  }

  private async finishGesture(command: { objectId: string; actionId: string }, wait: number): Promise<void> {
    await new Promise(resolve => setTimeout(resolve, Math.max(0, wait)));
    if (this.activeAnimation !== command) return;
    try {
      const result = await EditorApi.previewAction(this.location(), command.objectId, command.actionId, true);
      if (this.activeAnimation !== command) return;
      this.clearAnimation();
      this.renderSnapshot(result);
      this.tell(result.success ? 'Aktion ausgeführt.' : result.error ?? 'Aktion abgelehnt.');
    } catch (error) {
      if (this.activeAnimation !== command) return;
      this.clearAnimation();
      this.tell(error instanceof Error ? error.message : String(error));
    }
  }

  private clearAnimation(): void {
    this.activeAnimation = null;
    if (this.previewCharacter) this.previewCharacter.actorAnimation = null;
  }

  private async enterExit(exit: RoomExit): Promise<void> {
    const revision = this.requestRevision;
    try {
      const result = await EditorApi.previewExit(this.location(), exit.id);
      if (revision !== this.requestRevision) return;
      if (!result.success) { this.tell(result.error ?? 'Der Ausgang ist derzeit gesperrt.'); return; }
      const room = result.snapshot.roomId === this.draft?.id ? this.draft : await EditorApi.fetchRoom(result.snapshot.roomId);
      if (revision !== this.requestRevision || !room) return;
      this.clearAnimation();
      this.room = room;
      this.renderSnapshot(result, true);
      this.tell(`Raum: ${room.name}`);
    } catch (error) {
      if (revision === this.requestRevision) this.tell(error instanceof Error ? error.message : String(error));
    }
  }

  private renderSnapshot(result: PreviewResult, crossedExit = false): void {
    this.objectStates = result.states;
    const definition = result.snapshot.room;
    const character = this.previewCharacter;
    if (!definition || !character) return;
    const player = result.snapshot.players.find(player => player.sessionId === result.snapshot.yourSessionId);
    if (player?.actorAnimation) {
      character.position = player.position;
      applyActorAnimation(character, player, path => EditorApi.backgroundUrl(path));
    }
    this.exits = definition.exits ?? [];
    const room = { ...definition, backgroundUrl: EditorApi.backgroundUrl(definition.backgroundUrl),
      objects: applyObjectAppearances(definition.objects ?? [], result.snapshot.objectStates ?? [])
        .map(object => ({ ...object, image: EditorApi.backgroundUrl(object.image) })) };
    if (crossedExit) {
      character.position = player?.position ?? room.spawnPoint;
      character.speed = room.walkSpeed ?? 130;
      this.previewRenderer?.setRoom(room);
    } else this.previewRenderer?.updateRoomState(room);
  }

  private tell(text: string): void {
    this.controls.result(text);
    if (this.previewCharacter) this.previewCharacter.speechBubble = { text, startTime: performance.now(), duration: 4000 };
  }

  private loop = (): void => {
    if (!this.isRunning) return;

    const now = performance.now();
    const dt = Math.min((now - this.lastFrameTime) / 1000, 0.1);
    this.lastFrameTime = now;

    if (this.previewCharacter) {
      updateCharacterMovement(this.previewCharacter, dt);
      this.actionFlow.update();
      this.dialogueFlow.update();
      if (this.previewCharacter.state === 'idle' && this.pendingExit) {
        const exit = this.pendingExit;
        this.pendingExit = null;
        void this.enterExit(exit);
      }
    }

    if (this.previewRenderer && this.previewCharacter) {
      this.previewRenderer.render(this.previewCharacter, []);
    }

    this.previewAnimationId = requestAnimationFrame(this.loop);
  };
}
