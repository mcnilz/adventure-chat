import type { ObjectAppearance } from '../../types.js';
import type { RoomView, SelectedEntity } from './editor-types.js';

export function roomWithSelectedAppearance(room: RoomView, selection: Readonly<SelectedEntity>,
  appearance?: ObjectAppearance): RoomView {
  if (selection.type !== 'object' || !appearance || !room.objects) return room;
  const { image, position, size, depthY } = appearance;
  const objects = room.objects.map(object => object.id === selection.objectId
    ? { ...object, image, position, size, depthY }
    : object);
  return { ...room, objects };
}
