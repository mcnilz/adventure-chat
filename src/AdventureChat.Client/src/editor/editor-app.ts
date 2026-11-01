import { EditorApi } from './core/editor-api.js';
import { EditorState } from './core/editor-state.js';
import type {
  EditorMode,
  EditorTool
} from './core/editor-types.js';
import { EditorCanvasInput } from './canvas/editor-canvas-input.js';
import { EditorPreview } from './canvas/editor-preview.js';
import { EditorRenderer } from './canvas/editor-renderer.js';
import { EditorBackgroundModal } from './ui/editor-background-modal.js';
import { EditorExitDialog } from './ui/editor-exit-dialog.js';
import { EditorInspector } from './ui/editor-inspector.js';
import { EditorSidebarForm } from './ui/editor-sidebar-form.js';
import { EditorToolbar } from './ui/editor-toolbar.js';

export interface EditorElements {
  roomSelect: HTMLSelectElement;
  btnNewRoom: HTMLButtonElement;
  btnSave: HTMLButtonElement;
  btnValidate: HTMLButtonElement;
  btnValidateWorld?: HTMLButtonElement | null;
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
  errorsSection: HTMLElement;
  errorList: HTMLElement;
  unsavedBadge: HTMLElement;
  // Inspector
  inspectorEmpty: HTMLElement;
  inspectorVertex: HTMLElement;
  inspectorVertexBadge: HTMLElement;
  inspectorVertexX: HTMLInputElement;
  inspectorVertexY: HTMLInputElement;
  btnInspectorDeleteVertex: HTMLButtonElement;
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
  inspectorExitRoom: HTMLSelectElement;
  inspectorExitTargetSpawn: HTMLSelectElement;
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
  // Exit Dialog
  modalExitDialog: HTMLDialogElement;
  btnCloseExitModal: HTMLButtonElement;
  btnCancelExitModal: HTMLButtonElement;
  btnConfirmExitModal: HTMLButtonElement;
  modalExitId: HTMLInputElement;
  modalExitRoom: HTMLSelectElement;
  modalExitTargetSpawn: HTMLSelectElement;
  modalExitLabel: HTMLInputElement;
  modalExitRadius: HTMLInputElement;
}

export class EditorApp {
  private readonly state: EditorState;
  private readonly editorRenderer: EditorRenderer;
  private readonly preview: EditorPreview;
  private readonly exitDialog: EditorExitDialog;
  private readonly inspector: EditorInspector;
  private readonly bgModal: EditorBackgroundModal;
  private readonly toolbar: EditorToolbar;
  private readonly form: EditorSidebarForm;
  private readonly canvasInput: EditorCanvasInput;

  private mode: EditorMode = 'edit';
  private currentTool: EditorTool = 'select';

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly elements: EditorElements
  ) {
    this.state = new EditorState();
    this.editorRenderer = new EditorRenderer(this.canvas);
    this.preview = new EditorPreview(this.canvas);

    this.exitDialog = new EditorExitDialog(
      this.elements,
      (msg, type) => this.setStatus(msg, type)
    );

    this.inspector = new EditorInspector(
      this.elements,
      this.exitDialog,
      () => this.handleInspectorInput(),
      () => this.deleteSelected()
    );

    this.bgModal = new EditorBackgroundModal(
      this.elements,
      (bgPath) => {
        this.elements.inputBg.value = bgPath;
        this.state.updateMetadata({ background: bgPath });
        this.editorRenderer.setBackground(bgPath);
        this.render();
      },
      (msg, type) => this.setStatus(msg, type),
      () => this.state.getRoom().background
    );

    this.toolbar = new EditorToolbar(this.elements, {
      onRoomSelect: (roomId) => this.loadRoom(roomId).catch(() => {}),
      onNewRoom: () => this.newRoom(),
      onSave: () => this.saveRoom().catch(() => {}),
      onValidate: () => this.validateRoom().catch(() => {}),
      onValidateWorld: () => this.validateWorld().catch(() => {}),
      onToggleMode: () => this.toggleMode(),
      onDeleteSelected: () => this.deleteSelected(),
      onToolSelect: (tool) => this.setTool(tool)
    });

    this.form = new EditorSidebarForm(
      this.elements,
      () => {
        this.form.applyToRoom(this.state.getRoom());
        this.state.updateMetadata({});
        this.form.setUnsaved(this.state.isDirty());
      },
      (bg) => {
        this.editorRenderer.setBackground(bg);
        this.render();
      }
    );

    this.canvasInput = new EditorCanvasInput(
      this.canvas,
      this.state,
      {
        onStateChange: () => this.syncStateAndRender(),
        onToolAutoReset: () => {
          this.setTool('select');
          this.syncStateAndRender();
        },
        onHoverChange: () => this.render(),
        onAddExitRequest: (pos) => {
          this.exitDialog.openForNewExit(pos, this.state.getRoom().id, (exitDto) => {
            this.state.addExit(exitDto);
            this.setTool('select');
            this.syncStateAndRender();
          }).catch(() => {});
        }
      },
      () => this.currentTool,
      () => this.mode === 'preview',
      (x, y) => this.preview.handleCanvasClick(x, y, this.state.toRoomDefinition())
    );

    window.addEventListener('beforeunload', (e) => {
      if (this.state.isDirty()) e.preventDefault();
    });

    this.syncAllViews();
    this.form.clearValidationErrors();
    this.render();

    // Hook for E2E testing
    (window as unknown as { __ROOM_EDITOR__: EditorApp }).__ROOM_EDITOR__ = this;
  }

  public async init(): Promise<void> {
    await this.bgModal.loadBackgroundsOverview();
    await this.exitDialog.initRooms();
    await this.loadRoomList();
  }

  public getState(): EditorState {
    return this.state;
  }

  public getMode(): EditorMode {
    return this.mode;
  }

  public getTool(): EditorTool {
    return this.currentTool;
  }

  public setTool(tool: EditorTool): void {
    this.currentTool = tool;
    this.toolbar.updateToolButtons(tool);
    this.render();
  }

  public setStatus(msg: string, type: 'info' | 'success' | 'error' = 'info'): void {
    this.elements.statusBanner.textContent = msg;
    this.elements.statusBanner.className = `status-banner status-${type}`;
  }

  public async loadRoomList(selectedRoomId?: string): Promise<void> {
    try {
      const rooms = await EditorApi.fetchRooms();
      this.elements.roomSelect.innerHTML = '';

      for (const r of rooms) {
        const opt = document.createElement('option');
        opt.value = r.id;
        opt.textContent = `${r.name} (${r.id})`;
        this.elements.roomSelect.appendChild(opt);
      }

      if (selectedRoomId) {
        this.elements.roomSelect.value = selectedRoomId;
      } else if (rooms.length > 0) {
        await this.loadRoom(rooms[0]!.id);
      } else {
        this.newRoom();
      }
    } catch (err) {
      this.setStatus(`Fehler beim Laden der Räume: ${String(err)}`, 'error');
    }
  }

  public async loadRoom(id: string): Promise<void> {
    try {
      this.setStatus(`Lade Raum '${id}'...`, 'info');
      const room = await EditorApi.fetchRoom(id);
      this.state.setRoom(room);
      this.syncAllViews();
      this.form.clearValidationErrors();
      this.setStatus(`Raum '${room.name}' geladen.`, 'success');
      this.render();
    } catch (err) {
      this.setStatus(`Fehler beim Laden von '${id}': ${String(err)}`, 'error');
    }
  }

  public newRoom(): void {
    const def = this.state.createDefaultRoom();
    const uniqueId = `room-${Math.random().toString(36).substring(2, 10)}`;
    def.id = uniqueId;
    def.name = 'Neuer Raum';

    this.state.setRoom(def);
    this.syncAllViews();
    this.form.clearValidationErrors();
    this.setStatus('Neuer Raum erstellt.', 'info');
    this.render();
  }

  public async saveRoom(): Promise<boolean> {
    this.form.applyToRoom(this.state.getRoom());
    this.form.clearValidationErrors();

    const room = this.state.getRoom();
    this.setStatus(`Speichere Raum '${room.id}'...`, 'info');

    try {
      const result = await EditorApi.saveRoom(room);
      if (!result.success) {
        const errors = result.errors.length > 0 ? result.errors : ['Speichern fehlgeschlagen.'];
        this.form.showValidationErrors(errors);
        this.setStatus('Validierungsfehler beim Speichern.', 'error');
        return false;
      }

      this.state.markClean();
      this.form.setUnsaved(false);
      this.form.clearValidationErrors();
      await this.loadRoomList(room.id);
      this.exitDialog.updateCachedRoom(room);
      await this.exitDialog.refreshRooms();
      this.setStatus(`Raum '${room.name}' (${room.id}) erfolgreich atomar gespeichert!`, 'success');
      return true;
    } catch (err) {
      this.setStatus(`Fehler beim Speichern: ${String(err)}`, 'error');
      return false;
    }
  }

  public async validateRoom(): Promise<boolean> {
    this.form.applyToRoom(this.state.getRoom());
    this.form.clearValidationErrors();

    const room = this.state.getRoom();
    this.setStatus('Validiere Raum-Definition...', 'info');

    try {
      const result = await EditorApi.validateRoom(room);
      if (result.valid) {
        this.form.clearValidationErrors();
        this.setStatus('✅ Validierung erfolgreich! Raumgeometrie und Schema sind gültig.', 'success');
        return true;
      } else {
        this.form.showValidationErrors(result.errors);
        this.setStatus(`⚠️ Validierungsfehler: ${result.errors.length} Problem(e) gefunden.`, 'error');
        return false;
      }
    } catch (err) {
      this.setStatus(`Validierungsfehler: ${String(err)}`, 'error');
      return false;
    }
  }

  public async validateWorld(): Promise<boolean> {
    this.form.clearValidationErrors();
    this.setStatus('Validiere alle Räume und Übergänge der Welt...', 'info');

    try {
      const result = await EditorApi.validateWorld();
      if (result.valid) {
        this.form.clearValidationErrors();
        this.setStatus('✅ Weltweit gültig! Alle Räume, Ausgänge und Ziel-Spawns sind konsistent.', 'success');
        return true;
      } else {
        this.form.showValidationErrors(result.errors);
        this.setStatus(`⚠️ Welt-Validierung: ${result.errors.length} Problem(e) gefunden.`, 'error');
        return false;
      }
    } catch (err) {
      this.setStatus(`Welt-Validierungsfehler: ${String(err)}`, 'error');
      return false;
    }
  }

  public toggleMode(): void {
    if (this.mode === 'edit') {
      this.mode = 'preview';
      this.toolbar.updateModeButton('preview');
      this.setStatus('Vorschaumodus aktiv: Klicke auf die begehbare Fläche, um die Spielfigur zu steuern.', 'info');
      this.preview.start(this.state.toRoomDefinition());
    } else {
      this.mode = 'edit';
      this.toolbar.updateModeButton('edit');
      this.preview.stop();
      this.setStatus('Bearbeitungsmodus aktiv.', 'info');
      this.render();
    }
  }

  public deleteSelected(): void {
    const deleted = this.state.deleteSelected();
    if (deleted) {
      this.syncStateAndRender();
    }
  }

  public render(): void {
    if (this.mode === 'preview') return;

    this.editorRenderer.setBackground(this.state.getRoom().background);
    this.editorRenderer.render(
      this.state.getRoom(),
      this.state.getSelection(),
      this.canvasInput.getMouseLogicalPos(),
      this.canvasInput.getDragPos(),
      this.canvasInput.getHoveredVertex(),
      this.canvasInput.getHoveredEdge(),
      this.canvasInput.getHoveredEntity(),
      this.canvasInput.getDragOffset()
    );
  }

  private handleInspectorInput(): void {
    const room = this.state.getRoom();
    const sel = this.state.getSelection();

    if (sel.type === 'vertex') {
      if (this.inspector.applyVertexChanges(room, sel)) {
        this.syncStateAndRender();
      }
    } else if (sel.type === 'spawn') {
      if (this.inspector.applySpawnChanges(room, sel)) {
        this.syncStateAndRender();
      }
    } else if (sel.type === 'exit') {
      if (this.inspector.applyExitChanges(room, sel)) {
        this.syncStateAndRender();
      }
    }
  }

  private syncStateAndRender(): void {
    const room = this.state.getRoom();
    this.inspector.update(room, this.state.getSelection());
    this.form.setUnsaved(this.state.isDirty());
    this.render();
  }

  private syncAllViews(): void {
    const room = this.state.getRoom();
    this.form.updateFromRoom(room, this.state.isDirty());
    this.editorRenderer.setBackground(room.background);
    this.inspector.update(room, this.state.getSelection());
  }
}
