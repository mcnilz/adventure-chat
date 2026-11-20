import type { RenderedRoomObject, RoomObject } from '../types.js';

export function continueObjectMovements(objects: readonly RenderedRoomObject[],
  previous: readonly RenderedRoomObject[]): RenderedRoomObject[] {
  const previousMovements = new Map(previous.map(object => [object.id, object.movement]));
  return objects.map(object => {
    const movement = object.movement;
    const before = previousMovements.get(object.id);
    if (!movement || before?.startedAtMs !== movement.startedAtMs || before.startTime >= movement.startTime) return object;
    return { ...object, movement: { ...movement, startTime: before.startTime } };
  });
}

export function objectAtTime(object: RenderedRoomObject, now: number): RoomObject {
  const movement = object.movement;
  if (!movement || now >= movement.startTime + movement.durationMs) return object;
  const progress = Math.max(0, (now - movement.startTime) / movement.durationMs);
  const interpolatePixel = (from: number, to: number) => from + Math.trunc((to - from) * progress);
  return {
    ...object,
    position: {
      x: interpolatePixel(movement.from.position.x, object.position.x),
      y: interpolatePixel(movement.from.position.y, object.position.y)
    },
    depthY: movement.from.depthY + (object.depthY - movement.from.depthY) * progress
  };
}
