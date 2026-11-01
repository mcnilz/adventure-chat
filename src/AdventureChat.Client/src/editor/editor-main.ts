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
    errorList: document.getElementById('error-list') as HTMLElement,
    unsavedBadge: document.getElementById('unsaved-badge') as HTMLElement
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
