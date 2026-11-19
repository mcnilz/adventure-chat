import { EXIT_TRIGGER_DISTANCE } from '../constants.js';
import type { Point, RoomExit } from '../types.js';

export function exitAt(exits: readonly RoomExit[], point: Point): RoomExit | undefined {
  return exits.find(exit => {
    const bounds = exit.bounds;
    return (bounds && point.x >= bounds.x && point.x <= bounds.x + bounds.width &&
      point.y >= bounds.y && point.y <= bounds.y + bounds.height) ||
      Math.hypot(point.x - exit.position.x, point.y - exit.position.y) <= (exit.radius || EXIT_TRIGGER_DISTANCE);
  });
}

export function withinExitReach(exit: RoomExit, point: Point): boolean {
  return Math.hypot(point.x - exit.position.x, point.y - exit.position.y) <= (exit.radius ?? EXIT_TRIGGER_DISTANCE) + 5;
}
