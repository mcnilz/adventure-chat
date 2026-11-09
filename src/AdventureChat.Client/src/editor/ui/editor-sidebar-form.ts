import type { RoomYamlDto } from '../core/editor-types.js';

export interface SidebarFormElements {
  inputId: HTMLInputElement;
  inputName: HTMLInputElement;
  inputBg: HTMLInputElement;
  inputWidth: HTMLInputElement;
  inputHeight: HTMLInputElement;
  inputPerspective: HTMLSelectElement;
  inputActorHeight: HTMLInputElement;
  inputDepthEnabled: HTMLInputElement;
  inputDepthBackY: HTMLInputElement;
  inputDepthBackFactor: HTMLInputElement;
  inputDepthFrontY: HTMLInputElement;
  inputDepthFrontFactor: HTMLInputElement;
  inputWalkSpeed: HTMLInputElement;
  inputViewportWidth: HTMLInputElement;
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
    el.inputPerspective.value = room.perspective ?? 'adventure';
    el.inputActorHeight.value = String(room.actorHeight ?? 64);
    el.inputWalkSpeed.value = String(room.walkSpeed ?? 130);
    el.inputViewportWidth.value = room.viewport ? String(room.viewport.width) : '';
    el.inputDepthEnabled.checked = !!room.depthScale;
    el.inputDepthBackY.value = String(room.depthScale?.back.y ?? 120);
    el.inputDepthBackFactor.value = String(room.depthScale?.back.factor ?? 0.5);
    el.inputDepthFrontY.value = String(room.depthScale?.front.y ?? 320);
    el.inputDepthFrontFactor.value = String(room.depthScale?.front.factor ?? 1);
    el.inputNotes.value = room.notes ?? '';
    el.unsavedBadge.style.display = isDirty ? 'inline-block' : 'none';
  }

  public applyToRoom(room: RoomYamlDto): void {
    const { elements: el } = this;
    if (el.inputWidth.checkValidity() && el.inputHeight.checkValidity()) {
      room.size = { width: Number(el.inputWidth.value), height: Number(el.inputHeight.value) };
    }
    room.id = el.inputId.value.trim() || 'unnamed-room';
    room.name = el.inputName.value.trim() || 'Unbenannter Raum';
    room.background = el.inputBg.value.trim() || 'assets/images/lounge.png';
    if (el.inputPerspective.value === 'top-down') room.perspective = 'top-down';
    else delete room.perspective;
    const height = Number(el.inputActorHeight.value);
    const speed = Number(el.inputWalkSpeed.value);
    if (height !== 64) room.actorHeight = height; else delete room.actorHeight;
    if (speed !== 130) room.walkSpeed = speed; else delete room.walkSpeed;
    if (el.inputViewportWidth.value.trim()) room.viewport = { width: Number(el.inputViewportWidth.value), height: room.size.height, scrollMode: 'horizontal' };
    else delete room.viewport;
    if (el.inputDepthEnabled.checked) {
      room.depthScale = {
        back: { y: el.inputDepthBackY.valueAsNumber, factor: el.inputDepthBackFactor.valueAsNumber },
        front: { y: el.inputDepthFrontY.valueAsNumber, factor: el.inputDepthFrontFactor.valueAsNumber }
      };
    } else delete room.depthScale;
    const notesVal = el.inputNotes.value.trim();
    if (notesVal) {
      room.notes = notesVal;
    } else {
      delete room.notes;
    }
  }

  public validateDimensions(): boolean {
    return this.elements.inputWidth.reportValidity() && this.elements.inputHeight.reportValidity();
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

    for (const input of [el.inputDepthEnabled, el.inputDepthBackY, el.inputDepthBackFactor, el.inputDepthFrontY, el.inputDepthFrontFactor, el.inputPerspective, el.inputActorHeight, el.inputWalkSpeed, el.inputViewportWidth, el.inputWidth, el.inputHeight]) {
      input.addEventListener('change', () => this.onFormChange());
    }
    el.inputId.addEventListener('input', () => this.onFormChange());
    el.inputName.addEventListener('input', () => this.onFormChange());
    el.inputBg.addEventListener('input', () => {
      this.onFormChange();
      this.onBackgroundChange(el.inputBg.value);
    });
    el.inputNotes.addEventListener('input', () => this.onFormChange());
  }
}
