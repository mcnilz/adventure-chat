export interface Point {
  readonly x: number;
  readonly y: number;
}

export interface RoomGeometry {
  readonly outerBoundary: ReadonlyArray<Point>;
  readonly obstacles: ReadonlyArray<ReadonlyArray<Point>>;
}

export type FacingDirection = 'left' | 'right' | 'up' | 'down';
export type Direction = FacingDirection;
export type CharacterState = 'idle' | 'walk';

export interface SpeechBubble {
  readonly text: string;
  readonly startTime: number;
  readonly duration: number; // in milliseconds (e.g. 6000ms = 6s)
}

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
  speechBubble?: SpeechBubble | null;
}

export interface RoomSpawn {
  readonly id: string;
  readonly position: Point;
}

export interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

export interface RoomExit {
  readonly id: string;
  readonly position: Point;
  readonly radius?: number;
  readonly bounds?: Rect;
  readonly label?: string;
  readonly targetRoomId: string;
  readonly targetSpawnId: string;
}

export interface RoomDefinition {
  readonly id: string;
  readonly name: string;
  readonly width: number;
  readonly height: number;
  readonly backgroundUrl: string;
  readonly geometry: RoomGeometry;
  readonly spawnPoint: Point;
  readonly spawns?: ReadonlyArray<RoomSpawn>;
  readonly exits?: ReadonlyArray<RoomExit>;
}

export interface ChatMessageDto {
  readonly messageId: string;
  readonly sessionId: string;
  readonly senderName: string;
  readonly text: string;
  readonly timestamp: string;
  readonly roomId: string;
}

export interface SendChatMessageRequestDto {
  readonly text: string;
}

export interface SendChatMessageResultDto {
  readonly success: boolean;
  readonly error: string | null;
  readonly message: ChatMessageDto | null;
}

export interface PlayerDto {
  readonly sessionId: string;
  readonly displayName: string;
  readonly position: Point;
  readonly direction: FacingDirection;
  readonly state: CharacterState;
  readonly target: Point | null;
  readonly waypoints: ReadonlyArray<Point>;
  readonly movementRevision: number;
}

export interface RoomSnapshotDto {
  readonly roomId: string;
  readonly yourSessionId: string;
  readonly players: ReadonlyArray<PlayerDto>;
  readonly recentMessages?: ReadonlyArray<ChatMessageDto>;
  readonly room?: RoomDefinition;
}

export interface PlayerMovedDto {
  readonly sessionId: string;
  readonly target: Point;
  readonly waypoints: ReadonlyArray<Point>;
  readonly roomId: string;
  readonly revision: number;
}

export interface UserJoinedDto {
  readonly player: PlayerDto;
  readonly roomId: string;
}

export interface UserLeftDto {
  readonly sessionId: string;
  readonly displayName: string;
  readonly roomId: string;
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

export interface ChangeRoomRequestDto {
  readonly exitId: string;
}

export interface ChangeRoomResultDto {
  readonly success: boolean;
  readonly error: string | null;
  readonly snapshot: RoomSnapshotDto | null;
}

export interface MoveResultDto {
  readonly success: boolean;
  readonly error: string | null;
}

export interface RequestMoveDto {
  readonly start?: Point | null;
  readonly target: Point;
}
