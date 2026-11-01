import { EditorApi } from './editor-api.js';
import { EditorBackgroundModal } from './editor-background-modal.js';
import { EditorCanvasInput } from './editor-canvas-input.js';
import { EditorExitDialog } from './editor-exit-dialog.js';
import { EditorInspector } from './editor-inspector.js';
import { EditorPreview } from './editor-preview.js';
import { EditorRenderer } from './editor-renderer.js';
import { EditorSidebarForm } from './editor-sidebar-form.js';
import { EditorState } from './editor-state.js';
import { EditorToolbar } from './editor-toolbar.js';
export class EditorApp {
    canvas;
    elements;
    state;
    editorRenderer;
    preview;
    exitDialog;
    inspector;
    bgModal;
    toolbar;
    form;
    canvasInput;
    mode = 'edit';
    currentTool = 'select';
    constructor(canvas, elements) {
        this.canvas = canvas;
        this.elements = elements;
        this.state = new EditorState();
        this.editorRenderer = new EditorRenderer(this.canvas);
        this.preview = new EditorPreview(this.canvas);
        this.exitDialog = new EditorExitDialog(this.elements, (msg, type) => this.setStatus(msg, type));
        this.inspector = new EditorInspector(this.elements, this.exitDialog, () => this.handleInspectorInput(), () => this.deleteSelected());
        this.bgModal = new EditorBackgroundModal(this.elements, (bgPath) => {
            this.elements.inputBg.value = bgPath;
            this.state.updateMetadata({ background: bgPath });
            this.editorRenderer.setBackground(bgPath);
            this.render();
        }, (msg, type) => this.setStatus(msg, type));
        this.toolbar = new EditorToolbar(this.elements, {
            onRoomSelect: (roomId) => this.loadRoom(roomId).catch(() => { }),
            onNewRoom: () => this.newRoom(),
            onSave: () => this.saveRoom().catch(() => { }),
            onValidate: () => this.validateRoom().catch(() => { }),
            onToggleMode: () => this.toggleMode(),
            onDeleteSelected: () => this.deleteSelected(),
            onToolSelect: (tool) => this.setTool(tool)
        });
        this.form = new EditorSidebarForm(this.elements, () => {
            this.form.applyToRoom(this.state.getRoom());
            this.state.updateMetadata({});
            this.form.setUnsaved(this.state.isDirty());
        }, (bg) => {
            this.editorRenderer.setBackground(bg);
            this.render();
        });
        this.canvasInput = new EditorCanvasInput(this.canvas, this.state, {
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
                }).catch(() => { });
            }
        }, () => this.currentTool, () => this.mode === 'preview', (x, y) => this.preview.handleCanvasClick(x, y, this.state.toRoomDefinition()));
        window.addEventListener('beforeunload', (e) => {
            if (this.state.isDirty())
                e.preventDefault();
        });
        this.syncAllViews();
        this.form.clearValidationErrors();
        this.render();
        // Hook for E2E testing
        window.__ROOM_EDITOR__ = this;
    }
    async init() {
        await this.bgModal.loadBackgroundsOverview();
        await this.exitDialog.initRooms();
        await this.loadRoomList();
    }
    getState() {
        return this.state;
    }
    getMode() {
        return this.mode;
    }
    getTool() {
        return this.currentTool;
    }
    setTool(tool) {
        this.currentTool = tool;
        this.toolbar.updateToolButtons(tool);
        this.render();
    }
    setStatus(msg, type = 'info') {
        this.elements.statusBanner.textContent = msg;
        this.elements.statusBanner.className = `status-banner status-${type}`;
    }
    async loadRoomList(selectedRoomId) {
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
            }
            else if (rooms.length > 0) {
                await this.loadRoom(rooms[0].id);
            }
            else {
                this.newRoom();
            }
        }
        catch (err) {
            this.setStatus(`Fehler beim Laden der Räume: ${String(err)}`, 'error');
        }
    }
    async loadRoom(id) {
        try {
            this.setStatus(`Lade Raum '${id}'...`, 'info');
            const room = await EditorApi.fetchRoom(id);
            this.state.setRoom(room);
            this.syncAllViews();
            this.form.clearValidationErrors();
            this.setStatus(`Raum '${room.name}' geladen.`, 'success');
            this.render();
        }
        catch (err) {
            this.setStatus(`Fehler beim Laden von '${id}': ${String(err)}`, 'error');
        }
    }
    newRoom() {
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
    async saveRoom() {
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
            this.setStatus(`Raum '${room.name}' (${room.id}) erfolgreich atomar gespeichert!`, 'success');
            return true;
        }
        catch (err) {
            this.setStatus(`Fehler beim Speichern: ${String(err)}`, 'error');
            return false;
        }
    }
    async validateRoom() {
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
            }
            else {
                this.form.showValidationErrors(result.errors);
                this.setStatus(`⚠️ Validierungsfehler: ${result.errors.length} Problem(e) gefunden.`, 'error');
                return false;
            }
        }
        catch (err) {
            this.setStatus(`Validierungsfehler: ${String(err)}`, 'error');
            return false;
        }
    }
    toggleMode() {
        if (this.mode === 'edit') {
            this.mode = 'preview';
            this.toolbar.updateModeButton('preview');
            this.setStatus('Vorschaumodus aktiv: Klicke auf die begehbare Fläche, um die Spielfigur zu steuern.', 'info');
            this.preview.start(this.state.toRoomDefinition());
        }
        else {
            this.mode = 'edit';
            this.toolbar.updateModeButton('edit');
            this.preview.stop();
            this.setStatus('Bearbeitungsmodus aktiv.', 'info');
            this.render();
        }
    }
    deleteSelected() {
        const deleted = this.state.deleteSelected();
        if (deleted) {
            this.syncStateAndRender();
        }
    }
    render() {
        if (this.mode === 'preview')
            return;
        this.editorRenderer.setBackground(this.state.getRoom().background);
        this.editorRenderer.render(this.state.getRoom(), this.state.getSelection(), this.canvasInput.getMouseLogicalPos(), this.canvasInput.getDragPos(), this.canvasInput.getHoveredVertex(), this.canvasInput.getHoveredEdge(), this.canvasInput.getHoveredEntity(), this.canvasInput.getDragOffset());
    }
    handleInspectorInput() {
        const room = this.state.getRoom();
        const sel = this.state.getSelection();
        if (sel.type === 'vertex') {
            if (this.inspector.applyVertexInputs(room, sel)) {
                this.syncStateAndRender();
            }
        }
        else if (sel.type === 'spawn') {
            if (this.inspector.applySpawnInputs(room, sel)) {
                this.syncStateAndRender();
            }
        }
        else if (sel.type === 'exit') {
            if (this.inspector.applyExitInputs(room, sel)) {
                this.syncStateAndRender();
            }
        }
    }
    syncStateAndRender() {
        const room = this.state.getRoom();
        this.inspector.update(room, this.state.getSelection());
        this.form.setUnsaved(this.state.isDirty());
        this.render();
    }
    syncAllViews() {
        const room = this.state.getRoom();
        this.form.updateFromRoom(room, this.state.isDirty());
        this.editorRenderer.setBackground(room.background);
        this.inspector.update(room, this.state.getSelection());
    }
}
//# sourceMappingURL=editor-app.js.map