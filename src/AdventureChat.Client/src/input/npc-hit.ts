import { actorHeightAt } from '../rendering/depth-scale.js';
import type { NpcCast } from '../state/npc-cast.js';
import type { NpcPresence, Point, RoomDefinition } from '../types.js';

export function npcAt(cast: NpcCast, room: RoomDefinition, point: Point, occludingDepth: number): NpcPresence | undefined {
  return [...cast.entries].sort((a, b) => b.character.position.y - a.character.position.y).find(({ character }) => {
    const position = character.position;
    if (occludingDepth > position.y) return false;
    const height = actorHeightAt(room, position.y);
    const aspect = character.appearance ? character.appearance.frameWidth / character.appearance.frameHeight : 0.75;
    return Math.abs(position.x - point.x) <= height * aspect / 2
      && point.y >= position.y - height && point.y <= position.y;
  })?.npc;
}
