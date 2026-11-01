export class EditorSidebarForm {
    elements;
    onFormChange;
    onBackgroundChange;
    constructor(elements, onFormChange, onBackgroundChange) {
        this.elements = elements;
        this.onFormChange = onFormChange;
        this.onBackgroundChange = onBackgroundChange;
        this.bindEvents();
    }
    updateFromRoom(room, isDirty) {
        const { elements: el } = this;
        el.inputId.value = room.id;
        el.inputName.value = room.name;
        el.inputBg.value = room.background;
        el.inputWidth.value = String(room.size.width);
        el.inputHeight.value = String(room.size.height);
        el.inputNotes.value = room.notes ?? '';
        el.unsavedBadge.style.display = isDirty ? 'inline-block' : 'none';
    }
    applyToRoom(room) {
        const { elements: el } = this;
        room.id = el.inputId.value.trim() || 'unnamed-room';
        room.name = el.inputName.value.trim() || 'Unbenannter Raum';
        room.background = el.inputBg.value.trim() || 'assets/images/lounge.png';
        const notesVal = el.inputNotes.value.trim();
        if (notesVal) {
            room.notes = notesVal;
        }
        else {
            delete room.notes;
        }
    }
    setUnsaved(isDirty) {
        this.elements.unsavedBadge.style.display = isDirty ? 'inline-block' : 'none';
    }
    showValidationErrors(errors) {
        this.elements.errorList.innerHTML = '';
        for (const err of errors) {
            const li = document.createElement('li');
            li.textContent = err;
            this.elements.errorList.appendChild(li);
        }
        this.elements.errorList.style.display = 'block';
        this.elements.errorsSection.style.display = 'block';
    }
    clearValidationErrors() {
        this.elements.errorList.innerHTML = '';
        this.elements.errorList.style.display = 'none';
        this.elements.errorsSection.style.display = 'none';
    }
    bindEvents() {
        const { elements: el } = this;
        el.inputId.addEventListener('input', () => this.onFormChange());
        el.inputName.addEventListener('input', () => this.onFormChange());
        el.inputBg.addEventListener('input', () => {
            this.onFormChange();
            this.onBackgroundChange(el.inputBg.value);
        });
        el.inputNotes.addEventListener('input', () => this.onFormChange());
    }
}
//# sourceMappingURL=editor-sidebar-form.js.map