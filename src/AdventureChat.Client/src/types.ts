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

export interface CharacterAppearance {
  readonly id: string;
  readonly name: string;
  readonly imageUrl: string;
  readonly topDownImageUrl?: string | null;
  readonly frameWidth: number;
  readonly frameHeight: number;
}
export interface CharacterCatalog {
  readonly defaultCharacterId: string | null;
  readonly characters: ReadonlyArray<CharacterAppearance>;
}

export interface Character {
  actorAnimation?: ActorAnimationDto & { readonly startedAt: number } | null;
  appearance?: CharacterAppearance | undefined;
  characterId?: string | null | undefined;
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

export interface RoomViewport {
  width: number;
  height: number;
  scrollMode: 'horizontal';
}

export interface RoomObject {
 readonly id: string;
 readonly image: string;
 readonly position: Point;
 readonly size: { readonly width: number; readonly height: number };
 readonly depthY: number;
 readonly interaction?: ObjectInteraction | null;
 readonly usePoints?: ReadonlyArray<Point> | null;
}

export interface RenderedRoomObject extends RoomObject {
 readonly movement?: ObjectMovement | null;
}

export interface ObjectInteraction {
 readonly name: string;
 readonly initialState: string;
 readonly usePoint?: Point | null;
 readonly useRadius: number;
 readonly states: ReadonlyArray<ObjectAppearance>;
 readonly actions: ReadonlyArray<ObjectAction>;
 readonly transitions?: ReadonlyArray<ObjectTransition> | null;
}
export interface ObjectTransition {
 readonly from: string;
 readonly to: string;
 readonly durationMs: number;
}
export interface ObjectMovement {
 readonly from: ObjectAppearance;
 readonly startedAtMs: number;
 readonly startTime: number;
 readonly durationMs: number;
}
export interface ObjectAction {
 readonly id: string;
 readonly name: string;
 readonly from: string;
 readonly to: string;
 readonly effects?: ReadonlyArray<{ readonly room: string; readonly object: string; readonly state: string }> | null;
 readonly actorAnimation?: ObjectActorAnimation | null;
}
export interface ObjectActorAnimation {
 readonly point: number;
 readonly direction: FacingDirection;
 readonly durationMs?: number | null;
 readonly sprites?: Readonly<Record<string, string>> | null;
}
export interface ActorAnimationDto {
 readonly image: string | null;
 readonly durationMs: number | null;
 readonly elapsedMs: number;
}
export type InteractionVerb = 'walk' | 'open' | 'close' | 'pull' | 'push' | 'use' | 'talk';
export interface NpcPresence {
  readonly id: string;
  readonly name: string;
  readonly characterId: string;
  readonly position: Point;
  readonly talkPoint: Point;
}
export interface DialogueOption {
  readonly id: string;
  readonly text: string;
  readonly reply: string;
}
export interface NpcDialogueResult {
  readonly error: string | null;
  readonly options: readonly DialogueOption[] | null;
  readonly tooFar: boolean;
}
export interface ObjectAppearance {
 readonly obstacles?: ReadonlyArray<{ readonly id: string; readonly points: ReadonlyArray<Point> }> | null;
 readonly blockedExits?: ReadonlyArray<string> | null;
 readonly id: string;
 readonly name: string;
 readonly image: string;
 readonly position: Point;
 readonly size: { readonly width: number; readonly height: number };
 readonly depthY: number;
}
export interface ObjectStateDto {
 readonly objectId: string;
 readonly stateId: string;
 readonly transition?: {
  readonly from: string;
  readonly startedAtMs: number;
  readonly durationMs: number;
  readonly elapsedMs: number;
 } | null;
}
export interface PreviewObjectStates { readonly roomId: string; readonly states: ReadonlyArray<ObjectStateDto> }
export interface ObjectStatesChangedDto {
 readonly roomId: string;
 readonly revision: number;
 readonly states: ReadonlyArray<ObjectStateDto>;
 readonly geometry?: RoomGeometry | null;
 readonly exits?: ReadonlyArray<RoomExit> | null;
 readonly players?: ReadonlyArray<PlayerDto> | null;
 readonly stoppedSessions?: ReadonlyArray<string> | null;
}

export interface DepthScale {
  readonly back: { readonly y: number; readonly factor: number };
  readonly front: { readonly y: number; readonly factor: number };
}

export interface RoomDefinition {
  readonly npcs?: readonly NpcPresence[] | null;
  depthScale?: DepthScale | null;
  objects?: ReadonlyArray<RoomObject> | null;
  viewport?: RoomViewport | null;
  perspective?: 'adventure' | 'top-down';
  actorHeight?: number;
  walkSpeed?: number;
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

export type RenderedRoom = Omit<RoomDefinition, 'objects'> & {
  objects?: ReadonlyArray<RenderedRoomObject> | null;
};

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
  readonly actorAnimation?: ActorAnimationDto | null;
  readonly characterId?: string | null | undefined;
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
  readonly objectRevision?: number;
  readonly objectStates?: ReadonlyArray<ObjectStateDto> | null;
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
  readonly worldId?: string | null;
  readonly characterId?: string | null;
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

export interface ObjectActionResultDto {
  readonly success: boolean;
  readonly error: string | null;
  readonly errorCode?: string | null;
  readonly pending?: boolean;
}
export interface ObjectActionFeedbackDto { readonly roomId: string; readonly message: string }

export interface RequestMoveDto {
  readonly start?: Point | null;
  readonly target: Point;
}
