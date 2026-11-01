export class EditorToolbar {
    elements;
    callbacks;
    constructor(elements, callbacks) {
        this.elements = elements;
        this.callbacks = callbacks;
        this.bindEvents();
    }
    updateToolButtons(currentTool) {
        const { elements: el } = this;
        el.toolSelect.classList.toggle('active', currentTool === 'select');
        el.toolWalkable.classList.toggle('active', currentTool === 'add-walkable');
        el.toolObstacle.classList.toggle('active', currentTool === 'add-obstacle');
        el.toolSpawn.classList.toggle('active', currentTool === 'add-spawn');
        el.toolExit.classList.toggle('active', currentTool === 'add-exit');
    }
    updateModeButton(mode) {
        const { btnToggleMode } = this.elements;
        if (mode === 'preview') {
            btnToggleMode.textContent = '✏️ Bearbeiten';
            btnToggleMode.className = 'btn-secondary';
        }
        else {
            btnToggleMode.textContent = '▶️ Raum-Vorschau';
            btnToggleMode.className = 'btn-preview';
        }
    }
    bindEvents() {
        const { elements: el, callbacks: cb } = this;
        el.roomSelect.addEventListener('change', () => {
            const id = el.roomSelect.value;
            if (id)
                cb.onRoomSelect(id);
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
//# sourceMappingURL=editor-toolbar.js.map