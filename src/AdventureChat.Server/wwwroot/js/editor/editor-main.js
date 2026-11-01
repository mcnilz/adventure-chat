import { EditorApp } from './editor-app.js';
function initializeEditor() {
    const canvas = document.getElementById('editor-canvas');
    if (!canvas) {
        throw new Error('Canvas #editor-canvas not found');
    }
    const elements = {
        roomSelect: document.getElementById('room-select'),
        btnNewRoom: document.getElementById('btn-new-room'),
        btnSave: document.getElementById('btn-save'),
        btnValidate: document.getElementById('btn-validate'),
        btnToggleMode: document.getElementById('btn-toggle-mode'),
        btnDeleteVertex: document.getElementById('btn-delete-vertex'),
        toolSelect: document.getElementById('tool-select'),
        toolWalkable: document.getElementById('tool-walkable'),
        toolObstacle: document.getElementById('tool-obstacle'),
        toolSpawn: document.getElementById('tool-spawn'),
        toolExit: document.getElementById('tool-exit'),
        inputId: document.getElementById('input-id'),
        inputName: document.getElementById('input-name'),
        inputBg: document.getElementById('input-bg'),
        inputWidth: document.getElementById('input-width'),
        inputHeight: document.getElementById('input-height'),
        inputNotes: document.getElementById('input-notes'),
        statusBanner: document.getElementById('status-banner'),
        errorsSection: document.getElementById('errors-section'),
        errorList: document.getElementById('error-list'),
        unsavedBadge: document.getElementById('unsaved-badge'),
        // Inspector elements
        inspectorEmpty: document.getElementById('inspector-empty'),
        inspectorVertex: document.getElementById('inspector-vertex'),
        inspectorVertexBadge: document.getElementById('inspector-vertex-badge'),
        inspectorVertexX: document.getElementById('inspector-vertex-x'),
        inspectorVertexY: document.getElementById('inspector-vertex-y'),
        btnInspectorDeleteVertex: document.getElementById('btn-inspector-delete-vertex'),
        // Polygon Inspector
        inspectorPolygon: document.getElementById('inspector-polygon'),
        inspectorPolygonBadge: document.getElementById('inspector-polygon-badge'),
        inspectorPolygonId: document.getElementById('inspector-polygon-id'),
        inspectorPolygonPointsCount: document.getElementById('inspector-polygon-points-count'),
        btnInspectorDeletePolygon: document.getElementById('btn-inspector-delete-polygon'),
        inspectorSpawn: document.getElementById('inspector-spawn'),
        inspectorSpawnId: document.getElementById('inspector-spawn-id'),
        inspectorSpawnX: document.getElementById('inspector-spawn-x'),
        inspectorSpawnY: document.getElementById('inspector-spawn-y'),
        btnInspectorDeleteSpawn: document.getElementById('btn-inspector-delete-spawn'),
        inspectorExit: document.getElementById('inspector-exit'),
        inspectorExitId: document.getElementById('inspector-exit-id'),
        inspectorExitRoom: document.getElementById('inspector-exit-room'),
        inspectorExitTargetSpawn: document.getElementById('inspector-exit-target-spawn'),
        inspectorExitLabel: document.getElementById('inspector-exit-label'),
        inspectorExitX: document.getElementById('inspector-exit-x'),
        inspectorExitY: document.getElementById('inspector-exit-y'),
        inspectorExitRadius: document.getElementById('inspector-exit-radius'),
        btnInspectorDeleteExit: document.getElementById('btn-inspector-delete-exit'),
        // Background picker & upload
        btnOpenBgModal: document.getElementById('btn-open-bg-modal'),
        btnUploadBgTrigger: document.getElementById('btn-upload-bg-trigger'),
        inputBgFile: document.getElementById('input-bg-file'),
        bgFsHintPath: document.getElementById('bg-fs-hint-path'),
        modalBgPicker: document.getElementById('modal-bg-picker'),
        btnCloseBgModal: document.getElementById('btn-close-bg-modal'),
        btnCloseBgModalFooter: document.getElementById('btn-close-bg-modal-footer'),
        modalFsPath: document.getElementById('modal-fs-path'),
        btnCopyFsPath: document.getElementById('btn-copy-fs-path'),
        bgDropZone: document.getElementById('bg-drop-zone'),
        btnModalUploadSelect: document.getElementById('btn-modal-upload-select'),
        bgGalleryGrid: document.getElementById('bg-gallery-grid'),
        bgCount: document.getElementById('bg-count'),
        // Exit Dialog
        modalExitDialog: document.getElementById('modal-exit-dialog'),
        btnCloseExitModal: document.getElementById('btn-close-exit-modal'),
        btnCancelExitModal: document.getElementById('btn-cancel-exit-modal'),
        btnConfirmExitModal: document.getElementById('btn-confirm-exit-modal'),
        modalExitId: document.getElementById('modal-exit-id'),
        modalExitRoom: document.getElementById('modal-exit-room'),
        modalExitTargetSpawn: document.getElementById('modal-exit-target-spawn'),
        modalExitLabel: document.getElementById('modal-exit-label'),
        modalExitRadius: document.getElementById('modal-exit-radius')
    };
    const app = new EditorApp(canvas, elements);
    window.__ROOM_EDITOR__ = app;
    void app.init();
    console.log('Room Editor initialized.');
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeEditor);
}
else {
    initializeEditor();
}
//# sourceMappingURL=editor-main.js.map