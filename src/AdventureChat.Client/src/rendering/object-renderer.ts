import type { Point, RoomObject } from '../types.js';

export function compareObjectDepth(a: RoomObject, b: RoomObject): number {
  return a.depthY - b.depthY || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
}

export class ObjectRenderer {
  private readonly images = new Map<string, HTMLImageElement>();
  private readonly hitCanvas = document.createElement('canvas');

  public containsVisiblePixel(object: RoomObject, point: Point): boolean {
    const x = (point.x - object.position.x) / object.size.width;
    const y = (point.y - object.position.y) / object.size.height;
    if (x < 0 || x >= 1 || y < 0 || y >= 1) return false;
    const image = this.images.get(object.image);
    if (!image?.complete || image.naturalWidth === 0) return false;
    this.hitCanvas.width = 1; this.hitCanvas.height = 1;
    const ctx = this.hitCanvas.getContext('2d');
    if (!ctx) return false;
    try {
      ctx.drawImage(image, Math.floor(x * image.naturalWidth), Math.floor(y * image.naturalHeight), 1, 1, 0, 0, 1, 1);
      return ctx.getImageData(0, 0, 1, 1).data[3]! > 0;
    } catch { return false; }
  }

  public draw(ctx: CanvasRenderingContext2D, object: RoomObject, onLoad?: () => void): void {
    let image = this.images.get(object.image);
    if (!image) {
      image = new Image();
      if (onLoad) image.onload = onLoad;
      image.src = object.image;
      this.images.set(object.image, image);
    }
    if (image.complete && image.naturalWidth > 0) {
      ctx.drawImage(image, object.position.x, object.position.y, object.size.width, object.size.height);
    }
  }
}
