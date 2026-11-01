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
        const halfW = Math.round(width / 2);
        const halfH = Math.round(height / 2);
        const maxX = this.currentRoom.size.width;
        const maxY = this.currentRoom.size.height;
        const clamp = (val, max) => Math.max(0, Math.min(max, Math.round(val)));
        const points = [
            { x: clamp(x - halfW, maxX), y: clamp(y - halfH, maxY) },
            { x: clamp(x + halfW, maxX), y: clamp(y - halfH, maxY) },
            { x: clamp(x + halfW, maxX), y: clamp(y + halfH, maxY) },
            { x: clamp(x - halfW, maxX), y: clamp(y + halfH, maxY) }
        ];
        if (this.currentRoom.walkable.length === 0 || this.currentRoom.walkable[0]?.points.length === 0) {
            this.currentRoom.walkable = [{ id: 'floor', points }];
            this.selected = { type: 'polygon', polyType: 'walkable', polyIndex: 0 };
            this.dirty = true;
            return 0;
        }
        const index = this.currentRoom.walkable.length;
        this.currentRoom.walkable.push({
            id: `floor-${index + 1}`,
            points
        });
        this.selected = { type: 'polygon', polyType: 'walkable', polyIndex: index };
        this.dirty = true;
        return index;
    }
    addObstacle(x, y, width = 60, height = 40) {
        if (!this.currentRoom.obstacles) {
            this.currentRoom.obstacles = [];
        }
        const halfW = Math.round(width / 2);
        const halfH = Math.round(height / 2);
        const maxX = this.currentRoom.size.width;
        const maxY = this.currentRoom.size.height;
        const clamp = (val, max) => Math.max(0, Math.min(max, Math.round(val)));
        const points = [
            { x: clamp(x - halfW, maxX), y: clamp(y - halfH, maxY) },
            { x: clamp(x + halfW, maxX), y: clamp(y - halfH, maxY) },
            { x: clamp(x + halfW, maxX), y: clamp(y + halfH, maxY) },
            { x: clamp(x - halfW, maxX), y: clamp(y + halfH, maxY) }
        ];
        const index = this.currentRoom.obstacles.length;
        this.currentRoom.obstacles.push({
            id: `obstacle-${index + 1}`,
            points
        });
        this.selected = {
            type: 'polygon',
            polyType: 'obstacle',
            polyIndex: index
        };
        this.dirty = true;
        return index;
    }
    addObstaclePoint(x, y, obstacleIndex = 0) {
        if (!this.currentRoom.obstacles || this.currentRoom.obstacles.length === 0) {
            this.addObstacle(x, y);
            return;
        }
        if (this.currentRoom.obstacles.length <= obstacleIndex) {
            this.addObstacle(x, y);
            return;
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
    translatePolygon(polyType, polyIndex, dx, dy) {
        const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
        const poly = polys[polyIndex];
        if (!poly || poly.points.length === 0)
            return false;
        const roundDx = Math.round(dx);
        const roundDy = Math.round(dy);
        if (roundDx === 0 && roundDy === 0)
            return false;
        const maxX = this.currentRoom.size.width;
        const maxY = this.currentRoom.size.height;
        // Check boundary limits so shape doesn't collapse on edges
        let minX = Infinity;
        let maxXPt = -Infinity;
        let minY = Infinity;
        let maxYPt = -Infinity;
        for (const p of poly.points) {
            minX = Math.min(minX, p.x);
            maxXPt = Math.max(maxXPt, p.x);
            minY = Math.min(minY, p.y);
            maxYPt = Math.max(maxYPt, p.y);
        }
        const actualDx = Math.max(-minX, Math.min(maxX - maxXPt, roundDx));
        const actualDy = Math.max(-minY, Math.min(maxY - maxYPt, roundDy));
        if (actualDx === 0 && actualDy === 0)
            return false;
        for (const p of poly.points) {
            p.x += actualDx;
            p.y += actualDy;
        }
        this.dirty = true;
        return true;
    }
    deletePolygon(polyType, polyIndex) {
        const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
        if (!polys || polyIndex < 0 || polyIndex >= polys.length)
            return false;
        polys.splice(polyIndex, 1);
        this.selected = { type: null };
        this.dirty = true;
        return true;
    }
    moveVertex(polyType, polyIndex, vertexIndex, x, y) {
        const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
        const poly = polys[polyIndex];
        if (poly && poly.points[vertexIndex]) {
            poly.points[vertexIndex] = { x: Math.round(x), y: Math.round(y) };
            this.dirty = true;
        }
    }
    insertVertexOnEdge(polyType, polyIndex, edgeIndex, x, y) {
        const polys = polyType === 'walkable' ? this.currentRoom.walkable : (this.currentRoom.obstacles ?? []);
        const poly = polys[polyIndex];
        if (!poly)
            return -1;
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
    updateSelectedVertex(x, y) {
        if (this.selected.type !== 'vertex' || this.selected.vertexIndex === undefined)
            return false;
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
    updateSelectedSpawn(id, x, y) {
        if (this.selected.type !== 'spawn' || !this.selected.spawnId)
            return false;
        const spawn = this.currentRoom.spawns.find(s => s.id === this.selected.spawnId);
        if (!spawn)
            return false;
        spawn.id = id;
        spawn.position = { x: Math.round(x), y: Math.round(y) };
        this.selected.spawnId = id;
        this.dirty = true;
        return true;
    }
    updateSelectedExit(update) {
        if (this.selected.type !== 'exit' || !this.selected.exitId || !this.currentRoom.exits)
            return false;
        const exit = this.currentRoom.exits.find(e => e.id === this.selected.exitId);
        if (!exit)
            return false;
        if (update.id !== undefined) {
            exit.id = update.id;
            this.selected.exitId = update.id;
        }
        if (update.position !== undefined) {
            exit.position = { x: Math.round(update.position.x), y: Math.round(update.position.y) };
        }
        if (update.radius !== undefined)
            exit.radius = update.radius;
        if (update.label !== undefined)
            exit.label = update.label;
        if (update.target !== undefined)
            exit.target = update.target;
        if (update.bounds !== undefined)
            exit.bounds = update.bounds;
        this.dirty = true;
        return true;
    }
    deleteSelectedEntity() {
        if (this.selected.type === 'polygon' && this.selected.polyType && this.selected.polyIndex !== undefined) {
            return this.deletePolygon(this.selected.polyType, this.selected.polyIndex);
        }
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
    deleteSelectedVertex() {
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
        // Polygons must not have fewer than 3 vertices
        if (poly.points.length <= 3) {
            if (polyType === 'obstacle') {
                // If obstacle has only 3 vertices, removing a vertex deletes the obstacle cleanly
                return this.deletePolygon('obstacle', polyIndex);
            }
            else if (polyType === 'walkable' && this.currentRoom.walkable.length > 1) {
                return this.deletePolygon('walkable', polyIndex);
            }
            return false; // Minimum 3 vertices required for primary floor polygon
        }
        poly.points.splice(vertexIndex, 1);
        this.selected = { type: null };
        this.dirty = true;
        return true;
    }
    addOrUpdateSpawn(id, x, y) {
        const existing = this.currentRoom.spawns.find(s => s.id.toLowerCase() === id.toLowerCase());
        if (existing) {
            existing.position = { x: Math.round(x), y: Math.round(y) };
        }
        else {
            this.currentRoom.spawns.push({
                id,
                position: { x: Math.round(x), y: Math.round(y) }
            });
        }
        this.selected = { type: 'spawn', spawnId: id };
        this.dirty = true;
    }
    moveSpawn(id, x, y) {
        const spawn = this.currentRoom.spawns.find(s => s.id === id);
        if (spawn) {
            spawn.position = { x: Math.round(x), y: Math.round(y) };
            this.dirty = true;
        }
    }
    deleteSpawn(id) {
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
    addOrUpdateExit(exit) {
        if (!this.currentRoom.exits) {
            this.currentRoom.exits = [];
        }
        const idx = this.currentRoom.exits.findIndex(e => e.id.toLowerCase() === exit.id.toLowerCase());
        if (idx >= 0) {
            this.currentRoom.exits[idx] = exit;
        }
        else {
            this.currentRoom.exits.push(exit);
        }
        this.selected = { type: 'exit', exitId: exit.id };
        this.dirty = true;
    }
    moveExit(id, x, y) {
        if (!this.currentRoom.exits)
            return;
        const exit = this.currentRoom.exits.find(e => e.id === id);
        if (exit) {
            exit.position = { x: Math.round(x), y: Math.round(y) };
            this.dirty = true;
        }
    }
    deleteExit(id) {
        if (!this.currentRoom.exits)
            return false;
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
    toRuntimeRoomDefinition() {
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
                const exit = {
                    id: e.id,
                    position: e.position,
                    radius: e.radius,
                    targetRoomId: e.target.room,
                    targetSpawnId: e.target.spawn
                };
                if (e.bounds)
                    exit.bounds = e.bounds;
                if (e.label)
                    exit.label = e.label;
                return exit;
            })
        };
    }
}
//# sourceMappingURL=editor-state.js.map