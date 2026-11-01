import type { RoomDefinition, Rect, Point } from '../types.js';
import type {
  RoomYamlDto,
  SelectedEntity,
  RoomExitDto
} from './editor-types.js';

export class EditorState {
  private currentRoom: RoomYamlDto;
  private selected: SelectedEntity = { type: null };
  private dirty = false;

  public constructor(initialRoom?: RoomYamlDto) {
    this.currentRoom = initialRoom ?? this.createDefaultRoom();
  }

  public getRoom(): RoomYamlDto {
    return this.currentRoom;
  }

  public setRoom(room: RoomYamlDto): void {
    this.currentRoom = JSON.parse(JSON.stringify(room)) as RoomYamlDto;
    this.selected = { type: null };
    this.dirty = false;
  }

  public isDirty(): boolean {
    return this.dirty;
  }

  public markClean(): void {
    this.dirty = false;
  }

  public getSelection(): SelectedEntity {
    return this.selected;
  }

  public setSelection(sel: SelectedEntity): void {
    this.selected = sel;
  }

  public clearSelection(): void {
    this.selected = { type: null };
  }

  public createDefaultRoom(): RoomYamlDto {
    return {
      version: 1,
      id: 'new-room',
      name: 'Neuer Raum',
      background: 'assets/images/lounge.png',
      size: { width: 640, height: 360 },
      spawns: [
        {
          id: 'entry',
          position: { x: 120, y: 300 }
        }
      ],
      walkable: [
        {
          id: 'floor',
          points: [
            { x: 50, y: 240 },
            { x: 590, y: 240 },
            { x: 590, y: 340 },
            { x: 50, y: 340 }
          ]
        }
      ],
      obstacles: [],
      exits: [],
      notes: ''
    };
  }

  public updateMetadata(metadata: Partial<Pick<RoomYamlDto, 'id' | 'name' | 'background' | 'notes'>>): void {
    if (metadata.id !== undefined) this.currentRoom.id = metadata.id;
    if (metadata.name !== undefined) this.currentRoom.name = metadata.name;
    if (metadata.background !== undefined) this.currentRoom.background = metadata.background;
    if (metadata.notes !== undefined) this.currentRoom.notes = metadata.notes;
    this.dirty = true;
  }

  public addWalkablePoint(x: number, y: number): void {
    if (!this.currentRoom.walkable || this.currentRoom.walkable.length === 0) {
      this.currentRoom.walkable = [{ id: 'floor', points: [] }];
    }
    const poly = this.currentRoom.walkable[0];
    if (poly) {
      poly.points.push({ x: Math.round(x), y: Math.round(y) });
      this.selected = {
        type: 'vertex',
        polyType: 'walkable',
        polyIndex: 0,
        vertexIndex: poly.points.length - 1
      };
      this.dirty = true;
    }
  }

  public addObstaclePoint(x: number, y: number, obstacleIndex = 0): void {
    if (!this.currentRoom.obstacles) {
      this.currentRoom.obstacles = [];
    }
    if (this.currentRoom.obstacles.length <= obstacleIndex) {
      this.currentRoom.obstacles.push({
        id: `obstacle-${this.currentRoom.obstacles.length + 1}`,
        points: []
      });
    }
    const obs = this.currentRoom.obstacles[obstacleIndex];
    if (obs) {
      obs.points.push({ x: Math.round(x), y: Math.round(y) });
      this.selected = {
        type: 'vertex',
        polyType: 'obstacle',
        polyIndex: obstacleIndex,
        vertexIndex: obs.points.length - 1
      };
      this.dirty = true;
    }
  }

  public moveVertex(polyType: 'walkable' | 'obstacle', polyIndex: number, vertexIndex: number, x: number, y: number): void {
    const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
    const poly = polys[polyIndex];
    if (poly && poly.points[vertexIndex]) {
      poly.points[vertexIndex] = { x: Math.round(x), y: Math.round(y) };
      this.dirty = true;
    }
  }

  public insertVertexOnEdge(
    polyType: 'walkable' | 'obstacle',
    polyIndex: number,
    edgeIndex: number,
    x: number,
    y: number
  ): number {
    const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
    const poly = polys[polyIndex];
    if (!poly) return -1;

    const newIndex = edgeIndex + 1;
    poly.points.splice(newIndex, 0, { x: Math.round(x), y: Math.round(y) });
    this.selected = {
      type: 'vertex',
      polyType,
      polyIndex,
      vertexIndex: newIndex
    };
    this.dirty = true;
    return newIndex;
  }

  public updateSelectedVertex(x: number, y: number): boolean {
    if (this.selected.type !== 'vertex' || this.selected.vertexIndex === undefined) return false;
    const polyType = this.selected.polyType ?? 'walkable';
    const polyIndex = this.selected.polyIndex ?? 0;
    const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
    const poly = polys[polyIndex];
    if (poly && poly.points[this.selected.vertexIndex]) {
      poly.points[this.selected.vertexIndex] = { x: Math.round(x), y: Math.round(y) };
      this.dirty = true;
      return true;
    }
    return false;
  }

  public updateSelectedSpawn(id: string, x: number, y: number): boolean {
    if (this.selected.type !== 'spawn' || !this.selected.spawnId) return false;
    const spawn = this.currentRoom.spawns.find(s => s.id === this.selected.spawnId);
    if (!spawn) return false;
    spawn.id = id;
    spawn.position = { x: Math.round(x), y: Math.round(y) };
    this.selected.spawnId = id;
    this.dirty = true;
    return true;
  }

  public updateSelectedExit(update: Partial<RoomExitDto>): boolean {
    if (this.selected.type !== 'exit' || !this.selected.exitId || !this.currentRoom.exits) return false;
    const exit = this.currentRoom.exits.find(e => e.id === this.selected.exitId);
    if (!exit) return false;
    if (update.id !== undefined) {
      exit.id = update.id;
      this.selected.exitId = update.id;
    }
    if (update.position !== undefined) {
      exit.position = { x: Math.round(update.position.x), y: Math.round(update.position.y) };
    }
    if (update.radius !== undefined) exit.radius = update.radius;
    if (update.label !== undefined) exit.label = update.label;
    if (update.target !== undefined) exit.target = update.target;
    if (update.bounds !== undefined) exit.bounds = update.bounds;
    this.dirty = true;
    return true;
  }

  public deleteSelectedEntity(): boolean {
    if (this.selected.type === 'vertex') {
      return this.deleteSelectedVertex();
    }
    if (this.selected.type === 'spawn' && this.selected.spawnId) {
      return this.deleteSpawn(this.selected.spawnId);
    }
    if (this.selected.type === 'exit' && this.selected.exitId) {
      return this.deleteExit(this.selected.exitId);
    }
    return false;
  }

  public deleteSelectedVertex(): boolean {
    if (this.selected.type !== 'vertex' || this.selected.vertexIndex === undefined) {
      return false;
    }

    const polyType = this.selected.polyType ?? 'walkable';
    const polyIndex = this.selected.polyIndex ?? 0;
    const vertexIndex = this.selected.vertexIndex;

    const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
    const poly = polys[polyIndex];
    if (!poly || poly.points.length <= vertexIndex) {
      return false;
    }

    poly.points.splice(vertexIndex, 1);
    this.selected = { type: null };
    this.dirty = true;
    return true;
  }

  public addOrUpdateSpawn(id: string, x: number, y: number): void {
    const existing = this.currentRoom.spawns.find(s => s.id.toLowerCase() === id.toLowerCase());
    if (existing) {
      existing.position = { x: Math.round(x), y: Math.round(y) };
    } else {
      this.currentRoom.spawns.push({
        id,
        position: { x: Math.round(x), y: Math.round(y) }
      });
    }
    this.selected = { type: 'spawn', spawnId: id };
    this.dirty = true;
  }

  public moveSpawn(id: string, x: number, y: number): void {
    const spawn = this.currentRoom.spawns.find(s => s.id === id);
    if (spawn) {
      spawn.position = { x: Math.round(x), y: Math.round(y) };
      this.dirty = true;
    }
  }

  public deleteSpawn(id: string): boolean {
    const idx = this.currentRoom.spawns.findIndex(s => s.id === id);
    if (idx >= 0) {
      this.currentRoom.spawns.splice(idx, 1);
      if (this.selected.type === 'spawn' && this.selected.spawnId === id) {
        this.selected = { type: null };
      }
      this.dirty = true;
      return true;
    }
    return false;
  }

  public addOrUpdateExit(exit: RoomExitDto): void {
    if (!this.currentRoom.exits) {
      this.currentRoom.exits = [];
    }
    const idx = this.currentRoom.exits.findIndex(e => e.id.toLowerCase() === exit.id.toLowerCase());
    if (idx >= 0) {
      this.currentRoom.exits[idx] = exit;
    } else {
      this.currentRoom.exits.push(exit);
    }
    this.selected = { type: 'exit', exitId: exit.id };
    this.dirty = true;
  }

  public moveExit(id: string, x: number, y: number): void {
    if (!this.currentRoom.exits) return;
    const exit = this.currentRoom.exits.find(e => e.id === id);
    if (exit) {
      exit.position = { x: Math.round(x), y: Math.round(y) };
      this.dirty = true;
    }
  }

  public deleteExit(id: string): boolean {
    if (!this.currentRoom.exits) return false;
    const idx = this.currentRoom.exits.findIndex(e => e.id === id);
    if (idx >= 0) {
      this.currentRoom.exits.splice(idx, 1);
      if (this.selected.type === 'exit' && this.selected.exitId === id) {
        this.selected = { type: null };
      }
      this.dirty = true;
      return true;
    }
    return false;
  }

  public toRuntimeRoomDefinition(): RoomDefinition {
    const { currentRoom } = this;
    const outerBoundary = currentRoom.walkable[0]?.points ?? [
      { x: 0, y: 0 },
      { x: currentRoom.size.width, y: 0 },
      { x: currentRoom.size.width, y: currentRoom.size.height },
      { x: 0, y: currentRoom.size.height }
    ];

    const obstacles = (currentRoom.obstacles ?? []).map(o => o.points);

    const defaultSpawn = currentRoom.spawns[0]?.position ?? {
      x: currentRoom.size.width / 2,
      y: currentRoom.size.height / 2
    };

    return {
      id: currentRoom.id,
      name: currentRoom.name,
      width: currentRoom.size.width,
      height: currentRoom.size.height,
      backgroundUrl: currentRoom.background,
      spawnPoint: defaultSpawn,
      geometry: {
        outerBoundary,
        obstacles
      },
      spawns: currentRoom.spawns.map(s => ({ id: s.id, position: s.position })),
      exits: (currentRoom.exits ?? []).map(e => {
        const exit: {
          id: string;
          position: Point;
          radius?: number;
          targetRoomId: string;
          targetSpawnId: string;
          bounds?: Rect;
          label?: string;
        } = {
          id: e.id,
          position: e.position,
          radius: e.radius,
          targetRoomId: e.target.room,
          targetSpawnId: e.target.spawn
        };
        if (e.bounds) exit.bounds = e.bounds;
        if (e.label) exit.label = e.label;
        return exit;
      })
    };
  }
}
