import type { RoomDefinition } from './types.js';

export const DEFAULT_SPEED = 130; // Logical pixels per second
export const WALK_FRAME_INTERVAL = 0.12; // Seconds per animation frame
export const SPEECH_BUBBLE_DURATION = 6000; // Milliseconds visible
export const CLICK_MARKER_DURATION = 600; // Milliseconds ripple animation
export const EXIT_TRIGGER_DISTANCE = 45; // Pixel distance to trigger room exit

export const DEFAULT_ROOM: RoomDefinition = {
  id: 'lounge',
  name: 'Lounge',
  width: 640,
  height: 360,
  backgroundUrl: 'assets/images/lounge.png',
  spawnPoint: {
    x: 120,
    y: 300,
  },
  spawns: [
    { id: 'entry', position: { x: 120, y: 300 } },
    { id: 'from-garden', position: { x: 540, y: 300 } },
  ],
  exits: [
    {
      id: 'to-garden',
      position: { x: 580, y: 300 },
      radius: 45,
      bounds: { x: 535, y: 75, width: 80, height: 260 },
      label: 'Tavernen-Garten',
      targetRoomId: 'garden',
      targetSpawnId: 'from-lounge',
    },
  ],
  geometry: {
    outerBoundary: [
      { x: 40, y: 240 },
      { x: 300, y: 220 },
      { x: 600, y: 240 },
      { x: 600, y: 345 },
      { x: 40, y: 345 },
    ],
    obstacles: [
      [
        { x: 250, y: 255 },
        { x: 390, y: 255 },
        { x: 390, y: 310 },
        { x: 250, y: 310 },
      ],
    ],
  },
};
