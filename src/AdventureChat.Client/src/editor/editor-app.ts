import { EditorObjects } from './ui/editor-objects.js';
import { applyObjectAppearances } from '../state/object-appearance.js';
import { blocksExit } from './core/editor-interaction-references.js';
import { roomWithSelectedAppearance } from './core/editor-state-view.js';
import { EditorCamera } from './canvas/editor-camera.js';
import { EditorApi } from './core/editor-api.js';
import { EditorState } from './core/editor-state.js';
import type {
  EditorMode,
  EditorTool
} from './core/editor-types.js';
import { EditorCanvasInput } from './canvas/editor-canvas-input.js';
import { EditorPreview } from './canvas/editor-preview.js';
import { EditorRenderer } from './canvas/editor-renderer.js';
import { EditorBackgroundModal, type BackgroundModalElements } from './ui/editor-background-modal.js';
import { EditorExitDialog, type ExitDialogElements } from './ui/editor-exit-dialog.js';
import { EditorFormat } from './ui/editor-format.js';
import { EditorInspector, type InspectorElements } from './ui/editor-inspector.js';
import { EditorSidebarForm, type SidebarFormElements } from './ui/editor-sidebar-form.js';
import { EditorToolbar, type ToolbarElements } from './ui/editor-toolbar.js';

export interface EditorElements extends ToolbarElements, SidebarFormElements, InspectorElements,
  BackgroundModalElements, ExitDialogElements {
  statusBanner: HTMLElement;
}

export class EditorApp {
  private readonly objectPanel: EditorObjects;
  private readonly format: EditorFormat;
  private readonly camera: EditorCamera;
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
  private saving = false;
  private loadedRoomId: string | null = null;
  private loadRequest = 0;

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly elements: EditorElements
  ) {
    this.state = new EditorState();
    this.format = new EditorFormat();
    canvas.addEventListener("object-loaded", () => this.render());
    this.camera = new EditorCamera(canvas, () => this.render());
    this.editorRenderer = new EditorRenderer(this.canvas, () => this.render());
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
        this.syncStateAndRender();
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
        this.state.updateProperties(this.form.readEdit());
        this.syncStateAndRender();
      },
      (bg) => {
        this.editorRenderer.setBackground(EditorApi.backgroundUrl(bg));
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
      (x, y) => void this.preview.handleCanvasClick(x, y),
      () => this.mode === 'preview' ? this.preview.cameraOffset : this.camera.camera.offset,
      () => this.preview.activeRoom
    );

    window.addEventListener('beforeunload', (e) => {
      if (this.state.isDirty()) e.preventDefault();
    });

    this.objectPanel = new EditorObjects(this.state, () => this.syncStateAndRender(), (m) => this.setStatus(m, "error"));
    this.syncAllViews();
    this.form.clearValidationErrors();
    this.render();

    // Hook for E2E testing
    (window as unknown as { __ROOM_EDITOR__: EditorApp }).__ROOM_EDITOR__ = this;
  }

  public async init(): Promise<void> {
    await this.format.init();
    await this.bgModal.loadBackgroundsOverview();
    await this.exitDialog.initRooms();
    await this.loadRoomList();
    await this.refreshObjectReferences();
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

      if (selectedRoomId !== undefined || this.loadedRoomId !== null) {
        this.elements.roomSelect.value = this.loadedRoomId ?? '';
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
    if (!this.confirmDiscard()) return;
    const request = ++this.loadRequest;
    const version = this.state.getEditVersion();
    try {
      this.setStatus(`Lade Raum '${id}'...`, 'info');
      const room = await EditorApi.fetchRoom(id);
      if (request !== this.loadRequest) return;
      if (version !== this.state.getEditVersion()) {
        this.elements.roomSelect.value = this.loadedRoomId ?? '';
        this.setStatus('Raumwechsel abgebrochen: Der Entwurf wurde inzwischen bearbeitet.', 'info');
        return;
      }
      if (this.mode === 'preview') this.toggleMode();
      this.state.setRoom(room);
      this.format.resetSelection();
      this.loadedRoomId = room.id;
      this.elements.roomSelect.value = room.id;
      this.syncAllViews();
      this.form.clearValidationErrors();
      this.setStatus(`Raum '${room.name}' geladen.`, 'success');
      this.render();
    } catch (err) {
      if (request !== this.loadRequest) return;
      this.elements.roomSelect.value = this.loadedRoomId ?? '';
      this.setStatus(`Fehler beim Laden von '${id}': ${String(err)}`, 'error');
    }
  }

  public newRoom(): void {
    if (!this.confirmDiscard()) return;
    this.loadRequest++;
    if (this.mode === 'preview') this.toggleMode();
    const def = this.state.createDefaultRoom();
    const uniqueId = `room-${Math.random().toString(36).substring(2, 10)}`;
    def.id = uniqueId;
    def.name = 'Neuer Raum';

    this.state.setRoom(def);
    this.format.resetSelection();
    this.loadedRoomId = null;
    this.elements.roomSelect.value = '';
    this.syncAllViews();
    this.form.clearValidationErrors();
    this.setStatus('Neuer Raum erstellt.', 'info');
    this.render();
  }

  public async saveRoom(): Promise<boolean> {
    if (this.saving) return false;
    if (!this.form.validateDimensions() || !this.objectPanel.interactions.validateFields()) return false;
    this.state.updateProperties(this.form.readEdit());
    this.form.setUnsaved(this.state.isDirty());
    this.form.clearValidationErrors();

    const save = this.state.beginSave();
    const room = save.room;
    this.setStatus(`Speichere Raum '${room.id}'...`, 'info');
    this.saving = true;
    this.elements.btnSave.disabled = true;
    this.format.setSaving(true);

    try {
      const result = await EditorApi.saveRoom(room, this.format.upgradeFrom());
      if (result.success && result.formatVersion !== undefined) this.format.showVersion(result.formatVersion);
      if (!this.state.isCurrentDraft(save)) return result.success;
      if (!result.success) {
        const errors = result.errors.length > 0 ? result.errors : ['Speichern fehlgeschlagen.'];
        this.form.showValidationErrors(errors);
        this.objectPanel.interactions.reportValidation(errors);
        this.setStatus('Validierungsfehler beim Speichern.', 'error');
        return false;
      }

      this.state.completeSave(save);
      this.loadedRoomId = room.id;
      this.form.setUnsaved(this.state.isDirty());
      this.form.clearValidationErrors();
      await this.loadRoomList(room.id);
      if (!this.state.isCurrentDraft(save)) return true;
      this.exitDialog.updateCachedRoom(room);
      await this.exitDialog.refreshRooms();
      await this.refreshObjectReferences();
      if (!this.state.isCurrentDraft(save)) return true;
      const remaining = this.state.isDirty() ? ' Weitere Änderungen sind noch ungespeichert.' : '';
      this.setStatus(`Raum '${room.name}' (${room.id}) erfolgreich atomar gespeichert!${remaining}`, 'success');
      return true;
    } catch (err) {
      if (this.state.isCurrentDraft(save)) this.setStatus(`Fehler beim Speichern: ${String(err)}`, 'error');
      return false;
    } finally {
      this.saving = false;
      this.elements.btnSave.disabled = false;
      this.format.setSaving(false);
    }
  }

  public async validateRoom(): Promise<boolean> {
    if (!this.form.validateDimensions() || !this.objectPanel.interactions.validateFields()) return false;
    this.state.updateProperties(this.form.readEdit());
    this.form.setUnsaved(this.state.isDirty());
    this.form.clearValidationErrors();

    const room = this.state.getRoom();
    this.setStatus('Validiere Raum-Definition...', 'info');

    try {
      const result = await EditorApi.validateRoom(room, this.format.targetVersion());
      if (result.valid) {
        this.form.clearValidationErrors();
        this.setStatus('✅ Validierung erfolgreich! Raumgeometrie und Schema sind gültig.', 'success');
        return true;
      } else {
        this.form.showValidationErrors(result.errors);
        this.objectPanel.interactions.reportValidation(result.errors);
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
      const result = await EditorApi.validateRoom(this.state.getRoom(), this.format.targetVersion());
      if (result.valid) {
        this.form.clearValidationErrors();
        this.setStatus('✅ Weltweit gültig! Alle Räume, Ausgänge und Ziel-Spawns sind konsistent.', 'success');
        return true;
      } else {
        this.form.showValidationErrors(result.errors);
        this.objectPanel.interactions.reportValidation(result.errors);
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
      this.camera.configure(this.state.toRoomDefinition(), true);
      this.toolbar.updateModeButton('preview');
      this.setStatus('Vorschaumodus aktiv: Klicke auf die begehbare Fläche, um die Spielfigur zu steuern.', 'info');
      const room = this.state.toRoomDefinition();
      const states = (room.objects ?? []).flatMap(o => o.interaction ? [{ objectId: o.id, stateId: o.interaction.initialState }] : []);
      const objects = applyObjectAppearances(room.objects ?? [], states).map(o => ({ ...o, image: EditorApi.backgroundUrl(o.image) }));
      this.preview.start({ ...room, objects, backgroundUrl: EditorApi.backgroundUrl(room.backgroundUrl) }, this.state.getRoom(), this.format.targetVersion());
    } else {
      this.mode = 'edit';
      this.toolbar.updateModeButton('edit');
      this.preview.stop();
      this.setStatus('Bearbeitungsmodus aktiv.', 'info');
      this.render();
    }
  }

  public deleteSelected(): void {
    if (!this.objectPanel.canDeleteSelected()) return;
    const selection = this.state.getSelection();
    if (selection.type === 'exit' && selection.exitId && blocksExit(this.state.getRoom(), selection.exitId)) {
      this.setStatus('Der Ausgang wird in einem Objektzustand gesperrt.', 'error');
      return;
    }
    const deleted = this.state.deleteSelected();
    if (deleted) {
      this.syncStateAndRender();
    }
  }

  public render(): void {
    if (this.mode === 'preview') return;
    this.camera.configure(this.state.toRoomDefinition(), false);

    this.objectPanel.update();
    const room = this.state.getRoom();
    const selection = this.state.getSelection();
    const state = this.objectPanel.interactions.activeAppearance();
    this.editorRenderer.setBackground(EditorApi.backgroundUrl(room.background));
    this.editorRenderer.render(
      roomWithSelectedAppearance(room, selection, state),
      selection,
      this.canvasInput.getMouseLogicalPos(),
      this.canvasInput.getDragPos(),
      this.canvasInput.getHoveredVertex(),
      this.canvasInput.getHoveredEdge(),
      this.canvasInput.getHoveredEntity(),
      this.canvasInput.getDragOffset(),
      this.camera.camera.offset,
      state?.obstacles ?? []
    );
  }

  private handleInspectorInput(): void {
    const selection = this.state.getSelection();
    const edit = this.inspector.readEdit(selection.type);
    if (edit) {
      if (edit.type === 'exit' && selection.exitId && edit.id !== selection.exitId && blocksExit(this.state.getRoom(), selection.exitId)) {
        this.setStatus('Die Ausgangs-ID wird in einem Objektzustand verwendet.', 'error');
        return;
      }
      this.state.updateSelected(edit);
      this.syncStateAndRender();
    }
  }

  private async refreshObjectReferences(): Promise<void> {
    try { await this.objectPanel.interactions.refreshReferences(); }
    catch (error) { this.objectPanel.interactions.report(`Zielobjekte konnten nicht geladen werden: ${String(error)}`); }
  }

  private confirmDiscard(): boolean {
    if (!this.state.isDirty() || window.confirm('Ungespeicherte Änderungen verwerfen?')) return true;
    this.elements.roomSelect.value = this.loadedRoomId ?? '';
    return false;
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
    this.editorRenderer.setBackground(EditorApi.backgroundUrl(room.background));
    this.inspector.update(room, this.state.getSelection());
  }
}
