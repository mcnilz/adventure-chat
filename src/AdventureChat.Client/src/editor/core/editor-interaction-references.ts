import type { ObjectAction } from '../../types.js';
import type { RoomView } from './editor-types.js';

export function hasEffectReference(rooms: readonly RoomView[], room: string, object: string, state?: string): boolean {
  return rooms.some(source => source.objects?.some(item => item.interaction?.actions.some(action =>
    action.effects?.some(effect => effect.room === room && effect.object === object &&
      (state === undefined || effect.state === state)))));
}

export function referencesState(action: ObjectAction, state: string): boolean {
  return action.from === state || action.to === state;
}

export function blocksExit(room: RoomView, exitId: string): boolean {
  return room.objects?.some(object => object.interaction?.states.some(state =>
    state.blockedExits?.some(id => id.toLowerCase() === exitId.toLowerCase()))) ?? false;
}
