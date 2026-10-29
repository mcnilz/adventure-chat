export interface Point {
  readonly x: number;
  readonly y: number;
}

export interface RoomGeometry {
  readonly outerBoundary: ReadonlyArray<Point>;
  readonly obstacles: ReadonlyArray<ReadonlyArray<Point>>;
}

export type FacingDirection = 'left' | 'right' | 'up' | 'down';
export type CharacterState = 'idle' | 'walk';

export interface Character {
  id: string;
  name: string;
  position: Point;
  target: Point | null;
  waypoints: Point[];
  state: CharacterState;
  facing: FacingDirection;
  speed: number; // logical pixels per second
  walkFrame: number;
  frameTimer: number;
}

export interface RoomDefinition {
  readonly id: string;
  readonly name: string;
  readonly width: number;
  readonly height: number;
  readonly backgroundUrl: string;
  readonly geometry: RoomGeometry;
  readonly spawnPoint: Point;
}
