import { moveTowards } from '../math.js';
import { WALK_FRAME_INTERVAL } from '../constants.js';
import type { Character, Direction, Point } from '../types.js';

/**
 * Calculates the cardinal direction based on the movement delta vector.
 */
export function calculateFacingDirection(dx: number, dy: number, currentFacing: Direction): Direction {
  if (Math.abs(dx) >= Math.abs(dy)) {
    if (dx < -1) return 'left';
    if (dx > 1) return 'right';
  } else {
    if (dy < -1) return 'up';
    if (dy > 1) return 'down';
  }
  return currentFacing;
}

/**
 * Advances a character to its next waypoint in the queue.
 */
export function advanceToNextWaypoint(char: Character): void {
  if (char.waypoints.length === 0) {
    char.target = null;
    char.state = 'idle';
    char.walkFrame = 0;
    char.frameTimer = 0;
    return;
  }

  const nextTarget = char.waypoints.shift()!;
  char.target = nextTarget;
  char.state = 'walk';

  const dx = nextTarget.x - char.position.x;
  const dy = nextTarget.y - char.position.y;
  char.facing = calculateFacingDirection(dx, dy, char.facing);
}

/**
 * Sets a new sequence of waypoints for a character.
 */
export function assignPathToCharacter(char: Character, waypoints: Point[]): void {
  if (waypoints.length <= 1) {
    char.waypoints = [];
    char.target = null;
    char.state = 'idle';
    return;
  }

  char.waypoints = waypoints.slice(1);
  advanceToNextWaypoint(char);
}

/**
 * Steps character movement forward by delta time dt.
 */
export function updateCharacterMovement(char: Character, dt: number): void {
  if (char.target && char.state === 'walk') {
    const maxDistance = char.speed * dt;
    const { position, reached } = moveTowards(char.position, char.target, maxDistance);

    char.position = position;

    if (reached) {
      if (char.waypoints.length > 0) {
        advanceToNextWaypoint(char);
      } else {
        char.target = null;
        char.state = 'idle';
        char.walkFrame = 0;
        char.frameTimer = 0;
      }
    } else {
      char.frameTimer += dt;
      if (char.frameTimer >= WALK_FRAME_INTERVAL) {
        char.frameTimer = 0;
        char.walkFrame = (char.walkFrame + 1) % 4;
      }
    }
  } else {
    char.state = 'idle';
    char.walkFrame = 0;
  }
}
