import { closestPointOnSegment, distance, isPointInPolygon, isPointWalkable } from '../math.js';
import { RoomRenderer } from '../rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from '../simulation/movement.js';
import type { Character, Point } from '../types.js';
import { EditorRenderer } from './editor-renderer.js';
import { EditorState } from './editor-state.js';
import type {
  BackgroundsOverviewDto,
  EditorMode,
  EditorTool,
  HoveredEdge,
  RoomPointDto,
  RoomSummaryDto,
  RoomYamlDto
} from './editor-types.js';

export interface EditorElements {
  roomSelect: HTMLSelectElement;
  btnNewRoom: HTMLButtonElement;
  btnSave: HTMLButtonElement;
  btnValidate: HTMLButtonElement;
  btnToggleMode: HTMLButtonElement;
  btnDeleteVertex: HTMLButtonElement;
  toolSelect: HTMLButtonElement;
  toolWalkable: HTMLButtonElement;
  toolObstacle: HTMLButtonElement;
  toolSpawn: HTMLButtonElement;
  toolExit: HTMLButtonElement;
  inputId: HTMLInputElement;
  inputName: HTMLInputElement;
  inputBg: HTMLInputElement;
  inputWidth: HTMLInputElement;
  inputHeight: HTMLInputElement;
  inputNotes: HTMLTextAreaElement;
  statusBanner: HTMLElement;
  errorList: HTMLElement;
  unsavedBadge: HTMLElement;
  // Inspector
  inspectorEmpty: HTMLElement;
  inspectorVertex: HTMLElement;
  inspectorVertexBadge: HTMLElement;
  inspectorVertexX: HTMLInputElement;
  inspectorVertexY: HTMLInputElement;
  btnInspectorDeleteVertex: HTMLButtonElement;
  // Polygon Inspector
  inspectorPolygon: HTMLElement;
  inspectorPolygonBadge: HTMLElement;
  inspectorPolygonId: HTMLInputElement;
  inspectorPolygonPointsCount: HTMLInputElement;
  btnInspectorDeletePolygon: HTMLButtonElement;
  inspectorSpawn: HTMLElement;
  inspectorSpawnId: HTMLInputElement;
  inspectorSpawnX: HTMLInputElement;
  inspectorSpawnY: HTMLInputElement;
  btnInspectorDeleteSpawn: HTMLButtonElement;
  inspectorExit: HTMLElement;
  inspectorExitId: HTMLInputElement;
  inspectorExitRoom: HTMLInputElement;
  inspectorExitTargetSpawn: HTMLInputElement;
  inspectorExitLabel: HTMLInputElement;
  inspectorExitX: HTMLInputElement;
  inspectorExitY: HTMLInputElement;
  inspectorExitRadius: HTMLInputElement;
  btnInspectorDeleteExit: HTMLButtonElement;
  // Background picker & upload
  btnOpenBgModal: HTMLButtonElement;
  btnUploadBgTrigger: HTMLButtonElement;
  inputBgFile: HTMLInputElement;
  bgFsHintPath: HTMLElement;
  modalBgPicker: HTMLDialogElement;
  btnCloseBgModal: HTMLButtonElement;
  btnCloseBgModalFooter: HTMLButtonElement;
  modalFsPath: HTMLElement;
  btnCopyFsPath: HTMLButtonElement;
  bgDropZone: HTMLElement;
  btnModalUploadSelect: HTMLButtonElement;
  bgGalleryGrid: HTMLElement;
  bgCount: HTMLElement;
}

export class EditorApp {
  private readonly state: EditorState;
  private readonly editorRenderer: EditorRenderer;
  private previewRenderer: RoomRenderer | null = null;

  private mode: EditorMode = 'edit';
  private currentTool: EditorTool = 'select';

  private mouseLogicalPos: RoomPointDto | null = null;
  private isDragging = false;
  private dragPos: RoomPointDto | null = null;
  private dragStartLogicalPos: RoomPointDto | null = null;
  private dragOffset: { dx: number; dy: number } | null = null;
  private hoveredEdge: HoveredEdge | null = null;
  private hoveredEntity: { type: 'spawn' | 'exit'; id: string } | null = null;
  private backgroundsOverview: BackgroundsOverviewDto | null = null;

  // Preview character state
  private previewCharacter: Character | null = null;
  private previewAnimationId: number | null = null;
  private lastFrameTime = performance.now();

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly elements: EditorElements
  ) {
    this.state = new EditorState();
    this.editorRenderer = new EditorRenderer(this.canvas);

    this.bindEvents();
    this.updateFormFromState();
    this.updateInspector();
    this.render();
  }

  public async init(): Promise<void> {
    await this.loadRoomList(false);
    void this.loadBackgroundsList();
    window.addEventListener('beforeunload', (e) => {
      if (this.state.isDirty()) {
        e.preventDefault();
        e.returnValue = '';
      }
    });
  }

  public getState(): EditorState {
    return this.state;
  }

  public getMode(): EditorMode {
    return this.mode;
  }

  public setTool(tool: EditorTool): void {
    this.currentTool = tool;
    this.updateToolButtons();
    this.updateCursor();
    this.render();
  }

  public async loadRoomList(preserveActive = true): Promise<void> {
    try {
      const response = await fetch('/api/editor/rooms');
      if (!response.ok) return;

      const summaries = (await response.json()) as RoomSummaryDto[];
      const select = this.elements.roomSelect;
      const currentVal = this.state.getRoom().id;
      select.innerHTML = '';

      for (const summary of summaries) {
        const option = document.createElement('option');
        option.value = summary.id;
        option.textContent = `${summary.name} (${summary.id})`;
        select.appendChild(option);
      }

      if (preserveActive && currentVal) {
        select.value = currentVal;
      } else if (!preserveActive && summaries.length > 0 && summaries[0]) {
        await this.loadRoom(summaries[0].id);
      }
    } catch (err) {
      this.setStatus(`Fehler beim Laden der Raumliste: ${String(err)}`, 'error');
    }
  }

  public async loadRoom(roomId: string): Promise<void> {
    if (this.state.isDirty()) {
      const confirmLeave = confirm('Ungespeicherte Änderungen gehen verloren. Fortfahren?');
      if (!confirmLeave) return;
    }

    try {
      const response = await fetch(`/api/editor/rooms/${encodeURIComponent(roomId)}`);
      if (!response.ok) {
        this.setStatus(`Raum '${roomId}' konnte nicht geladen werden.`, 'error');
        return;
      }

      const dto = (await response.json()) as RoomYamlDto;
      this.state.setRoom(dto);
      this.editorRenderer.setBackground(dto.background);
      this.elements.roomSelect.value = dto.id;
      this.updateFormFromState();
      this.updateInspector();
      this.setStatus(`Raum '${dto.name}' geladen.`, 'success');
      this.render();
    } catch (err) {
      this.setStatus(`Fehler: ${String(err)}`, 'error');
    }
  }

  public async saveRoom(): Promise<boolean> {
    const room = this.state.getRoom();
    this.setStatus('Speichere Raum...', 'info');

    try {
      const response = await fetch(`/api/editor/rooms/${encodeURIComponent(room.id)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(room)
      });

      const result = await response.json();
      if (!response.ok || !result.success) {
        const errors = (result.errors as string[]) ?? ['Speichern fehlgeschlagen.'];
        this.showValidationErrors(errors);
        this.setStatus('Validierungsfehler beim Speichern.', 'error');
        return false;
      }

      this.state.markClean();
      this.clearValidationErrors();
      this.setStatus(`Raum '${room.id}' erfolgreich atomar gespeichert!`, 'success');
      this.updateUnsavedBadge();
      await this.loadRoomList(true);
      return true;
    } catch (err) {
      this.setStatus(`Netzwerkfehler beim Speichern: ${String(err)}`, 'error');
      return false;
    }
  }

  public async validateCurrentRoom(): Promise<boolean> {
    const room = this.state.getRoom();
    try {
      const response = await fetch('/api/editor/validate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(room)
      });

      if (!response.ok) {
        this.setStatus('Server-Validierung nicht erreichbar.', 'error');
        return false;
      }

      const result = await response.json();
      if (!result.valid) {
        this.showValidationErrors(result.errors as string[]);
        this.setStatus(`Raumprüfung: ${result.errors.length} Fehler gefunden.`, 'error');
        return false;
      }

      this.clearValidationErrors();
      this.setStatus('Raum ist gültig und schema-konform!', 'success');
      return true;
    } catch (err) {
      this.setStatus(`Validierungsfehler: ${String(err)}`, 'error');
      return false;
    }
  }

  public setMode(mode: EditorMode): void {
    if (this.mode === mode) return;
    this.mode = mode;

    if (mode === 'preview') {
      this.elements.btnToggleMode.textContent = '✏️ Editor-Modus';
      this.elements.btnToggleMode.classList.add('active');
      this.startPreview();
    } else {
      this.elements.btnToggleMode.textContent = '▶️ Raum-Vorschau';
      this.elements.btnToggleMode.classList.remove('active');
      this.stopPreview();
      this.updateCursor();
      this.render();
    }
  }

  private startPreview(): void {
    const runtimeRoom = this.state.toRuntimeRoomDefinition();
    this.previewRenderer = new RoomRenderer(this.canvas, runtimeRoom);
    this.previewRenderer.showDebugOverlay = true;

    const spawn = runtimeRoom.spawns?.[0]?.position ?? runtimeRoom.spawnPoint;
    this.previewCharacter = {
      id: 'preview-hero',
      name: 'Vorschau-Figur',
      position: { x: spawn.x, y: spawn.y },
      target: null,
      waypoints: [],
      facing: 'down',
      state: 'idle',
      speed: 120,
      walkFrame: 0,
      frameTimer: 0
    };

    this.lastFrameTime = performance.now();
    const loop = (time: number) => {
      const dt = Math.min((time - this.lastFrameTime) / 1000, 0.1);
      this.lastFrameTime = time;

      if (this.previewCharacter && this.previewRenderer) {
        if (this.previewCharacter.state === 'walk') {
          updateCharacterMovement(this.previewCharacter, dt);
        }
        this.previewRenderer.render(this.previewCharacter, []);
      }

      if (this.mode === 'preview') {
        this.previewAnimationId = requestAnimationFrame(loop);
      }
    };

    this.previewAnimationId = requestAnimationFrame(loop);
  }

  private stopPreview(): void {
    if (this.previewAnimationId !== null) {
      cancelAnimationFrame(this.previewAnimationId);
      this.previewAnimationId = null;
    }
    this.previewRenderer = null;
    this.previewCharacter = null;
  }

  private bindEvents(): void {
    // Toolbar buttons
    this.elements.btnSave.addEventListener('click', () => void this.saveRoom());
    this.elements.btnValidate.addEventListener('click', () => void this.validateCurrentRoom());
    this.elements.btnToggleMode.addEventListener('click', () => {
      this.setMode(this.mode === 'edit' ? 'preview' : 'edit');
    });

    this.elements.btnNewRoom.addEventListener('click', () => {
      if (this.state.isDirty() && !confirm('Ungespeicherte Änderungen verwerfen?')) return;
      const defaultRoom = this.state.createDefaultRoom();
      defaultRoom.id = `room-${Date.now().toString(36)}`;
      defaultRoom.name = 'Neuer Raum';
      this.state.setRoom(defaultRoom);
      this.editorRenderer.setBackground(defaultRoom.background);
      this.updateFormFromState();
      this.updateInspector();
      this.setStatus('Neuer Raum erstellt.', 'info');
      this.render();
    });

    this.elements.roomSelect.addEventListener('change', () => {
      const id = this.elements.roomSelect.value;
      if (id) void this.loadRoom(id);
    });

    const handleDelete = () => {
      if (this.state.deleteSelectedEntity()) {
        this.updateInspector();
        this.updateUnsavedBadge();
        this.render();
      }
    };

    this.elements.btnDeleteVertex.addEventListener('click', handleDelete);
    this.elements.btnInspectorDeleteVertex.addEventListener('click', handleDelete);
    this.elements.btnInspectorDeletePolygon.addEventListener('click', handleDelete);
    this.elements.btnInspectorDeleteSpawn.addEventListener('click', handleDelete);
    this.elements.btnInspectorDeleteExit.addEventListener('click', handleDelete);

    // Background modal & upload actions
    this.elements.btnOpenBgModal.addEventListener('click', () => this.openBackgroundModal());
    this.elements.btnUploadBgTrigger.addEventListener('click', () => this.elements.inputBgFile.click());
    this.elements.btnModalUploadSelect.addEventListener('click', () => this.elements.inputBgFile.click());
    this.elements.btnCloseBgModal.addEventListener('click', () => this.closeBackgroundModal());
    this.elements.btnCloseBgModalFooter.addEventListener('click', () => this.closeBackgroundModal());
    this.elements.btnCopyFsPath.addEventListener('click', () => void this.copyFilesystemPath());
    this.elements.inputBgFile.addEventListener('change', () => {
      const file = this.elements.inputBgFile.files?.[0];
      if (file) void this.uploadBackgroundFile(file);
    });

    const dropZone = this.elements.bgDropZone;
    if (dropZone) {
      dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('drag-over');
      });
      dropZone.addEventListener('dragleave', () => {
        dropZone.classList.remove('drag-over');
      });
      dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('drag-over');
        const file = e.dataTransfer?.files?.[0];
        if (file) void this.uploadBackgroundFile(file);
      });
    }

    // Tool selection buttons
    this.elements.toolSelect.addEventListener('click', () => this.setTool('select'));
    this.elements.toolWalkable.addEventListener('click', () => this.setTool('add-walkable'));
    this.elements.toolObstacle.addEventListener('click', () => this.setTool('add-obstacle'));
    this.elements.toolSpawn.addEventListener('click', () => this.setTool('add-spawn'));
    this.elements.toolExit.addEventListener('click', () => this.setTool('add-exit'));

    // Input bindings (Room metadata)
    this.elements.inputId.addEventListener('input', () => {
      this.state.updateMetadata({ id: this.elements.inputId.value.trim() });
      this.updateUnsavedBadge();
    });
    this.elements.inputName.addEventListener('input', () => {
      this.state.updateMetadata({ name: this.elements.inputName.value });
      this.updateUnsavedBadge();
    });
    this.elements.inputBg.addEventListener('change', () => {
      const bg = this.elements.inputBg.value.trim();
      this.state.updateMetadata({ background: bg });
      this.editorRenderer.setBackground(bg);
      this.updateUnsavedBadge();
      this.render();
    });
    this.elements.inputNotes.addEventListener('input', () => {
      this.state.updateMetadata({ notes: this.elements.inputNotes.value });
      this.updateUnsavedBadge();
    });

    // Inspector Inputs bindings
    this.elements.inspectorVertexX.addEventListener('input', () => {
      const x = Number(this.elements.inspectorVertexX.value);
      const y = Number(this.elements.inspectorVertexY.value);
      if (Number.isFinite(x) && Number.isFinite(y)) {
        this.state.updateSelectedVertex(x, y);
        this.updateUnsavedBadge();
        this.render();
      }
    });

    this.elements.inspectorVertexY.addEventListener('input', () => {
      const x = Number(this.elements.inspectorVertexX.value);
      const y = Number(this.elements.inspectorVertexY.value);
      if (Number.isFinite(x) && Number.isFinite(y)) {
        this.state.updateSelectedVertex(x, y);
        this.updateUnsavedBadge();
        this.render();
      }
    });

    this.elements.inspectorSpawnId.addEventListener('input', () => {
      const id = this.elements.inspectorSpawnId.value.trim();
      const x = Number(this.elements.inspectorSpawnX.value);
      const y = Number(this.elements.inspectorSpawnY.value);
      if (id && Number.isFinite(x) && Number.isFinite(y)) {
        this.state.updateSelectedSpawn(id, x, y);
        this.updateUnsavedBadge();
        this.render();
      }
    });

    this.elements.inspectorSpawnX.addEventListener('input', () => {
      const id = this.elements.inspectorSpawnId.value.trim();
      const x = Number(this.elements.inspectorSpawnX.value);
      const y = Number(this.elements.inspectorSpawnY.value);
      if (id && Number.isFinite(x) && Number.isFinite(y)) {
        this.state.updateSelectedSpawn(id, x, y);
        this.updateUnsavedBadge();
        this.render();
      }
    });

    this.elements.inspectorSpawnY.addEventListener('input', () => {
      const id = this.elements.inspectorSpawnId.value.trim();
      const x = Number(this.elements.inspectorSpawnX.value);
      const y = Number(this.elements.inspectorSpawnY.value);
      if (id && Number.isFinite(x) && Number.isFinite(y)) {
        this.state.updateSelectedSpawn(id, x, y);
        this.updateUnsavedBadge();
        this.render();
      }
    });

    const updateExitFromInspector = () => {
      const id = this.elements.inspectorExitId.value.trim();
      const room = this.elements.inspectorExitRoom.value.trim();
      const spawn = this.elements.inspectorExitTargetSpawn.value.trim();
      const label = this.elements.inspectorExitLabel.value.trim();
      const x = Number(this.elements.inspectorExitX.value);
      const y = Number(this.elements.inspectorExitY.value);
      const radius = Number(this.elements.inspectorExitRadius.value) || 35;

      if (id && room && spawn && Number.isFinite(x) && Number.isFinite(y)) {
        this.state.updateSelectedExit({
          id,
          target: { room, spawn },
          label: label || null,
          position: { x, y },
          radius
        });
        this.updateUnsavedBadge();
        this.render();
      }
    };

    this.elements.inspectorExitId.addEventListener('input', updateExitFromInspector);
    this.elements.inspectorExitRoom.addEventListener('input', updateExitFromInspector);
    this.elements.inspectorExitTargetSpawn.addEventListener('input', updateExitFromInspector);
    this.elements.inspectorExitLabel.addEventListener('input', updateExitFromInspector);
    this.elements.inspectorExitX.addEventListener('input', updateExitFromInspector);
    this.elements.inspectorExitY.addEventListener('input', updateExitFromInspector);
    this.elements.inspectorExitRadius.addEventListener('input', updateExitFromInspector);

    // Canvas Mouse events
    this.canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e));
    this.canvas.addEventListener('mousedown', (e) => this.handleMouseDown(e));
    this.canvas.addEventListener('mouseup', () => this.handleMouseUp());
    this.canvas.addEventListener('mouseleave', () => {
      this.mouseLogicalPos = null;
      this.hoveredEdge = null;
      this.hoveredEntity = null;
      if (this.isDragging) this.handleMouseUp();
      this.render();
    });
  }

  private screenToLogical(e: MouseEvent): RoomPointDto {
    const rect = this.canvas.getBoundingClientRect();
    const scaleX = this.canvas.width / rect.width;
    const scaleY = this.canvas.height / rect.height;
    return {
      x: (e.clientX - rect.left) * scaleX,
      y: (e.clientY - rect.top) * scaleY
    };
  }

  private handleMouseMove(e: MouseEvent): void {
    const pos = this.screenToLogical(e);
    this.mouseLogicalPos = pos;

    if (this.mode === 'preview') return;

    if (this.isDragging) {
      if (this.dragPos) {
        this.dragPos = { x: Math.round(pos.x), y: Math.round(pos.y) };
        this.updateInspectorCoordinates(this.dragPos.x, this.dragPos.y);
      } else if (this.dragStartLogicalPos) {
        this.dragOffset = {
          dx: Math.round(pos.x - this.dragStartLogicalPos.x),
          dy: Math.round(pos.y - this.dragStartLogicalPos.y)
        };
      }
      this.render();
      return;
    }

    const room = this.state.getRoom();
    const hitVertex = this.findVertexAt(pos, room);
    const hitEntity = this.findEntityAt(pos, room);
    this.hoveredEntity = hitEntity;

    if (!hitVertex && !hitEntity && this.currentTool === 'select') {
      this.hoveredEdge = this.findEdgeAt(pos, room);
    } else {
      this.hoveredEdge = null;
    }

    this.updateCursor();
    this.render();
  }

  private updateCursor(): void {
    if (this.mode === 'preview') {
      this.canvas.style.cursor = 'pointer';
      return;
    }

    if (this.isDragging) {
      this.canvas.style.cursor = 'grabbing';
      return;
    }

    if (this.currentTool === 'select') {
      if (this.hoveredEntity || (this.mouseLogicalPos && this.findVertexAt(this.mouseLogicalPos, this.state.getRoom()))) {
        this.canvas.style.cursor = 'grab';
      } else if (this.hoveredEdge) {
        this.canvas.style.cursor = 'copy';
      } else {
        this.canvas.style.cursor = 'default';
      }
    } else {
      this.canvas.style.cursor = 'crosshair';
    }
  }

  private handleMouseDown(e: MouseEvent): void {
    const pos = this.screenToLogical(e);

    if (this.mode === 'preview') {
      this.handlePreviewClick(pos);
      return;
    }

    const room = this.state.getRoom();

    if (this.currentTool === 'select') {
      // 1. Check if clicking on an existing vertex
      const hitVertex = this.findVertexAt(pos, room);
      if (hitVertex) {
        this.state.setSelection({
          type: 'vertex',
          polyType: hitVertex.polyType,
          polyIndex: hitVertex.polyIndex,
          vertexIndex: hitVertex.vertexIndex
        });
        this.isDragging = true;
        this.dragPos = { ...pos };
        this.updateInspector();
        this.updateCursor();
        this.render();
        return;
      }

      // 2. Check if clicking on a spawn
      const hitSpawn = room.spawns.find(s => distance(s.position, pos) <= 12);
      if (hitSpawn) {
        this.state.setSelection({ type: 'spawn', spawnId: hitSpawn.id });
        this.isDragging = true;
        this.dragPos = { ...pos };
        this.updateInspector();
        this.updateCursor();
        this.render();
        return;
      }

      // 3. Check if clicking on an exit
      const hitExit = (room.exits ?? []).find(ex => distance(ex.position, pos) <= 15);
      if (hitExit) {
        this.state.setSelection({ type: 'exit', exitId: hitExit.id });
        this.isDragging = true;
        this.dragPos = { ...pos };
        this.updateInspector();
        this.updateCursor();
        this.render();
        return;
      }

      // 4. Check if clicking on an edge -> split & start dragging!
      const hitEdge = this.findEdgeAt(pos, room);
      if (hitEdge) {
        this.state.insertVertexOnEdge(
          hitEdge.polyType,
          hitEdge.polyIndex,
          hitEdge.edgeIndex,
          hitEdge.splitPoint.x,
          hitEdge.splitPoint.y
        );
        this.isDragging = true;
        this.dragPos = { ...hitEdge.splitPoint };
        this.hoveredEdge = null;
        this.updateInspector();
        this.updateUnsavedBadge();
        this.updateCursor();
        this.render();
        return;
      }

      // 5. Check if clicking inside an obstacle polygon -> select whole obstacle & start drag!
      if (room.obstacles) {
        for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
          const obs = room.obstacles[oIdx];
          if (obs && obs.points.length >= 3 && isPointInPolygon(pos, obs.points)) {
            this.state.setSelection({
              type: 'polygon',
              polyType: 'obstacle',
              polyIndex: oIdx
            });
            this.isDragging = true;
            this.dragStartLogicalPos = { ...pos };
            this.dragOffset = { dx: 0, dy: 0 };
            this.updateInspector();
            this.updateCursor();
            this.render();
            return;
          }
        }
      }

      // 6. Check if clicking inside a walkable polygon -> select whole floor & start drag!
      for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
        const walk = room.walkable[pIdx];
        if (walk && walk.points.length >= 3 && isPointInPolygon(pos, walk.points)) {
          this.state.setSelection({
            type: 'polygon',
            polyType: 'walkable',
            polyIndex: pIdx
          });
          this.isDragging = true;
          this.dragStartLogicalPos = { ...pos };
          this.dragOffset = { dx: 0, dy: 0 };
          this.updateInspector();
          this.updateCursor();
          this.render();
          return;
        }
      }

      // Clicked on empty space -> clear selection
      this.state.clearSelection();
      this.updateInspector();
      this.render();
    } else if (this.currentTool === 'add-walkable') {
      this.state.addWalkablePoint(pos.x, pos.y);
      this.updateInspector();
      this.updateUnsavedBadge();
      this.setTool('select');
    } else if (this.currentTool === 'add-obstacle') {
      this.state.addObstacle(pos.x, pos.y);
      this.updateInspector();
      this.updateUnsavedBadge();
      this.setTool('select');
    } else if (this.currentTool === 'add-spawn') {
      const spawnId = prompt('Spawn-ID eingeben:', 'entry') || 'entry';
      this.state.addOrUpdateSpawn(spawnId, pos.x, pos.y);
      this.updateInspector();
      this.updateUnsavedBadge();
      this.setTool('select');
    } else if (this.currentTool === 'add-exit') {
      const exitId = prompt('Ausgangs-ID eingeben:', 'to-other') || 'to-other';
      const targetRoom = prompt('Zielraum-ID:', 'lounge') || 'lounge';
      const targetSpawn = prompt('Zielspawn-ID:', 'entry') || 'entry';
      this.state.addOrUpdateExit({
        id: exitId,
        position: { x: Math.round(pos.x), y: Math.round(pos.y) },
        radius: 35,
        target: { room: targetRoom, spawn: targetSpawn }
      });
      this.updateInspector();
      this.updateUnsavedBadge();
      this.setTool('select');
    }
  }

  private handleMouseUp(): void {
    if (this.isDragging) {
      const sel = this.state.getSelection();
      if (sel.type === 'polygon' && sel.polyType && sel.polyIndex !== undefined && this.dragOffset) {
        if (this.dragOffset.dx !== 0 || this.dragOffset.dy !== 0) {
          this.state.translatePolygon(sel.polyType, sel.polyIndex, this.dragOffset.dx, this.dragOffset.dy);
          this.updateUnsavedBadge();
        }
      } else if (this.dragPos) {
        if (sel.type === 'vertex' && sel.polyType && sel.polyIndex !== undefined && sel.vertexIndex !== undefined) {
          this.state.moveVertex(sel.polyType, sel.polyIndex, sel.vertexIndex, this.dragPos.x, this.dragPos.y);
        } else if (sel.type === 'spawn' && sel.spawnId) {
          this.state.moveSpawn(sel.spawnId, this.dragPos.x, this.dragPos.y);
        } else if (sel.type === 'exit' && sel.exitId) {
          this.state.moveExit(sel.exitId, this.dragPos.x, this.dragPos.y);
        }
        this.updateUnsavedBadge();
      }
      this.updateInspector();
    }

    this.isDragging = false;
    this.dragPos = null;
    this.dragStartLogicalPos = null;
    this.dragOffset = null;
    this.updateCursor();
    this.render();
  }

  private handlePreviewClick(pos: Point): void {
    if (!this.previewCharacter || !this.previewRenderer) return;

    const runtimeRoom = this.state.toRuntimeRoomDefinition();
    const isWalkable = isPointWalkable(pos, runtimeRoom.geometry.outerBoundary, runtimeRoom.geometry.obstacles);

    if (isWalkable) {
      this.previewRenderer.addClickMarker(pos.x, pos.y, 'valid');
      assignPathToCharacter(this.previewCharacter, [
        { x: this.previewCharacter.position.x, y: this.previewCharacter.position.y },
        { x: pos.x, y: pos.y }
      ]);
    } else {
      this.previewRenderer.addClickMarker(pos.x, pos.y, 'invalid');
    }
  }

  private findVertexAt(
    pos: RoomPointDto,
    room: RoomYamlDto
  ): { polyType: 'walkable' | 'obstacle'; polyIndex: number; vertexIndex: number } | null {
    // Check walkable
    for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
      const poly = room.walkable[pIdx];
      if (!poly) continue;
      for (let vIdx = 0; vIdx < poly.points.length; vIdx++) {
        if (distance(poly.points[vIdx]!, pos) <= 8) {
          return { polyType: 'walkable', polyIndex: pIdx, vertexIndex: vIdx };
        }
      }
    }

    // Check obstacles
    if (room.obstacles) {
      for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
        const poly = room.obstacles[oIdx];
        if (!poly) continue;
        for (let vIdx = 0; vIdx < poly.points.length; vIdx++) {
          if (distance(poly.points[vIdx]!, pos) <= 8) {
            return { polyType: 'obstacle', polyIndex: oIdx, vertexIndex: vIdx };
          }
        }
      }
    }

    return null;
  }

  private findEntityAt(
    pos: RoomPointDto,
    room: RoomYamlDto
  ): { type: 'spawn' | 'exit'; id: string } | null {
    const hitSpawn = room.spawns.find(s => distance(s.position, pos) <= 12);
    if (hitSpawn) return { type: 'spawn', id: hitSpawn.id };

    const hitExit = (room.exits ?? []).find(e => distance(e.position, pos) <= 15);
    if (hitExit) return { type: 'exit', id: hitExit.id };

    return null;
  }

  private findEdgeAt(pos: RoomPointDto, room: RoomYamlDto): HoveredEdge | null {
    let closestEdge: HoveredEdge | null = null;
    let minDistance = 8.0; // 8px threshold

    const checkPoly = (polyType: 'walkable' | 'obstacle', polyIndex: number, points: RoomPointDto[]) => {
      const count = points.length;
      if (count < 2) return;

      for (let i = 0; i < count; i++) {
        const p1 = points[i]!;
        const p2 = points[(i + 1) % count]!;

        const proj = closestPointOnSegment(pos, p1, p2);
        if (proj.distance < minDistance) {
          // Ignore if too close to an existing endpoint (within 6px) to avoid accidental splits over vertices
          if (distance(proj.point, p1) > 6 && distance(proj.point, p2) > 6) {
            minDistance = proj.distance;
            closestEdge = {
              polyType,
              polyIndex,
              edgeIndex: i,
              splitPoint: { x: Math.round(proj.point.x), y: Math.round(proj.point.y) }
            };
          }
        }
      }
    };

    for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
      const poly = room.walkable[pIdx];
      if (poly) checkPoly('walkable', pIdx, poly.points);
    }

    if (room.obstacles) {
      for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
        const poly = room.obstacles[oIdx];
        if (poly) checkPoly('obstacle', oIdx, poly.points);
      }
    }

    return closestEdge;
  }

  private render(): void {
    if (this.mode === 'preview') return;

    const room = this.state.getRoom();
    const selection = this.state.getSelection();

    const hovered = this.mouseLogicalPos ? this.findVertexAt(this.mouseLogicalPos, room) : null;
    this.editorRenderer.render(
      room,
      selection,
      this.mouseLogicalPos,
      this.dragPos,
      hovered,
      this.hoveredEdge,
      this.hoveredEntity,
      this.dragOffset
    );
  }

  private updateFormFromState(): void {
    const room = this.state.getRoom();
    this.elements.inputId.value = room.id;
    this.elements.inputName.value = room.name;
    this.elements.inputBg.value = room.background;
    this.elements.inputWidth.value = String(room.size.width);
    this.elements.inputHeight.value = String(room.size.height);
    this.elements.inputNotes.value = room.notes ?? '';
    this.updateUnsavedBadge();
  }

  private updateInspector(): void {
    const sel = this.state.getSelection();
    const room = this.state.getRoom();

    this.elements.inspectorEmpty.style.display = 'none';
    this.elements.inspectorVertex.style.display = 'none';
    this.elements.inspectorPolygon.style.display = 'none';
    this.elements.inspectorSpawn.style.display = 'none';
    this.elements.inspectorExit.style.display = 'none';

    if (sel.type === 'polygon' && sel.polyType && sel.polyIndex !== undefined) {
      const polys = sel.polyType === 'walkable' ? room.walkable : (room.obstacles ?? []);
      const poly = polys[sel.polyIndex];
      if (poly) {
        this.elements.inspectorPolygon.style.display = 'block';
        if (sel.polyType === 'obstacle') {
          this.elements.inspectorPolygonBadge.className = 'inspector-badge badge-obstacle';
          this.elements.inspectorPolygonBadge.textContent = '🔴 Hindernis-Fläche';
        } else {
          this.elements.inspectorPolygonBadge.className = 'inspector-badge';
          this.elements.inspectorPolygonBadge.textContent = '🟢 Begehbare Boden-Fläche';
        }
        this.elements.inspectorPolygonId.value = poly.id;
        this.elements.inspectorPolygonPointsCount.value = `${poly.points.length} Eckpunkte`;
      } else {
        this.elements.inspectorEmpty.style.display = 'block';
      }
    } else if (sel.type === 'vertex' && sel.vertexIndex !== undefined) {
      const polys = sel.polyType === 'walkable' ? room.walkable : (room.obstacles ?? []);
      const poly = polys[sel.polyIndex ?? 0];
      const pt = poly?.points[sel.vertexIndex];

      if (pt) {
        this.elements.inspectorVertex.style.display = 'block';
        if (sel.polyType === 'obstacle') {
          this.elements.inspectorVertexBadge.className = 'inspector-badge badge-obstacle';
          this.elements.inspectorVertexBadge.textContent = `🔴 Hindernispunkt #${sel.vertexIndex + 1}`;
        } else {
          this.elements.inspectorVertexBadge.className = 'inspector-badge';
          this.elements.inspectorVertexBadge.textContent = `🟢 Bodenpunkt #${sel.vertexIndex + 1}`;
        }
        this.elements.inspectorVertexX.value = String(pt.x);
        this.elements.inspectorVertexY.value = String(pt.y);
      } else {
        this.elements.inspectorEmpty.style.display = 'block';
      }
    } else if (sel.type === 'spawn' && sel.spawnId) {
      const spawn = room.spawns.find(s => s.id === sel.spawnId);
      if (spawn) {
        this.elements.inspectorSpawn.style.display = 'block';
        this.elements.inspectorSpawnId.value = spawn.id;
        this.elements.inspectorSpawnX.value = String(spawn.position.x);
        this.elements.inspectorSpawnY.value = String(spawn.position.y);
      } else {
        this.elements.inspectorEmpty.style.display = 'block';
      }
    } else if (sel.type === 'exit' && sel.exitId) {
      const exit = (room.exits ?? []).find(e => e.id === sel.exitId);
      if (exit) {
        this.elements.inspectorExit.style.display = 'block';
        this.elements.inspectorExitId.value = exit.id;
        this.elements.inspectorExitRoom.value = exit.target.room;
        this.elements.inspectorExitTargetSpawn.value = exit.target.spawn;
        this.elements.inspectorExitLabel.value = exit.label ?? '';
        this.elements.inspectorExitX.value = String(exit.position.x);
        this.elements.inspectorExitY.value = String(exit.position.y);
        this.elements.inspectorExitRadius.value = String(exit.radius || 35);
      } else {
        this.elements.inspectorEmpty.style.display = 'block';
      }
    } else {
      this.elements.inspectorEmpty.style.display = 'block';
    }
  }

  public async loadBackgroundsList(): Promise<void> {
    try {
      const response = await fetch('/api/editor/backgrounds');
      if (!response.ok) return;

      const overview = (await response.json()) as BackgroundsOverviewDto;
      this.backgroundsOverview = overview;

      const displayPath = overview.filesystemDirectory || overview.relativePath || 'assets/images';
      if (this.elements.bgFsHintPath) {
        this.elements.bgFsHintPath.textContent = displayPath;
      }
      if (this.elements.modalFsPath) {
        this.elements.modalFsPath.textContent = displayPath;
      }
      if (this.elements.bgCount) {
        this.elements.bgCount.textContent = String(overview.backgrounds.length);
      }

      this.renderBackgroundGallery();
    } catch {
      // Ignore background load errors
    }
  }

  private renderBackgroundGallery(): void {
    const grid = this.elements.bgGalleryGrid;
    if (!grid || !this.backgroundsOverview) return;

    grid.innerHTML = '';
    const currentBg = this.state.getRoom().background;

    for (const bg of this.backgroundsOverview.backgrounds) {
      const isCurrent = bg.path === currentBg || currentBg.endsWith(bg.name);
      const card = document.createElement('div');
      card.className = `bg-gallery-card ${isCurrent ? 'active' : ''}`;
      card.dataset.path = bg.path;
      card.title = `Klicken, um '${bg.name}' als Hintergrund zu wählen`;

      const sizeKb = Math.round(bg.sizeBytes / 1024);

      card.innerHTML = `
        <div class="bg-thumbnail-wrap">
          <img class="bg-thumbnail-img" src="${bg.path}" alt="${bg.name}" loading="lazy">
          ${isCurrent ? '<span class="bg-active-tag">✓ Aktiv</span>' : ''}
        </div>
        <div class="bg-card-info">
          <div class="bg-card-title">${bg.name}</div>
          <div class="bg-card-meta">${sizeKb} KB</div>
        </div>
      `;

      card.addEventListener('click', () => {
        this.selectBackground(bg.path);
        this.closeBackgroundModal();
      });

      grid.appendChild(card);
    }
  }

  public openBackgroundModal(): void {
    void this.loadBackgroundsList();
    if (typeof this.elements.modalBgPicker.showModal === 'function') {
      this.elements.modalBgPicker.showModal();
    } else {
      this.elements.modalBgPicker.style.display = 'block';
    }
  }

  public closeBackgroundModal(): void {
    if (typeof this.elements.modalBgPicker.close === 'function') {
      this.elements.modalBgPicker.close();
    } else {
      this.elements.modalBgPicker.style.display = 'none';
    }
  }

  public selectBackground(path: string): void {
    this.elements.inputBg.value = path;
    this.state.updateMetadata({ background: path });
    this.editorRenderer.setBackground(path);
    this.updateUnsavedBadge();
    this.render();
    this.renderBackgroundGallery();
    this.setStatus(`Hintergrund '${path}' ausgewählt.`, 'info');
  }

  public async uploadBackgroundFile(file: File): Promise<void> {
    if (!file) return;

    this.setStatus(`Lade '${file.name}' hoch...`, 'info');
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/editor/backgrounds', {
        method: 'POST',
        body: formData
      });

      const res = await response.json();
      if (!response.ok || !res.success) {
        this.setStatus(`Fehler beim Upload: ${res.error ?? 'Unbekannter Fehler'}`, 'error');
        alert(`Fehler beim Upload: ${res.error ?? 'Unbekannter Fehler'}`);
        return;
      }

      this.setStatus(`Hintergrundbild '${res.name}' erfolgreich hochgeladen!`, 'success');
      this.selectBackground(res.path);
      await this.loadBackgroundsList();
    } catch (err) {
      this.setStatus(`Netzwerkfehler beim Upload: ${String(err)}`, 'error');
    }
  }

  public async copyFilesystemPath(): Promise<void> {
    const pathText = this.elements.modalFsPath.textContent ?? '';
    if (!pathText) return;

    try {
      await navigator.clipboard.writeText(pathText);
      const originalText = this.elements.btnCopyFsPath.textContent;
      this.elements.btnCopyFsPath.textContent = '✓ Kopiert!';
      setTimeout(() => {
        this.elements.btnCopyFsPath.textContent = originalText;
      }, 2000);
    } catch {
      // Fallback
    }
  }

  private updateInspectorCoordinates(x: number, y: number): void {
    const sel = this.state.getSelection();
    if (sel.type === 'vertex') {
      this.elements.inspectorVertexX.value = String(x);
      this.elements.inspectorVertexY.value = String(y);
    } else if (sel.type === 'spawn') {
      this.elements.inspectorSpawnX.value = String(x);
      this.elements.inspectorSpawnY.value = String(y);
    } else if (sel.type === 'exit') {
      this.elements.inspectorExitX.value = String(x);
      this.elements.inspectorExitY.value = String(y);
    }
  }

  private updateToolButtons(): void {
    const tools: Record<EditorTool, HTMLButtonElement> = {
      select: this.elements.toolSelect,
      'add-walkable': this.elements.toolWalkable,
      'add-obstacle': this.elements.toolObstacle,
      'add-spawn': this.elements.toolSpawn,
      'add-exit': this.elements.toolExit
    };

    for (const [toolName, btn] of Object.entries(tools)) {
      if (toolName === this.currentTool) {
        btn.classList.add('active');
      } else {
        btn.classList.remove('active');
      }
    }
  }

  private updateUnsavedBadge(): void {
    if (this.state.isDirty()) {
      this.elements.unsavedBadge.style.display = 'inline-block';
    } else {
      this.elements.unsavedBadge.style.display = 'none';
    }
  }

  private setStatus(text: string, type: 'info' | 'success' | 'error'): void {
    this.elements.statusBanner.textContent = text;
    this.elements.statusBanner.className = `status-banner status-${type}`;
  }

  private showValidationErrors(errors: string[]): void {
    this.elements.errorList.innerHTML = '';
    for (const err of errors) {
      const li = document.createElement('li');
      li.textContent = err;
      this.elements.errorList.appendChild(li);
    }
    this.elements.errorList.style.display = 'block';
  }

  private clearValidationErrors(): void {
    this.elements.errorList.innerHTML = '';
    this.elements.errorList.style.display = 'none';
  }
}
