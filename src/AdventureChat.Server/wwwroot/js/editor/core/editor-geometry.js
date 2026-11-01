import { closestPointOnSegment, distance, isPointInPolygon } from '../../math.js';
export class EditorGeometry {
    static createDefaultRoomYaml() {
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
            exits: []
        };
    }
    static roomYamlToRoomDefinition(r) {
        const outerBoundary = r.walkable[0]?.points.map(p => ({ x: p.x, y: p.y })) ?? [];
        const obstacles = (r.obstacles ?? []).map(o => o.points.map(p => ({ x: p.x, y: p.y })));
        const spawns = r.spawns.map(sp => ({
            id: sp.id,
            position: { x: sp.position.x, y: sp.position.y }
        }));
        const exits = (r.exits ?? []).map(ex => {
            const exit = {
                id: ex.id,
                position: { x: ex.position.x, y: ex.position.y },
                targetRoomId: ex.target.room,
                targetSpawnId: ex.target.spawn,
                radius: ex.radius
            };
            if (ex.bounds) {
                exit.bounds = ex.bounds;
            }
            if (ex.label) {
                exit.label = ex.label;
            }
            return exit;
        });
        const defaultSpawn = spawns[0]?.position ?? { x: 320, y: 240 };
        return {
            id: r.id,
            name: r.name,
            width: r.size.width,
            height: r.size.height,
            backgroundUrl: r.background,
            geometry: {
                outerBoundary,
                obstacles
            },
            spawnPoint: defaultSpawn,
            spawns,
            exits
        };
    }
    static findHoveredVertex(room, pos, hitRadius = 8) {
        for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
            const poly = room.walkable[pIdx];
            if (!poly)
                continue;
            for (let vIdx = 0; vIdx < poly.points.length; vIdx++) {
                const pt = poly.points[vIdx];
                if (pt && distance(pos, pt) <= hitRadius) {
                    return { polyType: 'walkable', polyIndex: pIdx, vertexIndex: vIdx };
                }
            }
        }
        if (room.obstacles) {
            for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
                const poly = room.obstacles[oIdx];
                if (!poly)
                    continue;
                for (let vIdx = 0; vIdx < poly.points.length; vIdx++) {
                    const pt = poly.points[vIdx];
                    if (pt && distance(pos, pt) <= hitRadius) {
                        return { polyType: 'obstacle', polyIndex: oIdx, vertexIndex: vIdx };
                    }
                }
            }
        }
        return null;
    }
    static findHoveredEdge(room, pos, hitThreshold = 7) {
        for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
            const poly = room.walkable[pIdx];
            if (!poly || poly.points.length < 2)
                continue;
            const pts = poly.points;
            for (let i = 0; i < pts.length; i++) {
                const p1 = pts[i];
                const p2 = pts[(i + 1) % pts.length];
                if (!p1 || !p2)
                    continue;
                const res = closestPointOnSegment(pos, p1, p2);
                const proj = res.point;
                const dist = res.distance;
                if (dist <= hitThreshold && distance(proj, p1) > 10 && distance(proj, p2) > 10) {
                    return {
                        polyType: 'walkable',
                        polyIndex: pIdx,
                        edgeIndex: i,
                        splitPoint: { x: Math.round(proj.x), y: Math.round(proj.y) }
                    };
                }
            }
        }
        if (room.obstacles) {
            for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
                const poly = room.obstacles[oIdx];
                if (!poly || poly.points.length < 2)
                    continue;
                const pts = poly.points;
                for (let i = 0; i < pts.length; i++) {
                    const p1 = pts[i];
                    const p2 = pts[(i + 1) % pts.length];
                    if (!p1 || !p2)
                        continue;
                    const res = closestPointOnSegment(pos, p1, p2);
                    const proj = res.point;
                    const dist = res.distance;
                    if (dist <= hitThreshold && distance(proj, p1) > 10 && distance(proj, p2) > 10) {
                        return {
                            polyType: 'obstacle',
                            polyIndex: oIdx,
                            edgeIndex: i,
                            splitPoint: { x: Math.round(proj.x), y: Math.round(proj.y) }
                        };
                    }
                }
            }
        }
        return null;
    }
    static findHoveredPolygon(room, pos) {
        if (room.obstacles) {
            for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
                const poly = room.obstacles[oIdx];
                if (poly && poly.points.length >= 3 && isPointInPolygon(pos, poly.points)) {
                    return { polyType: 'obstacle', polyIndex: oIdx };
                }
            }
        }
        for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
            const poly = room.walkable[pIdx];
            if (poly && poly.points.length >= 3 && isPointInPolygon(pos, poly.points)) {
                return { polyType: 'walkable', polyIndex: pIdx };
            }
        }
        return null;
    }
    static findHoveredSpawn(room, pos, hitRadius = 14) {
        for (const spawn of room.spawns) {
            if (distance(pos, spawn.position) <= hitRadius) {
                return spawn;
            }
        }
        return null;
    }
    static findHoveredExit(room, pos) {
        if (!room.exits)
            return null;
        for (const exit of room.exits) {
            const hitR = Math.max(12, exit.radius || 20);
            if (distance(pos, exit.position) <= hitR) {
                return exit;
            }
        }
        return null;
    }
    static createDefaultBoxPolygon(id, centerX, centerY, width, height, roomWidth, roomHeight) {
        const halfW = Math.round(width / 2);
        const halfH = Math.round(height / 2);
        const cx = Math.max(halfW, Math.min(roomWidth - halfW, Math.round(centerX)));
        const cy = Math.max(halfH, Math.min(roomHeight - halfH, Math.round(centerY)));
        return {
            id,
            points: [
                { x: cx - halfW, y: cy - halfH },
                { x: cx + halfW, y: cy - halfH },
                { x: cx + halfW, y: cy + halfH },
                { x: cx - halfW, y: cy + halfH }
            ]
        };
    }
    static translatePoints(points, dx, dy, maxWidth = 640, maxHeight = 360) {
        let minX = Infinity;
        let minY = Infinity;
        let maxX = -Infinity;
        let maxY = -Infinity;
        for (const pt of points) {
            if (pt.x < minX)
                minX = pt.x;
            if (pt.y < minY)
                minY = pt.y;
            if (pt.x > maxX)
                maxX = pt.x;
            if (pt.y > maxY)
                maxY = pt.y;
        }
        const clampedDx = Math.max(-minX, Math.min(maxWidth - maxX, dx));
        const clampedDy = Math.max(-minY, Math.min(maxHeight - maxY, dy));
        return points.map(pt => ({
            x: Math.round(pt.x + clampedDx),
            y: Math.round(pt.y + clampedDy)
        }));
    }
}
//# sourceMappingURL=editor-geometry.js.map