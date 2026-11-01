import type { EditorMode, EditorTool } from './editor-types.js';

export interface ToolbarElements {
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
}

export interface ToolbarCallbacks {
  onRoomSelect: (roomId: string) => void;
  onNewRoom: () => void;
  onSave: () => void;
  onValidate: () => void;
  onToggleMode: () => void;
  onDeleteSelected: () => void;
  onToolSelect: (tool: EditorTool) => void;
}

export class EditorToolbar {
  public constructor(
    private readonly elements: ToolbarElements,
    private readonly callbacks: ToolbarCallbacks
  ) {
    this.bindEvents();
  }

  public updateToolButtons(currentTool: EditorTool): void {
    const { elements: el } = this;
    el.toolSelect.classList.toggle('active', currentTool === 'select');
    el.toolWalkable.classList.toggle('active', currentTool === 'add-walkable');
    el.toolObstacle.classList.toggle('active', currentTool === 'add-obstacle');
    el.toolSpawn.classList.toggle('active', currentTool === 'add-spawn');
    el.toolExit.classList.toggle('active', currentTool === 'add-exit');
  }

  public updateModeButton(mode: EditorMode): void {
    const { btnToggleMode } = this.elements;
    if (mode === 'preview') {
      btnToggleMode.textContent = '✏️ Bearbeiten';
      btnToggleMode.className = 'btn-secondary';
    } else {
      btnToggleMode.textContent = '▶️ Raum-Vorschau';
      btnToggleMode.className = 'btn-preview';
    }
  }

  private bindEvents(): void {
    const { elements: el, callbacks: cb } = this;

    el.roomSelect.addEventListener('change', () => {
      const id = el.roomSelect.value;
      if (id) cb.onRoomSelect(id);
    });

    el.btnNewRoom.addEventListener('click', () => cb.onNewRoom());
    el.btnSave.addEventListener('click', () => cb.onSave());
    el.btnValidate.addEventListener('click', () => cb.onValidate());
    el.btnToggleMode.addEventListener('click', () => cb.onToggleMode());
    el.btnDeleteVertex.addEventListener('click', () => cb.onDeleteSelected());

    // Tools
    el.toolSelect.addEventListener('click', () => cb.onToolSelect('select'));
    el.toolWalkable.addEventListener('click', () => cb.onToolSelect('add-walkable'));
    el.toolObstacle.addEventListener('click', () => cb.onToolSelect('add-obstacle'));
    el.toolSpawn.addEventListener('click', () => cb.onToolSelect('add-spawn'));
    el.toolExit.addEventListener('click', () => cb.onToolSelect('add-exit'));
  }
}
