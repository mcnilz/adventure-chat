import type { ObjectStateDto, RenderedRoomObject, RoomObject } from '../types.js';

export function applyObjectAppearances(objects: ReadonlyArray<RoomObject>, states: ReadonlyArray<ObjectStateDto>): RenderedRoomObject[] {
  const current = new Map(states.map(state => [state.objectId, state]));
  const now = performance.now();
  return objects.map(obj => {
    const state = current.get(obj.id);
    const appearance = obj.interaction?.states.find(item => item.id === state?.stateId);
    if (!appearance) return obj;
    const transition = state?.transition;
    const from = transition && obj.interaction?.states.find(item => item.id === transition.from);
    const movement = from && transition ? { from, startedAtMs: transition.startedAtMs,
      startTime: now - transition.elapsedMs, durationMs: transition.durationMs } : null;
    return { ...obj, image: appearance.image, position: appearance.position, size: appearance.size, depthY: appearance.depthY, movement };
  });
}
