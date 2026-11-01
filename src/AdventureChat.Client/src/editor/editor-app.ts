import { distance, isPointWalkable } from '../math.js';
import { RoomRenderer } from '../rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from '../simulation/movement.js';
import type { Character, Point } from '../types.js';
import { EditorRenderer } from './editor-renderer.js';
import { EditorState } from './editor-state.js';
import type {
  EditorMode,
  EditorTool,
  RoomPointDto,
  RoomSummaryDto,
  RoomYamlDto
} from './editor-types.js';

export class EditorApp {
  private readonly state: EditorState;
  private readonly editorRenderer: EditorRenderer;
  private previewRenderer: RoomRenderer | null = null;

  private mode: EditorMode = 'edit';
  private currentTool: EditorTool = 'select';

  private mouseLogicalPos: RoomPointDto | null = null;
  private isDragging = false;
  private dragPos: RoomPointDto | null = null;

  // Preview character state
  private previewCharacter: Character | null = null;
  private previewAnimationId: number | null = null;
  private lastFrameTime = performance.now();

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly elements: {
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
    }
  ) {
    this.state = new EditorState();
    this.editorRenderer = new EditorRenderer(this.canvas);

    this.bindEvents();
    this.updateFormFromState();
    this.render();
  }

  public async init(): Promise<void> {
    await this.loadRoomList(false);
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
      this.setStatus('Neuer Raum erstellt.', 'info');
      this.render();
    });

    this.elements.roomSelect.addEventListener('change', () => {
      const id = this.elements.roomSelect.value;
      if (id) void this.loadRoom(id);
    });

    this.elements.btnDeleteVertex.addEventListener('click', () => {
      if (this.state.deleteSelectedVertex()) {
        this.updateUnsavedBadge();
        this.render();
      }
    });

    // Tool selection buttons
    this.elements.toolSelect.addEventListener('click', () => this.setTool('select'));
    this.elements.toolWalkable.addEventListener('click', () => this.setTool('add-walkable'));
    this.elements.toolObstacle.addEventListener('click', () => this.setTool('add-obstacle'));
    this.elements.toolSpawn.addEventListener('click', () => this.setTool('add-spawn'));
    this.elements.toolExit.addEventListener('click', () => this.setTool('add-exit'));

    // Input bindings
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

    // Canvas Mouse events
    this.canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e));
    this.canvas.addEventListener('mousedown', (e) => this.handleMouseDown(e));
    this.canvas.addEventListener('mouseup', () => this.handleMouseUp());
    this.canvas.addEventListener('mouseleave', () => {
      this.mouseLogicalPos = null;
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

    if (this.isDragging && this.dragPos) {
      this.dragPos = { x: Math.round(pos.x), y: Math.round(pos.y) };
      this.render();
      return;
    }

    this.render();
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
        this.render();
        return;
      }

      // 2. Check if clicking on a spawn
      const hitSpawn = room.spawns.find(s => distance(s.position, pos) <= 12);
      if (hitSpawn) {
        this.state.setSelection({ type: 'spawn', spawnId: hitSpawn.id });
        this.isDragging = true;
        this.dragPos = { ...pos };
        this.render();
        return;
      }

      // 3. Check if clicking on an exit
      const hitExit = (room.exits ?? []).find(ex => distance(ex.position, pos) <= 15);
      if (hitExit) {
        this.state.setSelection({ type: 'exit', exitId: hitExit.id });
        this.isDragging = true;
        this.dragPos = { ...pos };
        this.render();
        return;
      }

      // Clicked on empty space -> clear selection
      this.state.clearSelection();
      this.render();
    } else if (this.currentTool === 'add-walkable') {
      this.state.addWalkablePoint(pos.x, pos.y);
      this.updateUnsavedBadge();
      this.render();
    } else if (this.currentTool === 'add-obstacle') {
      this.state.addObstaclePoint(pos.x, pos.y);
      this.updateUnsavedBadge();
      this.render();
    } else if (this.currentTool === 'add-spawn') {
      const spawnId = prompt('Spawn-ID eingeben:', 'entry') || 'entry';
      this.state.addOrUpdateSpawn(spawnId, pos.x, pos.y);
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
      this.updateUnsavedBadge();
      this.setTool('select');
    }
  }

  private handleMouseUp(): void {
    if (this.isDragging && this.dragPos) {
      const sel = this.state.getSelection();
      if (sel.type === 'vertex' && sel.polyType && sel.polyIndex !== undefined && sel.vertexIndex !== undefined) {
        this.state.moveVertex(sel.polyType, sel.polyIndex, sel.vertexIndex, this.dragPos.x, this.dragPos.y);
      } else if (sel.type === 'spawn' && sel.spawnId) {
        this.state.moveSpawn(sel.spawnId, this.dragPos.x, this.dragPos.y);
      } else if (sel.type === 'exit' && sel.exitId) {
        this.state.moveExit(sel.exitId, this.dragPos.x, this.dragPos.y);
      }
      this.updateUnsavedBadge();
    }

    this.isDragging = false;
    this.dragPos = null;
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

  private render(): void {
    if (this.mode === 'preview') return;

    const room = this.state.getRoom();
    const selection = this.state.getSelection();

    const hovered = this.mouseLogicalPos ? this.findVertexAt(this.mouseLogicalPos, room) : null;
    this.editorRenderer.render(room, selection, this.mouseLogicalPos, this.dragPos, hovered);
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
