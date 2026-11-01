import type { RoomYamlDto } from '../core/editor-types.js';

export interface SidebarFormElements {
  inputId: HTMLInputElement;
  inputName: HTMLInputElement;
  inputBg: HTMLInputElement;
  inputWidth: HTMLInputElement;
  inputHeight: HTMLInputElement;
  inputNotes: HTMLTextAreaElement;
  unsavedBadge: HTMLElement;
  errorsSection: HTMLElement;
  errorList: HTMLElement;
}

export class EditorSidebarForm {
  public constructor(
    private readonly elements: SidebarFormElements,
    private readonly onFormChange: () => void,
    private readonly onBackgroundChange: (bg: string) => void
  ) {
    this.bindEvents();
  }

  public updateFromRoom(room: RoomYamlDto, isDirty: boolean): void {
    const { elements: el } = this;
    el.inputId.value = room.id;
    el.inputName.value = room.name;
    el.inputBg.value = room.background;
    el.inputWidth.value = String(room.size.width);
    el.inputHeight.value = String(room.size.height);
    el.inputNotes.value = room.notes ?? '';
    el.unsavedBadge.style.display = isDirty ? 'inline-block' : 'none';
  }

  public applyToRoom(room: RoomYamlDto): void {
    const { elements: el } = this;
    room.id = el.inputId.value.trim() || 'unnamed-room';
    room.name = el.inputName.value.trim() || 'Unbenannter Raum';
    room.background = el.inputBg.value.trim() || 'assets/images/lounge.png';
    const notesVal = el.inputNotes.value.trim();
    if (notesVal) {
      room.notes = notesVal;
    } else {
      delete room.notes;
    }
  }

  public setUnsaved(isDirty: boolean): void {
    this.elements.unsavedBadge.style.display = isDirty ? 'inline-block' : 'none';
  }

  public showValidationErrors(errors: string[]): void {
    this.elements.errorList.innerHTML = '';
    for (const err of errors) {
      const li = document.createElement('li');
      li.textContent = err;
      this.elements.errorList.appendChild(li);
    }
    this.elements.errorList.style.display = 'block';
    this.elements.errorsSection.style.display = 'block';
  }

  public clearValidationErrors(): void {
    this.elements.errorList.innerHTML = '';
    this.elements.errorList.style.display = 'none';
    this.elements.errorsSection.style.display = 'none';
  }

  private bindEvents(): void {
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
