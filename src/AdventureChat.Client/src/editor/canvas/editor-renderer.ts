import { resizeCanvas } from '../../rendering/camera.js';
import { EditorDrawUtils } from './editor-draw-utils.js';
import type {
  HoveredEdge,
  RoomPointDto,
  RoomYamlDto,
  SelectedEntity
} from '../core/editor-types.js';

export class EditorRenderer {
  private readonly ctx: CanvasRenderingContext2D;
  private readonly backgroundImage: HTMLImageElement;
  private backgroundLoaded = false;
  private currentBackgroundUrl = '';

  public constructor(private readonly canvas: HTMLCanvasElement) {
    const context = this.canvas.getContext('2d');
    if (!context) {
      throw new Error('Canvas 2D context not available');
    }
    this.ctx = context;
    this.ctx.imageSmoothingEnabled = false;

    this.backgroundImage = new Image();
    this.backgroundImage.onload = () => {
      this.backgroundLoaded = true;
    };
  }

  public setBackground(url: string): void {
    if (this.currentBackgroundUrl !== url) {
      this.currentBackgroundUrl = url;
      this.backgroundLoaded = false;
      this.backgroundImage.src = url;
    }
  }

  public render(
    room: RoomYamlDto,
    selection: SelectedEntity,
    mousePos: RoomPointDto | null,
    dragPos: RoomPointDto | null,
    hoveredVertex: { polyType: 'walkable' | 'obstacle'; polyIndex: number; vertexIndex: number } | null,
    hoveredEdge: HoveredEdge | null = null,
    hoveredEntity: { type: 'spawn' | 'exit'; id: string } | null = null,
    dragOffset: { dx: number; dy: number } | null = null,
    cameraOffset = 0
  ): void {
    const { ctx, canvas } = this;
    resizeCanvas(canvas, { ...room.size, viewport: room.viewport ?? null });
    ctx.imageSmoothingEnabled = false;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.save();
    ctx.translate(-cameraOffset, 0);

    // 1. Background image or dark fallback
    if (this.backgroundLoaded && this.backgroundImage.width > 0) {
      ctx.drawImage(this.backgroundImage, 0, 0, room.size.width, room.size.height);
    } else {
      ctx.fillStyle = '#1a202c';
      ctx.fillRect(0, 0, room.size.width, room.size.height);
    }

    // 2. Subtle coordinate grid (every 40px)
    this.drawGrid(room.size.width, room.size.height);

    // 3. Walkable Areas
    for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
      const poly = room.walkable[pIdx];
      if (!poly || poly.points.length === 0) continue;

      EditorDrawUtils.drawPolygon(
        ctx,
        poly.points,
        'rgba(72, 187, 120, 0.25)',
        '#48bb78',
        'walkable',
        pIdx,
        selection,
        hoveredVertex,
        dragPos,
        dragOffset
      );
    }

    // 4. Obstacles
    if (room.obstacles) {
      for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
        const poly = room.obstacles[oIdx];
        if (!poly || poly.points.length === 0) continue;

        EditorDrawUtils.drawPolygon(
          ctx,
          poly.points,
          'rgba(245, 101, 101, 0.3)',
          '#f56565',
          'obstacle',
          oIdx,
          selection,
          hoveredVertex,
          dragPos,
          dragOffset
        );
      }
    }

    // 5. Highlight Hovered Edge & Split Indicator
    if (hoveredEdge) {
      EditorDrawUtils.drawEdgeHighlight(ctx, room, hoveredEdge);
    }

    // 6. Exits
    if (room.exits) {
      for (const exit of room.exits) {
        const isHovered = hoveredEntity?.type === 'exit' && hoveredEntity.id === exit.id;
        EditorDrawUtils.drawExit(ctx, exit, selection, dragPos, isHovered);
      }
    }

    // 7. Spawns
    for (const spawn of room.spawns) {
      const isHovered = hoveredEntity?.type === 'spawn' && hoveredEntity.id === spawn.id;
      EditorDrawUtils.drawSpawn(ctx, spawn, selection, dragPos, isHovered);
    }

    ctx.restore();

    // 8. Hover Coordinate Badge
    if (mousePos) {
      ctx.save();
      ctx.translate(-cameraOffset, 0);
      EditorDrawUtils.drawCoordinateTooltip(ctx, mousePos, canvas.width + cameraOffset, canvas.height);
      ctx.restore();
    }
  }

  private drawGrid(width: number, height: number): void {
    const { ctx } = this;
    ctx.save();
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.05)';
    ctx.lineWidth = 1;
    for (let x = 0; x < width; x += 40) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, height);
      ctx.stroke();
    }
    for (let y = 0; y < height; y += 40) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(width, y);
      ctx.stroke();
    }
    ctx.restore();
  }
}
