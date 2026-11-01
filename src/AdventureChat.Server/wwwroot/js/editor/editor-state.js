import { EditorGeometry } from './editor-geometry.js';
export class EditorState {
    currentRoom;
    selected = { type: null };
    dirty = false;
    constructor(initialRoom) {
        this.currentRoom = initialRoom ?? this.createDefaultRoom();
    }
    getRoom() {
        return this.currentRoom;
    }
    setRoom(room) {
        this.currentRoom = JSON.parse(JSON.stringify(room));
        this.selected = { type: null };
        this.dirty = false;
    }
    isDirty() {
        return this.dirty;
    }
    markClean() {
        this.dirty = false;
    }
    getSelection() {
        return this.selected;
    }
    setSelection(sel) {
        this.selected = sel;
    }
    clearSelection() {
        this.selected = { type: null };
    }
    createDefaultRoom() {
        return EditorGeometry.createDefaultRoomYaml();
    }
    updateMetadata(metadata) {
        if (metadata.id !== undefined)
            this.currentRoom.id = metadata.id;
        if (metadata.name !== undefined)
            this.currentRoom.name = metadata.name;
        if (metadata.background !== undefined)
            this.currentRoom.background = metadata.background;
        if (metadata.notes !== undefined)
            this.currentRoom.notes = metadata.notes;
        this.dirty = true;
    }
    addWalkablePoint(x, y) {
        if (!this.currentRoom.walkable || this.currentRoom.walkable.length === 0) {
            this.addWalkablePolygon(x, y);
            return;
        }
        const poly = this.currentRoom.walkable[0];
        if (poly) {
            if (poly.points.length < 3) {
                this.addWalkablePolygon(x, y);
                return;
            }
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
    addWalkablePolygon(x, y, width = 80, height = 60) {
        if (!this.currentRoom.walkable) {
            this.currentRoom.walkable = [];
        }
        const id = `floor-${this.currentRoom.walkable.length + 1}`;
        const poly = EditorGeometry.createDefaultBoxPolygon(id, x, y, width, height, this.currentRoom.size.width, this.currentRoom.size.height);
        this.currentRoom.walkable.push(poly);
        this.dirty = true;
        const pIdx = this.currentRoom.walkable.length - 1;
        this.selected = { type: 'polygon', polyType: 'walkable', polyIndex: pIdx };
        return pIdx;
    }
    addObstaclePolygon(x, y, width = 60, height = 40) {
        if (!this.currentRoom.obstacles) {
            this.currentRoom.obstacles = [];
        }
        const id = `obs-${this.currentRoom.obstacles.length + 1}`;
        const poly = EditorGeometry.createDefaultBoxPolygon(id, x, y, width, height, this.currentRoom.size.width, this.currentRoom.size.height);
        this.currentRoom.obstacles.push(poly);
        this.dirty = true;
        const oIdx = this.currentRoom.obstacles.length - 1;
        this.selected = { type: 'polygon', polyType: 'obstacle', polyIndex: oIdx };
        return oIdx;
    }
    addObstaclePoint(x, y) {
        if (!this.currentRoom.obstacles || this.currentRoom.obstacles.length === 0) {
            this.addObstaclePolygon(x, y);
            return;
        }
        const lastIdx = this.currentRoom.obstacles.length - 1;
        const poly = this.currentRoom.obstacles[lastIdx];
        if (poly) {
            if (poly.points.length < 3) {
                this.addObstaclePolygon(x, y);
                return;
            }
            poly.points.push({ x: Math.round(x), y: Math.round(y) });
            this.selected = {
                type: 'vertex',
                polyType: 'obstacle',
                polyIndex: lastIdx,
                vertexIndex: poly.points.length - 1
            };
            this.dirty = true;
        }
    }
    splitEdge(edge) {
        const list = edge.polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
        if (!list)
            return;
        const poly = list[edge.polyIndex];
        if (!poly)
            return;
        poly.points.splice(edge.edgeIndex + 1, 0, { ...edge.splitPoint });
        this.selected = {
            type: 'vertex',
            polyType: edge.polyType,
            polyIndex: edge.polyIndex,
            vertexIndex: edge.edgeIndex + 1
        };
        this.dirty = true;
    }
    addSpawn(pos, id) {
        const nextNum = this.currentRoom.spawns.length + 1;
        const spawnId = id ?? (nextNum === 1 ? 'entry' : `spawn-${nextNum}`);
        this.currentRoom.spawns.push({
            id: spawnId,
            position: { x: Math.round(pos.x), y: Math.round(pos.y) }
        });
        this.selected = { type: 'spawn', spawnId };
        this.dirty = true;
    }
    addOrUpdateSpawn(id, x, y) {
        const existing = this.currentRoom.spawns.find(s => s.id === id);
        if (existing) {
            existing.position = { x: Math.round(x), y: Math.round(y) };
        }
        else {
            this.currentRoom.spawns.push({ id, position: { x: Math.round(x), y: Math.round(y) } });
        }
        this.selected = { type: 'spawn', spawnId: id };
        this.dirty = true;
    }
    addExit(exitOrPos) {
        if (!this.currentRoom.exits) {
            this.currentRoom.exits = [];
        }
        if ('target' in exitOrPos) {
            this.currentRoom.exits.push({ ...exitOrPos });
            this.selected = { type: 'exit', exitId: exitOrPos.id };
        }
        else {
            const nextNum = this.currentRoom.exits.length + 1;
            const exitId = `exit-${nextNum}`;
            const newExit = {
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
        this.dirty = true;
    }
    moveVertex(polyType, polyIdx, vIdx, pos) {
        const list = polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
        if (!list)
            return;
        const poly = list[polyIdx];
        if (poly && poly.points[vIdx]) {
            const clampedX = Math.max(0, Math.min(this.currentRoom.size.width, Math.round(pos.x)));
            const clampedY = Math.max(0, Math.min(this.currentRoom.size.height, Math.round(pos.y)));
            poly.points[vIdx] = { x: clampedX, y: clampedY };
            this.dirty = true;
        }
    }
    movePolygon(polyType, polyIdx, dx, dy) {
        const list = polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
        if (!list)
            return;
        const poly = list[polyIdx];
        if (poly && poly.points.length > 0) {
            poly.points = EditorGeometry.translatePoints(poly.points, dx, dy, this.currentRoom.size.width, this.currentRoom.size.height);
            this.dirty = true;
        }
    }
    moveSpawn(id, pos) {
        const spawn = this.currentRoom.spawns.find(s => s.id === id);
        if (spawn) {
            spawn.position = {
                x: Math.max(0, Math.min(this.currentRoom.size.width, Math.round(pos.x))),
                y: Math.max(0, Math.min(this.currentRoom.size.height, Math.round(pos.y)))
            };
            this.dirty = true;
        }
    }
    moveExit(id, pos) {
        if (!this.currentRoom.exits)
            return;
        const exit = this.currentRoom.exits.find(e => e.id === id);
        if (exit) {
            exit.position = {
                x: Math.max(0, Math.min(this.currentRoom.size.width, Math.round(pos.x))),
                y: Math.max(0, Math.min(this.currentRoom.size.height, Math.round(pos.y)))
            };
            this.dirty = true;
        }
    }
    deleteSelected() {
        const sel = this.selected;
        if (!sel || !sel.type)
            return false;
        if (sel.type === 'vertex' && sel.polyType && sel.polyIndex !== undefined && sel.vertexIndex !== undefined) {
            const list = sel.polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
            if (list && list[sel.polyIndex]) {
                const poly = list[sel.polyIndex];
                if (poly && poly.points.length > 3) {
                    poly.points.splice(sel.vertexIndex, 1);
                    this.selected = { type: null };
                    this.dirty = true;
                    return true;
                }
                else if (poly && poly.points.length <= 3 && list.length > 1) {
                    list.splice(sel.polyIndex, 1);
                    this.selected = { type: null };
                    this.dirty = true;
                    return true;
                }
            }
        }
        else if (sel.type === 'polygon' && sel.polyType && sel.polyIndex !== undefined) {
            const list = sel.polyType === 'walkable' ? this.currentRoom.walkable : this.currentRoom.obstacles;
            if (list && list[sel.polyIndex]) {
                if (sel.polyType === 'walkable' && list.length <= 1) {
                    return false;
                }
                list.splice(sel.polyIndex, 1);
                this.selected = { type: null };
                this.dirty = true;
                return true;
            }
        }
        else if (sel.type === 'spawn' && sel.spawnId) {
            const idx = this.currentRoom.spawns.findIndex(s => s.id === sel.spawnId);
            if (idx >= 0 && this.currentRoom.spawns.length > 1) {
                this.currentRoom.spawns.splice(idx, 1);
                this.selected = { type: null };
                this.dirty = true;
                return true;
            }
        }
        else if (sel.type === 'exit' && sel.exitId && this.currentRoom.exits) {
            const idx = this.currentRoom.exits.findIndex(e => e.id === sel.exitId);
            if (idx >= 0) {
                this.currentRoom.exits.splice(idx, 1);
                this.selected = { type: null };
                this.dirty = true;
                return true;
            }
        }
        return false;
    }
    toRoomDefinition() {
        return EditorGeometry.roomYamlToRoomDefinition(this.currentRoom);
    }
}
//# sourceMappingURL=editor-state.js.map