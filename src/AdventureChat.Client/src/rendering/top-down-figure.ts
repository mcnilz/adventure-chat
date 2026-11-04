import type { Character } from '../types.js';

// Reference figure drawn as native Canvas pixel art, in a 48 x 64 frame.
// Every direction has a distinct face; walking alternates the feet and arms.
export function drawTopDownFigure(ctx: CanvasRenderingContext2D, character: Character): void {
  const angle = { up: 0, right: Math.PI / 2, down: Math.PI, left: -Math.PI / 2 }[character.facing];
  const step = character.state === 'walk' ? (character.walkFrame % 2 === 0 ? -4 : 4) : 0;
  ctx.save();
  ctx.translate(0, -32);
  ctx.rotate(angle);
  ctx.fillStyle = '#172533';
  ctx.fillRect(-15, 12 + step, 11, 12);
  ctx.fillRect(4, 12 - step, 11, 12);
  ctx.fillStyle = '#387b87';
  ctx.fillRect(-20, -6, 40, 24);
  ctx.fillStyle = '#dba67a';
  ctx.fillRect(-23, -12 - step, 7, 16);
  ctx.fillRect(16, -12 + step, 7, 16);
  ctx.fillRect(-12, -26, 24, 22);
  ctx.fillStyle = '#573b2c';
  ctx.fillRect(-12, -20, 24, 16);
  ctx.fillStyle = '#f2c695';
  ctx.fillRect(-8, -28, 16, 8);
  ctx.restore();
}
