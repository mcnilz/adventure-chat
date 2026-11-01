export class EditorInspector {
    elements;
    exitDialog;
    onUpdateEntity;
    onDeleteEntity;
    constructor(elements, exitDialog, onUpdateEntity, onDeleteEntity) {
        this.elements = elements;
        this.exitDialog = exitDialog;
        this.onUpdateEntity = onUpdateEntity;
        this.onDeleteEntity = onDeleteEntity;
        this.bindEvents();
    }
    update(room, selection) {
        const { elements: el } = this;
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
        if (selection.type === 'vertex' &&
            selection.polyType &&
            selection.polyIndex !== undefined &&
            selection.vertexIndex !== undefined) {
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
            }
            else {
                el.inspectorEmpty.style.display = 'block';
            }
        }
        else if (selection.type === 'polygon' &&
            selection.polyType &&
            selection.polyIndex !== undefined) {
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
            }
            else {
                el.inspectorEmpty.style.display = 'block';
            }
        }
        else if (selection.type === 'spawn' && selection.spawnId) {
            const spawn = room.spawns.find(s => s.id === selection.spawnId);
            if (spawn) {
                el.inspectorSpawn.style.display = 'block';
                el.inspectorSpawnId.value = spawn.id;
                el.inspectorSpawnX.value = String(spawn.position.x);
                el.inspectorSpawnY.value = String(spawn.position.y);
            }
            else {
                el.inspectorEmpty.style.display = 'block';
            }
        }
        else if (selection.type === 'exit' && selection.exitId && room.exits) {
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
            }
            else {
                el.inspectorEmpty.style.display = 'block';
            }
        }
        else {
            el.inspectorEmpty.style.display = 'block';
        }
    }
    applyVertexChanges(room, selection) {
        if (selection.type === 'vertex' &&
            selection.polyType &&
            selection.polyIndex !== undefined &&
            selection.vertexIndex !== undefined) {
            const list = selection.polyType === 'walkable' ? room.walkable : room.obstacles;
            const poly = list ? list[selection.polyIndex] : null;
            const pt = poly ? poly.points[selection.vertexIndex] : null;
            if (pt) {
                const nx = parseInt(this.elements.inspectorVertexX.value, 10);
                const ny = parseInt(this.elements.inspectorVertexY.value, 10);
                if (!isNaN(nx))
                    pt.x = Math.max(0, Math.min(room.size.width, nx));
                if (!isNaN(ny))
                    pt.y = Math.max(0, Math.min(room.size.height, ny));
                return true;
            }
        }
        return false;
    }
    applySpawnChanges(room, selection) {
        if (selection.type === 'spawn' && selection.spawnId) {
            const spawn = room.spawns.find(s => s.id === selection.spawnId);
            if (spawn) {
                const nid = this.elements.inspectorSpawnId.value.trim();
                if (nid) {
                    spawn.id = nid;
                    selection.spawnId = nid;
                }
                const nx = parseInt(this.elements.inspectorSpawnX.value, 10);
                const ny = parseInt(this.elements.inspectorSpawnY.value, 10);
                if (!isNaN(nx))
                    spawn.position.x = Math.max(0, Math.min(room.size.width, nx));
                if (!isNaN(ny))
                    spawn.position.y = Math.max(0, Math.min(room.size.height, ny));
                return true;
            }
        }
        return false;
    }
    applyExitChanges(room, selection) {
        if (selection.type === 'exit' && selection.exitId && room.exits) {
            const exit = room.exits.find(e => e.id === selection.exitId);
            if (exit) {
                const nid = this.elements.inspectorExitId.value.trim();
                if (nid) {
                    exit.id = nid;
                    selection.exitId = nid;
                }
                exit.target.room = this.elements.inspectorExitRoom.value.trim();
                exit.target.spawn = this.elements.inspectorExitTargetSpawn.value.trim() || 'entry';
                exit.label = this.elements.inspectorExitLabel.value.trim() || null;
                const nx = parseInt(this.elements.inspectorExitX.value, 10);
                const ny = parseInt(this.elements.inspectorExitY.value, 10);
                const nr = parseInt(this.elements.inspectorExitRadius.value, 10);
                if (!isNaN(nx))
                    exit.position.x = Math.max(0, Math.min(room.size.width, nx));
                if (!isNaN(ny))
                    exit.position.y = Math.max(0, Math.min(room.size.height, ny));
                if (!isNaN(nr))
                    exit.radius = Math.max(5, Math.min(200, nr));
                return true;
            }
        }
        return false;
    }
    bindEvents() {
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
//# sourceMappingURL=editor-inspector.js.map