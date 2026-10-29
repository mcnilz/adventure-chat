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

export interface PlayerDto {
  readonly sessionId: string;
  readonly displayName: string;
  readonly position: Point;
  readonly direction: FacingDirection;
  readonly state: CharacterState;
  readonly target: Point | null;
  readonly waypoints: ReadonlyArray<Point>;
}

export interface RoomSnapshotDto {
  readonly roomId: string;
  readonly yourSessionId: string;
  readonly players: ReadonlyArray<PlayerDto>;
}

export interface PlayerMovedDto {
  readonly sessionId: string;
  readonly target: Point;
  readonly waypoints: ReadonlyArray<Point>;
}

export interface UserJoinedDto {
  readonly player: PlayerDto;
}

export interface UserLeftDto {
  readonly sessionId: string;
  readonly displayName: string;
}

export interface JoinRoomRequestDto {
  readonly roomId: string;
  readonly displayName: string;
}

export interface JoinRoomResultDto {
  readonly success: boolean;
  readonly error: string | null;
  readonly snapshot: RoomSnapshotDto | null;
}

export interface MoveResultDto {
  readonly success: boolean;
  readonly error: string | null;
}
