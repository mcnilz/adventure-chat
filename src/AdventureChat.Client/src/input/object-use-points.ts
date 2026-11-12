import type { Point, RoomObject } from '../types.js';

export function objectUsePoints(object: RoomObject, origin?: Point): ReadonlyArray<Point> {
  if (object.usePoints?.length) return object.usePoints;
  if (object.interaction?.usePoint) return [object.interaction.usePoint];
  const { x, y } = object.position, { width, height } = object.size;
  if (origin && origin.x >= x && origin.x <= x + width && origin.y >= y && origin.y <= y + height) return [origin];
  const left = x - 8, right = x + width + 8, top = y - 8, bottom = y + height + 8;
  return [
    { x: x + width / 2, y: bottom }, { x: left, y: y + height / 2 },
    { x: right, y: y + height / 2 }, { x: x + width / 2, y: top },
    { x: left, y: bottom }, { x: right, y: bottom }, { x: left, y: top }, { x: right, y: top },
  ];
}

export function nearestUsePoints(points: ReadonlyArray<Point>, origin: Point): Point[] {
  return [...points].sort((a, b) => Math.hypot(a.x - origin.x, a.y - origin.y) - Math.hypot(b.x - origin.x, b.y - origin.y));
}
