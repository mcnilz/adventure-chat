import type { ObjectStateDto, RoomObject } from '../types.js';

export function applyObjectAppearances(objects: ReadonlyArray<RoomObject>, states: ReadonlyArray<ObjectStateDto>): RoomObject[] {
  const current = new Map(states.map(state => [state.objectId, state.stateId]));
  return objects.map(obj => {
    const appearance = obj.interaction?.states.find(state => state.id === current.get(obj.id));
    return appearance ? { ...obj, image: appearance.image, position: appearance.position, size: appearance.size, depthY: appearance.depthY } : obj;
  });
}
