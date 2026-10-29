export function isPointInPolygon(point, polygon) {
    if (!Number.isFinite(point.x) || !Number.isFinite(point.y) || polygon.length < 3) {
        return false;
    }
    let inside = false;
    const count = polygon.length;
    for (let i = 0, j = count - 1; i < count; j = i++) {
        const pi = polygon[i];
        const pj = polygon[j];
        if (!pi || !pj) {
            continue;
        }
        const intersects = pi.y > point.y !== pj.y > point.y &&
            point.x < ((pj.x - pi.x) * (point.y - pi.y)) / (pj.y - pi.y + Number.EPSILON) + pi.x;
        if (intersects) {
            inside = !inside;
        }
    }
    return inside;
}
export function isPointWalkable(point, outerBoundary, obstacles) {
    if (!Number.isFinite(point.x) || !Number.isFinite(point.y)) {
        return false;
    }
    if (!isPointInPolygon(point, outerBoundary)) {
        return false;
    }
    for (const obstacle of obstacles) {
        if (isPointInPolygon(point, obstacle)) {
            return false;
        }
    }
    return true;
}
export function distance(a, b) {
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    return Math.hypot(dx, dy);
}
export function moveTowards(current, target, maxDistance) {
    const dist = distance(current, target);
    if (dist <= maxDistance || dist === 0) {
        return { position: { x: target.x, y: target.y }, reached: true };
    }
    const ratio = maxDistance / dist;
    const nextX = current.x + (target.x - current.x) * ratio;
    const nextY = current.y + (target.y - current.y) * ratio;
    return {
        position: { x: nextX, y: nextY },
        reached: false,
    };
}
//# sourceMappingURL=math.js.map