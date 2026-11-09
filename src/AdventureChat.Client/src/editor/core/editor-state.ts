import type { RoomDefinition } from '../../types.js';
import { deleteSelected, editSelected } from './editor-entities.js';
import { EditorGeometry } from './editor-geometry.js';
import { applyRoomProperties, clampPosition, createRoomObject, validRoomObject } from './editor-room-properties.js';
import type {
  EntityEdit,
  HoveredEdge,
  RoomExitDto,
  RoomObjectDto,
  RoomPointDto,
  RoomPropertiesEdit,
  RoomYamlDto,
  RoomView,
  SelectedEntity
} from './editor-types.js';

export interface EditorSave {
  readonly room: RoomView;
  readonly draft: number;
  readonly revision: number;
}

export class EditorState {
  private currentRoom: RoomYamlDto;
  private selected: SelectedEntity = { type: null };
  private dirty = false;
  private content: string;
  private draft = 0;
  private revision = 0;

  public constructor(initialRoom?: RoomYamlDto) {
    this.currentRoom = structuredClone(initialRoom ?? this.createDefaultRoom());
    this.content = JSON.stringify(this.currentRoom);
  }

  public getRoom(): RoomView {
    return this.currentRoom;
  }

  public setRoom(room: RoomYamlDto): void {
    this.currentRoom = structuredClone(room);
    this.selected = { type: null };
    this.dirty = false;
    this.content = JSON.stringify(this.currentRoom);
    this.draft++;
    this.revision = 0;
  }

  public isDirty(): boolean {
    return this.dirty;
  }

  public getEditVersion(): string {
    return `${this.draft}:${this.revision}`;
  }

  public addObject(object = createRoomObject(this.currentRoom)): boolean {
    if ((this.currentRoom.objects?.length ?? 0) >= 128 || !validRoomObject(this.currentRoom, object)) return false;
    this.currentRoom.objects ??= [];
    this.currentRoom.objects.push(structuredClone(object));
    this.selected = { type: 'object', objectId: object.id };
    this.changed();
    return true;
  }

  public updateObject(id: string, object: RoomObjectDto): boolean {
    const index = this.currentRoom.objects?.findIndex(o => o.id === id) ?? -1;
    if (index < 0 || !this.currentRoom.objects || !validRoomObject(this.currentRoom, object, id)) return false;
    this.currentRoom.objects[index] = structuredClone(object);
    this.selected = { type: 'object', objectId: object.id };
    this.changed();
    return true;
  }

  public moveObject(id: string, dx: number, dy: number): void {
    const object = this.currentRoom.objects?.find(o => o.id === id);
    if (!object) return;
    dx = Math.round(Math.max(-object.position.x, Math.min(this.currentRoom.size.width-object.position.x-object.size.width, dx)));
    dy = Math.round(Math.max(-object.position.y, Math.min(this.currentRoom.size.height-object.position.y-object.size.height, dy)));
    this.updateObject(id, { ...object,
      position: { x: object.position.x+dx, y: object.position.y+dy },
      depthY: Math.max(0, Math.min(this.currentRoom.size.height, object.depthY+dy))
    });
  }

  public beginSave(): EditorSave {
    return { room: structuredClone(this.currentRoom), draft: this.draft, revision: this.revision };
  }

  public isCurrentDraft(save: EditorSave): boolean {
    return this.draft === save.draft;
  }

  public completeSave(save: EditorSave): void {
    if (this.isCurrentDraft(save) && this.revision === save.revision) this.dirty = false;
  }

  public getSelection(): Readonly<SelectedEntity> {
    return this.selected;
  }

  public setSelection(sel: SelectedEntity): void {
    this.selected = { ...sel };
  }

  public clearSelection(): void {
    this.selected = { type: null };
  }

  public createDefaultRoom(): RoomYamlDto {
    return EditorGeometry.createDefaultRoomYaml();
  }

  public updateMetadata(metadata: Partial<Pick<RoomYamlDto, 'id' | 'name' | 'background' | 'notes'>>): void {
    if (metadata.id !== undefined) this.currentRoom.id = metadata.id;
    if (metadata.name !== undefined) this.currentRoom.name = metadata.name;
    if (metadata.background !== undefined) this.currentRoom.background = metadata.background;
    if (metadata.notes !== undefined) this.currentRoom.notes = metadata.notes;
    this.changed();
  }

  public updateProperties(edit: RoomPropertiesEdit): void {
    applyRoomProperties(this.currentRoom, edit);
    this.changed();
  }

  public updateSelected(edit: EntityEdit): void {
    this.selected = editSelected(this.currentRoom, this.selected, edit);
    this.changed();
  }

  public addWalkablePoint(x: number, y: number): void {
    this.addPoint('walkable', 0, x, y);
  }

  public addObstaclePoint(x: number, y: number): void {
    this.addPoint('obstacle', (this.currentRoom.obstacles?.length ?? 1) - 1, x, y);
  }

  public addWalkablePolygon(x: number, y: number, width = 80, height = 60): number {
    return this.addPolygon('walkable', x, y, width, height);
  }

  public addObstaclePolygon(x: number, y: number, width = 60, height = 40): number {
    return this.addPolygon('obstacle', x, y, width, height);
  }

  private addPolygon(type: 'walkable' | 'obstacle', x: number, y: number, width: number, height: number): number {
    const list = type === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ??= []);
    const id = (type === 'walkable' ? 'floor-' : 'obs-') + (list.length + 1);
    list.push(EditorGeometry.createDefaultBoxPolygon(id, x, y, width, height,
      this.currentRoom.size.width, this.currentRoom.size.height));
    const index = list.length - 1;
    this.selected = { type: 'polygon', polyType: type, polyIndex: index };
    this.changed();
    return index;
  }

  private addPoint(type: 'walkable' | 'obstacle', index: number, x: number, y: number): void {
    const list = type === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
    const poly = list?.[index];
    if (!poly || poly.points.length < 3) {
      if (type === 'walkable') this.addWalkablePolygon(x, y); else this.addObstaclePolygon(x, y);
      return;
    }
    poly.points.push({ x: Math.round(x), y: Math.round(y) });
    this.selected = { type: 'vertex', polyType: type, polyIndex: index, vertexIndex: poly.points.length - 1 };
    this.changed();
  }

  public splitEdge(edge: HoveredEdge): void {
    const list = edge.polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
    if (!list) return;
    const poly = list[edge.polyIndex];
    if (!poly) return;

    poly.points.splice(edge.edgeIndex + 1, 0, { ...edge.splitPoint });
    this.selected = {
      type: 'vertex',
      polyType: edge.polyType,
      polyIndex: edge.polyIndex,
      vertexIndex: edge.edgeIndex + 1
    };
    this.changed();
  }

  public addSpawn(pos: RoomPointDto, id?: string): void {
    const nextNum = this.currentRoom.spawns.length + 1;
    const spawnId = id ?? (nextNum === 1 ? 'entry' : `spawn-${nextNum}`);
    this.currentRoom.spawns.push({
      id: spawnId,
      position: { x: Math.round(pos.x), y: Math.round(pos.y) }
    });
    this.selected = { type: 'spawn', spawnId };
    this.changed();
  }

  public addOrUpdateSpawn(id: string, x: number, y: number): void {
    const existing = this.currentRoom.spawns.find(s => s.id === id);
    if (existing) {
      existing.position = { x: Math.round(x), y: Math.round(y) };
    } else {
      this.currentRoom.spawns.push({ id, position: { x: Math.round(x), y: Math.round(y) } });
    }
    this.selected = { type: 'spawn', spawnId: id };
    this.changed();
  }

  public addExit(exitOrPos: RoomPointDto | RoomExitDto): void {
    if (!this.currentRoom.exits) {
      this.currentRoom.exits = [];
    }

    if ('target' in exitOrPos) {
      this.currentRoom.exits.push(structuredClone(exitOrPos));
      this.selected = { type: 'exit', exitId: exitOrPos.id };
    } else {
      const nextNum = this.currentRoom.exits.length + 1;
      const exitId = `exit-${nextNum}`;
      const newExit: RoomExitDto = {
        id: exitId,
        position: { x: Math.round(exitOrPos.x), y: Math.round(exitOrPos.y) },
        radius: 35,
        target: {
          room: 'garden',
          spawn: 'entry'
        },
        label: 'Nach Garten'
      };
      this.currentRoom.exits.push(newExit);
      this.selected = { type: 'exit', exitId };
    }
    this.changed();
  }

  public moveVertex(polyType: 'walkable' | 'obstacle', polyIdx: number, vIdx: number, pos: RoomPointDto): void {
    const list = polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
    if (!list) return;
    const poly = list[polyIdx];
    if (poly && poly.points[vIdx]) {
      poly.points[vIdx] = clampPosition(this.currentRoom, pos, poly.points[vIdx]);
      this.changed();
    }
  }

  public movePolygon(polyType: 'walkable' | 'obstacle', polyIdx: number, dx: number, dy: number): void {
    const list = polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
    if (!list) return;
    const poly = list[polyIdx];
    if (poly && poly.points.length > 0) {
      poly.points = EditorGeometry.translatePoints(
        poly.points,
        dx,
        dy,
        this.currentRoom.size.width,
        this.currentRoom.size.height
      );
      this.changed();
    }
  }

  public moveSpawn(id: string, pos: RoomPointDto): void {
    const spawn = this.currentRoom.spawns.find(s => s.id === id);
    if (spawn) {
      spawn.position = clampPosition(this.currentRoom, pos, spawn.position);
      this.changed();
    }
  }

  public moveExit(id: string, pos: RoomPointDto): void {
    if (!this.currentRoom.exits) return;
    const exit = this.currentRoom.exits.find(e => e.id === id);
    if (exit) {
      exit.position = clampPosition(this.currentRoom, pos, exit.position);
      this.changed();
    }
  }

  public deleteSelected(): boolean {
    if (!deleteSelected(this.currentRoom, this.selected)) return false;
    this.clearSelection();
    this.changed();
    return true;
  }

  // Compare only committed edits, never on the render/hover path. Selection alone is not an edit.
  private changed(): void {
    const content = JSON.stringify(this.currentRoom);
    if (content === this.content) return;
    this.content = content;
    this.revision++;
    this.dirty = true;
  }

  public toRoomDefinition(): RoomDefinition {
    return EditorGeometry.roomYamlToRoomDefinition(this.currentRoom);
  }
}
