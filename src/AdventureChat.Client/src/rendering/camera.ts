import type { Point, RoomDefinition } from '../types.js';

type CameraRoom = Pick<RoomDefinition, 'width' | 'height' | 'viewport'>;

export function viewSize(room: CameraRoom): { width: number; height: number } {
  const requestedWidth = room.viewport?.width;
  const width = requestedWidth !== undefined && Number.isInteger(requestedWidth) && requestedWidth > 0
    ? requestedWidth : room.width;
  return { width: Math.min(room.width, width), height: room.height };
}

export class HorizontalCamera {
  public offset = 0;

  public clamp(room: CameraRoom, offset: number): number {
    return Math.max(0, Math.min(room.width - viewSize(room).width, offset));
  }

  public reset(room: CameraRoom, footpointX: number): void {
    this.offset = this.clamp(room, footpointX - viewSize(room).width / 2);
  }

  public follow(room: CameraRoom, footpointX: number): void {
    const width = viewSize(room).width;
    const x = footpointX - this.offset;
    if (x < width * 0.35) this.offset = this.clamp(room, footpointX - width * 0.35);
    else if (x > width * 0.65) this.offset = this.clamp(room, footpointX - width * 0.65);
    else this.offset = this.clamp(room, this.offset);
  }
}

export function screenToRoom(clientX: number, clientY: number,
  rect: Pick<DOMRect, 'left' | 'top' | 'width' | 'height'>, room: CameraRoom, offset: number): Point {
  const size = viewSize(room);
  return { x: (clientX - rect.left) * size.width / rect.width + offset,
    y: (clientY - rect.top) * size.height / rect.height };
}

export function resizeCanvas(canvas: HTMLCanvasElement, room: CameraRoom): void {
  const size = viewSize(room);
  if (canvas.width !== size.width) canvas.width = size.width;
  if (canvas.height !== size.height) canvas.height = size.height;
  canvas.style.aspectRatio = `${size.width} / ${size.height}`;
  canvas.style.height = 'auto';
}
