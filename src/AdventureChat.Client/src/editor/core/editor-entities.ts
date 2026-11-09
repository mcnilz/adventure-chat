import { clampPosition } from './editor-room-properties.js';
import type { EntityEdit, RoomYamlDto, SelectedEntity } from './editor-types.js';

export function editSelected(room: RoomYamlDto, selection: SelectedEntity, edit: EntityEdit): SelectedEntity {
  if (edit.type !== selection.type) return selection;
  if (edit.type === 'vertex' && selection.polyType && selection.polyIndex !== undefined && selection.vertexIndex !== undefined) {
    const list = selection.polyType === 'walkable' ? room.walkable : room.obstacles;
    const point = list?.[selection.polyIndex]?.points[selection.vertexIndex];
    if (point) Object.assign(point, clampPosition(room, edit, point));
  } else if (edit.type === 'spawn') {
    const spawn = room.spawns.find(s => s.id === selection.spawnId);
    if (spawn) {
      spawn.position = clampPosition(room, edit, spawn.position);
      if (edit.id.trim()) spawn.id = edit.id.trim();
      return { type: 'spawn', spawnId: spawn.id };
    }
  } else if (edit.type === 'exit') {
    const exit = room.exits?.find(e => e.id === selection.exitId);
    if (exit) {
      exit.position = clampPosition(room, edit, exit.position);
      if (edit.id.trim()) exit.id = edit.id.trim();
      // Target selects may still be loading when another inspector field is edited.
      if (edit.room.trim()) exit.target.room = edit.room.trim();
      if (edit.spawn.trim()) exit.target.spawn = edit.spawn.trim();
      exit.label = edit.label.trim() || null;
      if (Number.isFinite(edit.radius)) exit.radius = Math.max(5, Math.min(200, edit.radius));
      return { type: 'exit', exitId: exit.id };
    }
  }
  return selection;
}

export function deleteSelected(room: RoomYamlDto, selection: SelectedEntity): boolean {
  if ((selection.type === 'vertex' || selection.type === 'polygon') && selection.polyType && selection.polyIndex !== undefined) {
    if (selection.type === 'vertex' && selection.vertexIndex === undefined) return false;
    const list = selection.polyType === 'walkable' ? room.walkable : room.obstacles;
    const poly = list?.[selection.polyIndex];
    if (!list || !poly) return false;
    if (selection.type === 'vertex' && selection.vertexIndex !== undefined && poly.points.length > 3) {
      if (!poly.points[selection.vertexIndex]) return false;
      poly.points.splice(selection.vertexIndex, 1);
    } else {
      if (selection.polyType === 'walkable' && list.length <= 1) return false;
      if (selection.type === 'vertex' && list.length <= 1) return false;
      list.splice(selection.polyIndex, 1);
    }
    return true;
  }
  if (selection.type === 'spawn') {
    const index = room.spawns.findIndex(s => s.id === selection.spawnId);
    if (index < 0 || room.spawns.length <= 1) return false;
    room.spawns.splice(index, 1);
    return true;
  }
  if (selection.type === 'exit') {
    const exits = room.exits;
    const index = exits?.findIndex(e => e.id === selection.exitId) ?? -1;
    if (!exits || index < 0) return false;
    exits.splice(index, 1);
    return true;
  }
  if (selection.type === 'object') {
    const objects = room.objects;
    const index = objects?.findIndex(o => o.id === selection.objectId) ?? -1;
    if (!objects || index < 0) return false;
    objects.splice(index, 1);
    return true;
  }
  return false;
}
