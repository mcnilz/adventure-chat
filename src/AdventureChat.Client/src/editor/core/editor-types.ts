export type EditorMode = 'edit' | 'preview';

export type EditorTool = 'select' | 'add-walkable' | 'add-obstacle' | 'add-spawn' | 'add-exit';

export interface RoomPointDto {
  x: number;
  y: number;
}

export interface RoomSpawnDto {
  id: string;
  position: RoomPointDto;
}

export interface RoomPolygonDto {
  id: string;
  points: RoomPointDto[];
}

export interface RoomRectDto {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface RoomExitTargetDto {
  room: string;
  spawn: string;
}

export interface RoomExitDto {
  id: string;
  position: RoomPointDto;
  radius: number;
  bounds?: RoomRectDto | null;
  label?: string | null;
  target: RoomExitTargetDto;
}

export interface RoomObjectDto {
 id: string; image: string; position: RoomPointDto; size: {width: number; height: number}; depthY: number;
}

export interface RoomYamlDto {
  depthScale?: import('../../types.js').DepthScale | null;
  objects?: RoomObjectDto[] | null;
  viewport?: import('../../types.js').RoomViewport | null;
  perspective?: 'adventure' | 'top-down';
  actorHeight?: number;
  walkSpeed?: number;
  version: number;
  id: string;
  name: string;
  background: string;
  size: {
    width: number;
    height: number;
  };
  spawns: RoomSpawnDto[];
  walkable: RoomPolygonDto[];
  obstacles?: RoomPolygonDto[] | null;
  exits?: RoomExitDto[] | null;
  notes?: string | null;
}

export interface RoomSummaryDto {
  id: string;
  name: string;
  background: string;
  width: number;
  height: number;
}

export interface SelectedEntity {
  type: 'vertex' | 'polygon' | 'spawn' | 'exit' | 'object' | null;
  polyType?: 'walkable' | 'obstacle';
  polyIndex?: number;
  vertexIndex?: number;
  objectId?: string;
  spawnId?: string;
  exitId?: string;
}

export interface HoveredEdge {
  polyType: 'walkable' | 'obstacle';
  polyIndex: number;
  edgeIndex: number;
  splitPoint: RoomPointDto;
}

export interface BackgroundItemDto {
  name: string;
  path: string;
  sizeBytes: number;
  lastModified: string;
}

export interface BackgroundsOverviewDto {
  filesystemDirectory: string;
  relativePath: string;
  backgrounds: BackgroundItemDto[];
}
