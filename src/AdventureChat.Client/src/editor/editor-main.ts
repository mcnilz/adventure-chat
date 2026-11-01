import { EditorApp } from './editor-app.js';

declare global {
  interface Window {
    __ROOM_EDITOR__?: EditorApp;
  }
}

function initializeEditor(): void {
  const canvas = document.getElementById('editor-canvas') as HTMLCanvasElement | null;
  if (!canvas) {
    throw new Error('Canvas #editor-canvas not found');
  }

  const elements = {
    roomSelect: document.getElementById('room-select') as HTMLSelectElement,
    btnNewRoom: document.getElementById('btn-new-room') as HTMLButtonElement,
    btnSave: document.getElementById('btn-save') as HTMLButtonElement,
    btnValidate: document.getElementById('btn-validate') as HTMLButtonElement,
    btnValidateWorld: document.getElementById('btn-validate-world') as HTMLButtonElement | null,
    btnToggleMode: document.getElementById('btn-toggle-mode') as HTMLButtonElement,
    btnDeleteVertex: document.getElementById('btn-delete-vertex') as HTMLButtonElement,
    toolSelect: document.getElementById('tool-select') as HTMLButtonElement,
    toolWalkable: document.getElementById('tool-walkable') as HTMLButtonElement,
    toolObstacle: document.getElementById('tool-obstacle') as HTMLButtonElement,
    toolSpawn: document.getElementById('tool-spawn') as HTMLButtonElement,
    toolExit: document.getElementById('tool-exit') as HTMLButtonElement,
    inputId: document.getElementById('input-id') as HTMLInputElement,
    inputName: document.getElementById('input-name') as HTMLInputElement,
    inputBg: document.getElementById('input-bg') as HTMLInputElement,
    inputWidth: document.getElementById('input-width') as HTMLInputElement,
    inputHeight: document.getElementById('input-height') as HTMLInputElement,
    inputNotes: document.getElementById('input-notes') as HTMLTextAreaElement,
    statusBanner: document.getElementById('status-banner') as HTMLElement,
    errorsSection: document.getElementById('errors-section') as HTMLElement,
    errorList: document.getElementById('error-list') as HTMLElement,
    unsavedBadge: document.getElementById('unsaved-badge') as HTMLElement,
    // Inspector elements
    inspectorEmpty: document.getElementById('inspector-empty') as HTMLElement,
    inspectorVertex: document.getElementById('inspector-vertex') as HTMLElement,
    inspectorVertexBadge: document.getElementById('inspector-vertex-badge') as HTMLElement,
    inspectorVertexX: document.getElementById('inspector-vertex-x') as HTMLInputElement,
    inspectorVertexY: document.getElementById('inspector-vertex-y') as HTMLInputElement,
    btnInspectorDeleteVertex: document.getElementById('btn-inspector-delete-vertex') as HTMLButtonElement,
    // Polygon Inspector
    inspectorPolygon: document.getElementById('inspector-polygon') as HTMLElement,
    inspectorPolygonBadge: document.getElementById('inspector-polygon-badge') as HTMLElement,
    inspectorPolygonId: document.getElementById('inspector-polygon-id') as HTMLInputElement,
    inspectorPolygonPointsCount: document.getElementById('inspector-polygon-points-count') as HTMLInputElement,
    btnInspectorDeletePolygon: document.getElementById('btn-inspector-delete-polygon') as HTMLButtonElement,
    inspectorSpawn: document.getElementById('inspector-spawn') as HTMLElement,
    inspectorSpawnId: document.getElementById('inspector-spawn-id') as HTMLInputElement,
    inspectorSpawnX: document.getElementById('inspector-spawn-x') as HTMLInputElement,
    inspectorSpawnY: document.getElementById('inspector-spawn-y') as HTMLInputElement,
    btnInspectorDeleteSpawn: document.getElementById('btn-inspector-delete-spawn') as HTMLButtonElement,
    inspectorExit: document.getElementById('inspector-exit') as HTMLElement,
    inspectorExitId: document.getElementById('inspector-exit-id') as HTMLInputElement,
    inspectorExitRoom: document.getElementById('inspector-exit-room') as HTMLSelectElement,
    inspectorExitTargetSpawn: document.getElementById('inspector-exit-target-spawn') as HTMLSelectElement,
    inspectorExitLabel: document.getElementById('inspector-exit-label') as HTMLInputElement,
    inspectorExitX: document.getElementById('inspector-exit-x') as HTMLInputElement,
    inspectorExitY: document.getElementById('inspector-exit-y') as HTMLInputElement,
    inspectorExitRadius: document.getElementById('inspector-exit-radius') as HTMLInputElement,
    btnInspectorDeleteExit: document.getElementById('btn-inspector-delete-exit') as HTMLButtonElement,
    // Background picker & upload
    btnOpenBgModal: document.getElementById('btn-open-bg-modal') as HTMLButtonElement,
    btnUploadBgTrigger: document.getElementById('btn-upload-bg-trigger') as HTMLButtonElement,
    inputBgFile: document.getElementById('input-bg-file') as HTMLInputElement,
    bgFsHintPath: document.getElementById('bg-fs-hint-path') as HTMLElement,
    modalBgPicker: document.getElementById('modal-bg-picker') as HTMLDialogElement,
    btnCloseBgModal: document.getElementById('btn-close-bg-modal') as HTMLButtonElement,
    btnCloseBgModalFooter: document.getElementById('btn-close-bg-modal-footer') as HTMLButtonElement,
    modalFsPath: document.getElementById('modal-fs-path') as HTMLElement,
    btnCopyFsPath: document.getElementById('btn-copy-fs-path') as HTMLButtonElement,
    bgDropZone: document.getElementById('bg-drop-zone') as HTMLElement,
    btnModalUploadSelect: document.getElementById('btn-modal-upload-select') as HTMLButtonElement,
    bgGalleryGrid: document.getElementById('bg-gallery-grid') as HTMLElement,
    bgCount: document.getElementById('bg-count') as HTMLElement,
    // Exit Dialog
    modalExitDialog: document.getElementById('modal-exit-dialog') as HTMLDialogElement,
    btnCloseExitModal: document.getElementById('btn-close-exit-modal') as HTMLButtonElement,
    btnCancelExitModal: document.getElementById('btn-cancel-exit-modal') as HTMLButtonElement,
    btnConfirmExitModal: document.getElementById('btn-confirm-exit-modal') as HTMLButtonElement,
    modalExitId: document.getElementById('modal-exit-id') as HTMLInputElement,
    modalExitRoom: document.getElementById('modal-exit-room') as HTMLSelectElement,
    modalExitTargetSpawn: document.getElementById('modal-exit-target-spawn') as HTMLSelectElement,
    modalExitLabel: document.getElementById('modal-exit-label') as HTMLInputElement,
    modalExitRadius: document.getElementById('modal-exit-radius') as HTMLInputElement
  };

  const app = new EditorApp(canvas, elements);
  window.__ROOM_EDITOR__ = app;

  void app.init();
  console.log('Room Editor initialized.');
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initializeEditor);
} else {
  initializeEditor();
}
