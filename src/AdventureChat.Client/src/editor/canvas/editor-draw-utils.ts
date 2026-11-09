import type {
  HoveredEdge,
  RoomExitDto,
  RoomPointDto,
  RoomSpawnDto,
  RoomView,
  SelectedEntity
} from '../core/editor-types.js';

export class EditorDrawUtils {
  public static drawPolygon(
    ctx: CanvasRenderingContext2D,
    points: readonly RoomPointDto[],
    fillColor: string,
    strokeColor: string,
    polyType: 'walkable' | 'obstacle',
    polyIndex: number,
    selection: Readonly<SelectedEntity>,
    hoveredVertex: { polyType: 'walkable' | 'obstacle'; polyIndex: number; vertexIndex: number } | null,
    dragPos: RoomPointDto | null,
    dragOffset: { dx: number; dy: number } | null
  ): void {
    if (points.length === 0) return;

    const isPolySelected =
      selection.type === 'polygon' &&
      selection.polyType === polyType &&
      selection.polyIndex === polyIndex;

    const effPoints = points.map((p, vIdx) => {
      if (
        isPolySelected &&
        dragOffset &&
        selection.type === 'polygon'
      ) {
        return {
          x: p.x + dragOffset.dx,
          y: p.y + dragOffset.dy
        };
      }
      if (
        dragPos &&
        selection.type === 'vertex' &&
        selection.polyType === polyType &&
        selection.polyIndex === polyIndex &&
        selection.vertexIndex === vIdx
      ) {
        return dragPos;
      }
      return p;
    });

    // 1. Draw polygon fill and boundary
    ctx.save();
    ctx.beginPath();
    ctx.moveTo(effPoints[0]!.x, effPoints[0]!.y);
    for (let i = 1; i < effPoints.length; i++) {
      ctx.lineTo(effPoints[i]!.x, effPoints[i]!.y);
    }
    ctx.closePath();

    ctx.fillStyle = fillColor;
    ctx.fill();

    ctx.strokeStyle = isPolySelected ? '#fbbf24' : strokeColor;
    ctx.lineWidth = isPolySelected ? 3 : 2;
    if (isPolySelected) {
      ctx.setLineDash([6, 3]);
    }
    ctx.stroke();
    ctx.restore();

    // 2. Draw vertices
    for (let vIdx = 0; vIdx < effPoints.length; vIdx++) {
      const pt = effPoints[vIdx]!;
      const isSelected =
        selection.type === 'vertex' &&
        selection.polyType === polyType &&
        selection.polyIndex === polyIndex &&
        selection.vertexIndex === vIdx;

      const isHovered =
        hoveredVertex &&
        hoveredVertex.polyType === polyType &&
        hoveredVertex.polyIndex === polyIndex &&
        hoveredVertex.vertexIndex === vIdx;

      ctx.save();
      ctx.beginPath();
      const radius = isSelected ? 6 : isHovered ? 5 : 4;
      ctx.arc(pt.x, pt.y, radius, 0, Math.PI * 2);

      if (isSelected) {
        ctx.fillStyle = '#fbbf24'; // Yellow
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 2;
      } else if (isHovered) {
        ctx.fillStyle = '#ffffff';
        ctx.strokeStyle = strokeColor;
        ctx.lineWidth = 2;
      } else {
        ctx.fillStyle = strokeColor;
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 1;
      }

      ctx.fill();
      ctx.stroke();
      ctx.restore();
    }
  }

  public static drawEdgeHighlight(
    ctx: CanvasRenderingContext2D,
    room: RoomView,
    edge: HoveredEdge
  ): void {
    const list = edge.polyType === 'walkable' ? room.walkable : room.obstacles;
    if (!list) return;
    const poly = list[edge.polyIndex];
    if (!poly || poly.points.length < 2) return;

    const p1 = poly.points[edge.edgeIndex];
    const p2 = poly.points[(edge.edgeIndex + 1) % poly.points.length];
    if (!p1 || !p2) return;

    // Glowing highlight line on edge
    ctx.save();
    ctx.beginPath();
    ctx.moveTo(p1.x, p1.y);
    ctx.lineTo(p2.x, p2.y);
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 4;
    ctx.stroke();

    // Split indicator at projection point
    ctx.beginPath();
    ctx.arc(edge.splitPoint.x, edge.splitPoint.y, 5, 0, Math.PI * 2);
    ctx.fillStyle = '#fbbf24';
    ctx.fill();
    ctx.strokeStyle = '#1a202c';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Plus (+) icon inside handle
    ctx.fillStyle = '#1a202c';
    ctx.font = 'bold 11px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('+', edge.splitPoint.x, edge.splitPoint.y + 1);

    ctx.restore();
  }

  public static drawSpawn(
    ctx: CanvasRenderingContext2D,
    spawn: RoomSpawnDto,
    selection: Readonly<SelectedEntity>,
    dragPos: RoomPointDto | null,
    isHovered: boolean
  ): void {
    const isSelected = selection.type === 'spawn' && selection.spawnId === spawn.id;
    const pos = isSelected && dragPos ? dragPos : spawn.position;

    ctx.save();

    // Outer glow / selection ring
    if (isSelected || isHovered) {
      ctx.beginPath();
      ctx.arc(pos.x, pos.y, 14, 0, Math.PI * 2);
      ctx.fillStyle = isSelected ? 'rgba(251, 191, 36, 0.4)' : 'rgba(255, 255, 255, 0.2)';
      ctx.fill();
      ctx.strokeStyle = isSelected ? '#fbbf24' : '#ffffff';
      ctx.lineWidth = 2;
      ctx.stroke();
    }

    // Pin Head
    ctx.beginPath();
    ctx.arc(pos.x, pos.y - 10, 8, 0, Math.PI * 2);
    ctx.fillStyle = '#3182ce'; // Blue
    ctx.fill();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Pin Needle
    ctx.beginPath();
    ctx.moveTo(pos.x - 4, pos.y - 4);
    ctx.lineTo(pos.x, pos.y);
    ctx.lineTo(pos.x + 4, pos.y - 4);
    ctx.fillStyle = '#3182ce';
    ctx.fill();

    // Label
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 11px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(`📍 ${spawn.id}`, pos.x, pos.y - 20);

    ctx.restore();
  }

  public static drawExit(
    ctx: CanvasRenderingContext2D,
    exit: RoomExitDto,
    selection: Readonly<SelectedEntity>,
    dragPos: RoomPointDto | null,
    isHovered: boolean
  ): void {
    const isSelected = selection.type === 'exit' && selection.exitId === exit.id;
    const pos = isSelected && dragPos ? dragPos : exit.position;
    const radius = exit.radius || 35;

    ctx.save();

    // Trigger Radius Circle
    ctx.beginPath();
    ctx.arc(pos.x, pos.y, radius, 0, Math.PI * 2);
    ctx.fillStyle = isSelected ? 'rgba(237, 137, 54, 0.3)' : isHovered ? 'rgba(237, 137, 54, 0.2)' : 'rgba(237, 137, 54, 0.1)';
    ctx.fill();
    ctx.strokeStyle = isSelected ? '#fbbf24' : '#ed8936';
    ctx.lineWidth = isSelected ? 3 : 2;
    ctx.setLineDash([4, 4]);
    ctx.stroke();

    // Center icon
    ctx.beginPath();
    ctx.arc(pos.x, pos.y, 10, 0, Math.PI * 2);
    ctx.fillStyle = '#ed8936';
    ctx.fill();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Label & Target
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 11px sans-serif';
    ctx.textAlign = 'center';
    const label = exit.label || exit.id;
    ctx.fillText(`🚪 ${label} ➔ ${exit.target.room}`, pos.x, pos.y - radius - 5);

    ctx.restore();
  }

  public static drawCoordinateTooltip(
    ctx: CanvasRenderingContext2D,
    mousePos: RoomPointDto,
    canvasWidth: number,
    canvasHeight: number
  ): void {
    const isMouseInTopHalf = mousePos.y < canvasHeight / 2;
    const isMouseInLeftHalf = mousePos.x < canvasWidth / 2;

    const pad = 8;
    const boxW = 82;
    const boxH = 22;

    const boxX = isMouseInLeftHalf ? canvasWidth - boxW - pad : pad;
    const boxY = isMouseInTopHalf ? canvasHeight - boxH - pad : pad;

    ctx.save();
    ctx.fillStyle = 'rgba(15, 23, 42, 0.85)';
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.2)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.roundRect(boxX, boxY, boxW, boxH, 4);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = '#38bdf8';
    ctx.font = 'bold 11px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(`${mousePos.x}, ${mousePos.y}`, boxX + boxW / 2, boxY + boxH / 2);
    ctx.restore();
  }
}
