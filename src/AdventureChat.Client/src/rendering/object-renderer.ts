import type { RoomObject } from '../types.js';

export function compareObjectDepth(a: RoomObject, b: RoomObject): number {
  return a.depthY - b.depthY || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
}

export class ObjectRenderer {
  private readonly images = new Map<string, HTMLImageElement>();

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
