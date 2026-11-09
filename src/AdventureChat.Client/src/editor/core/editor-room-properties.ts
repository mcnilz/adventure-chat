import type { RoomObjectDto, RoomPointDto, RoomPropertiesEdit, RoomYamlDto } from './editor-types.js';

// Keep loaded optional defaults intact when a form merely displays them.
export function applyRoomProperties(room: RoomYamlDto, edit: RoomPropertiesEdit): void {
  if (edit.size) room.size = { ...edit.size };
  room.id = edit.id.trim() || 'unnamed-room';
  room.name = edit.name.trim() || 'Unbenannter Raum';
  room.background = edit.background.trim() || 'assets/images/lounge.png';
  if ((room.perspective ?? 'adventure') !== edit.perspective) {
    if (edit.perspective === 'top-down') room.perspective = 'top-down';
    else delete room.perspective;
  }
  if ((room.actorHeight ?? 64) !== edit.actorHeight) {
    if (edit.actorHeight === 64) delete room.actorHeight;
    else room.actorHeight = edit.actorHeight;
  }
  if ((room.walkSpeed ?? 130) !== edit.walkSpeed) {
    if (edit.walkSpeed === 130) delete room.walkSpeed;
    else room.walkSpeed = edit.walkSpeed;
  }
  if (edit.viewportWidth !== null) {
    room.viewport = { width: edit.viewportWidth, height: room.size.height, scrollMode: 'horizontal' };
  } else if (room.viewport) delete room.viewport;
  if (edit.depthScale) room.depthScale = structuredClone(edit.depthScale);
  else if (room.depthScale) delete room.depthScale;
  const notes = edit.notes.trim();
  if ((room.notes ?? '') !== notes) {
    if (notes) room.notes = notes;
    else delete room.notes;
  }
}

export function clampPosition(room: RoomYamlDto, pos: RoomPointDto, previous: RoomPointDto): RoomPointDto {
  return {
    x: Number.isFinite(pos.x) ? Math.max(0, Math.min(room.size.width, Math.round(pos.x))) : previous.x,
    y: Number.isFinite(pos.y) ? Math.max(0, Math.min(room.size.height, Math.round(pos.y))) : previous.y
  };
}

export function createRoomObject(room: RoomYamlDto): RoomObjectDto {
  let number = 1;
  while (room.objects?.some(o => o.id === `object-${number}`)) number++;
  return {
    id: `object-${number}`, image: '', position: { x: 0, y: 0 },
    size: { width: Math.min(64, room.size.width), height: Math.min(64, room.size.height) },
    depthY: Math.min(64, room.size.height)
  };
}

export function validRoomObject(room: RoomYamlDto, object: RoomObjectDto, previousId?: string): boolean {
  const { id, position: { x, y }, size: { width, height }, depthY } = object;
  return /^[a-zA-Z0-9_-]{1,64}$/.test(id) &&
    !room.objects?.some(o => o.id !== previousId && o.id === id) &&
    [x, y, width, height, depthY].every(Number.isInteger) && x >= 0 && y >= 0 && width >= 1 && height >= 1 &&
    x + width <= room.size.width && y + height <= room.size.height && depthY >= 0 && depthY <= room.size.height;
}
