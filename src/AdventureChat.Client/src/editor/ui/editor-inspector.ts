import type { EditorExitDialog } from './editor-exit-dialog.js';
import type { EntityEdit, RoomView, SelectedEntity } from '../core/editor-types.js';

export interface InspectorElements {
  inspectorEmpty: HTMLElement;
  // Vertex
  inspectorVertex: HTMLElement;
  inspectorVertexBadge: HTMLElement;
  inspectorVertexX: HTMLInputElement;
  inspectorVertexY: HTMLInputElement;
  btnInspectorDeleteVertex: HTMLButtonElement;
  // Polygon
  inspectorPolygon: HTMLElement;
  inspectorPolygonBadge: HTMLElement;
  inspectorPolygonId: HTMLInputElement;
  inspectorPolygonPointsCount: HTMLInputElement;
  btnInspectorDeletePolygon: HTMLButtonElement;
  // Spawn
  inspectorSpawn: HTMLElement;
  inspectorSpawnId: HTMLInputElement;
  inspectorSpawnX: HTMLInputElement;
  inspectorSpawnY: HTMLInputElement;
  btnInspectorDeleteSpawn: HTMLButtonElement;
  // Exit
  inspectorExit: HTMLElement;
  inspectorExitId: HTMLInputElement;
  inspectorExitRoom: HTMLSelectElement;
  inspectorExitTargetSpawn: HTMLSelectElement;
  inspectorExitLabel: HTMLInputElement;
  inspectorExitX: HTMLInputElement;
  inspectorExitY: HTMLInputElement;
  inspectorExitRadius: HTMLInputElement;
  btnInspectorDeleteExit: HTMLButtonElement;
}

export class EditorInspector {
  public constructor(
    private readonly elements: InspectorElements,
    private readonly exitDialog: EditorExitDialog,
    private readonly onUpdateEntity: () => void,
    private readonly onDeleteEntity: () => void
  ) {
    this.bindEvents();
  }

  public update(room: RoomView, selection: Readonly<SelectedEntity>): void {
    const { elements: el } = this;
    for (const input of [el.inspectorVertexX, el.inspectorSpawnX, el.inspectorExitX]) {
      input.max = String(room.size.width);
      input.title = `X-Koordinate (0 bis ${room.size.width})`;
    }
    for (const input of [el.inspectorVertexY, el.inspectorSpawnY, el.inspectorExitY]) {
      input.max = String(room.size.height);
      input.title = `Y-Koordinate (0 bis ${room.size.height})`;
    }

    // Reset visibility
    el.inspectorEmpty.style.display = 'none';
    el.inspectorVertex.style.display = 'none';
    el.inspectorPolygon.style.display = 'none';
    el.inspectorSpawn.style.display = 'none';
    el.inspectorExit.style.display = 'none';

    if (!selection || !selection.type) {
      el.inspectorEmpty.style.display = 'block';
      return;
    }

    if (
      selection.type === 'vertex' &&
      selection.polyType &&
      selection.polyIndex !== undefined &&
      selection.vertexIndex !== undefined
    ) {
      const list = selection.polyType === 'walkable' ? room.walkable : room.obstacles;
      const poly = list ? list[selection.polyIndex] : null;
      const pt = poly ? poly.points[selection.vertexIndex] : null;
      if (pt) {
        el.inspectorVertex.style.display = 'block';
        el.inspectorVertexBadge.textContent =
          selection.polyType === 'walkable'
            ? `🟩 Bodenpunkt #${selection.vertexIndex + 1}`
            : `🟥 Hindernispunkt #${selection.vertexIndex + 1}`;
        el.inspectorVertexX.value = String(pt.x);
        el.inspectorVertexY.value = String(pt.y);
      } else {
        el.inspectorEmpty.style.display = 'block';
      }
    } else if (
      selection.type === 'polygon' &&
      selection.polyType &&
      selection.polyIndex !== undefined
    ) {
      const list = selection.polyType === 'walkable' ? room.walkable : room.obstacles;
      const poly = list ? list[selection.polyIndex] : null;
      if (poly) {
        el.inspectorPolygon.style.display = 'block';
        el.inspectorPolygonBadge.textContent =
          selection.polyType === 'walkable' ? '🟩 Begehbare Fläche' : '🟥 Hindernis-Fläche';
        el.inspectorPolygonBadge.className =
          selection.polyType === 'walkable'
            ? 'inspector-badge'
            : 'inspector-badge badge-obstacle';
        el.inspectorPolygonId.value = poly.id;
        el.inspectorPolygonPointsCount.value = `${poly.points.length} Eckpunkte`;
      } else {
        el.inspectorEmpty.style.display = 'block';
      }
    } else if (selection.type === 'spawn' && selection.spawnId) {
      const spawn = room.spawns.find(s => s.id === selection.spawnId);
      if (spawn) {
        el.inspectorSpawn.style.display = 'block';
        el.inspectorSpawnId.value = spawn.id;
        el.inspectorSpawnX.value = String(spawn.position.x);
        el.inspectorSpawnY.value = String(spawn.position.y);
      } else {
        el.inspectorEmpty.style.display = 'block';
      }
    } else if (selection.type === 'exit' && selection.exitId && room.exits) {
      const exit = room.exits.find(e => e.id === selection.exitId);
      if (exit) {
        el.inspectorExit.style.display = 'block';
        el.inspectorExitId.value = exit.id;

        void this.exitDialog.populateRoomsSelect(el.inspectorExitRoom, exit.target.room, room.id).then(() => {
          void this.exitDialog.populateSpawnsSelect(el.inspectorExitTargetSpawn, exit.target.room, exit.target.spawn);
        });

        el.inspectorExitLabel.value = exit.label ?? '';
        el.inspectorExitX.value = String(exit.position.x);
        el.inspectorExitY.value = String(exit.position.y);
        el.inspectorExitRadius.value = String(exit.radius || 35);
      } else {
        el.inspectorEmpty.style.display = 'block';
      }
    } else {
      el.inspectorEmpty.style.display = 'block';
    }
  }

  public readEdit(type: SelectedEntity['type']): EntityEdit | null {
    const el = this.elements;
    const integer = (input: HTMLInputElement) => parseInt(input.value, 10);
    if (type === 'vertex') return { type, x: integer(el.inspectorVertexX), y: integer(el.inspectorVertexY) };
    if (type === 'spawn') return {
      type, id: el.inspectorSpawnId.value, x: integer(el.inspectorSpawnX), y: integer(el.inspectorSpawnY)
    };
    if (type === 'exit') return {
      type, id: el.inspectorExitId.value, x: integer(el.inspectorExitX), y: integer(el.inspectorExitY),
      radius: integer(el.inspectorExitRadius), room: el.inspectorExitRoom.value,
      spawn: el.inspectorExitTargetSpawn.value, label: el.inspectorExitLabel.value
    };
    return null;
  }

  private bindEvents(): void {
    const { elements: el } = this;

    // Vertex inputs
    el.inspectorVertexX.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorVertexY.addEventListener('input', () => this.onUpdateEntity());
    el.btnInspectorDeleteVertex.addEventListener('click', () => this.onDeleteEntity());

    // Polygon inputs
    el.btnInspectorDeletePolygon.addEventListener('click', () => this.onDeleteEntity());

    // Spawn inputs
    el.inspectorSpawnId.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorSpawnX.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorSpawnY.addEventListener('input', () => this.onUpdateEntity());
    el.btnInspectorDeleteSpawn.addEventListener('click', () => this.onDeleteEntity());

    // Exit inputs
    el.inspectorExitId.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorExitRoom.addEventListener('change', async () => {
      await this.exitDialog.populateSpawnsSelect(el.inspectorExitTargetSpawn, el.inspectorExitRoom.value);
      this.onUpdateEntity();
    });
    el.inspectorExitTargetSpawn.addEventListener('change', () => this.onUpdateEntity());
    el.inspectorExitLabel.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorExitX.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorExitY.addEventListener('input', () => this.onUpdateEntity());
    el.inspectorExitRadius.addEventListener('input', () => this.onUpdateEntity());
    el.btnInspectorDeleteExit.addEventListener('click', () => this.onDeleteEntity());
  }
}
