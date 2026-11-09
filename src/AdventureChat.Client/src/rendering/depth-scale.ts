import type { RoomDefinition } from '../types.js';

/** Figure height in room pixels; camera and screen scaling are applied later. */
export function actorHeightAt(room: Pick<RoomDefinition, 'actorHeight' | 'depthScale'> | undefined, footY: number): number {
  const height = room?.actorHeight ?? 64;
  const scale = room?.depthScale;
  if (!scale) return height;
  const progress = Math.max(0, Math.min(1, (footY - scale.back.y) / (scale.front.y - scale.back.y)));
  return height * (scale.back.factor + progress * (scale.front.factor - scale.back.factor));
}
