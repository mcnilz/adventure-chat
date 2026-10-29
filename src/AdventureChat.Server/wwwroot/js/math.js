export function isInsideRect(point, rect) {
    return (point.x >= rect.x &&
        point.x <= rect.x + rect.width &&
        point.y >= rect.y &&
        point.y <= rect.y + rect.height);
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