import { HorizontalCamera, viewSize } from '../../rendering/camera.js';
import type { RoomDefinition } from '../../types.js';

// The slider changes only the local editing view, never authored positions.
export class EditorCamera {
  public readonly camera = new HorizontalCamera();
  private readonly slider: HTMLInputElement;
  private roomId = '';

  public constructor(canvas: HTMLCanvasElement, onPan: () => void) {
    const label = document.createElement('label');
    label.textContent = 'Ausschnitt verschieben ';
    this.slider = document.createElement('input');
    this.slider.id = 'editor-camera-pan';
    this.slider.type = 'range';
    this.slider.min = '0';
    this.slider.step = '1';
    this.slider.value = '0';
    this.slider.addEventListener('input', () => { this.camera.offset = Number(this.slider.value); onPan(); });
    label.append(this.slider);
    canvas.parentElement?.after(label);
  }

  public configure(room: RoomDefinition, preview: boolean): void {
    if (room.id !== this.roomId) { this.camera.offset = 0; this.roomId = room.id; }
    this.camera.offset = this.camera.clamp(room, this.camera.offset);
    this.slider.max = String(room.width - viewSize(room).width);
    this.slider.value = String(this.camera.offset);
    this.slider.disabled = preview || Number(this.slider.max) === 0;
  }
}
