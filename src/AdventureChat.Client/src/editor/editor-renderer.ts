import type { RoomYamlDto, SelectedEntity, RoomPointDto, HoveredEdge } from './editor-types.js';

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
    hoveredEntity: { type: 'spawn' | 'exit'; id: string } | null = null
  ): void {
    const { ctx, canvas } = this;
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // 1. Background image or dark fallback
    if (this.backgroundLoaded && this.backgroundImage.width > 0) {
      ctx.drawImage(this.backgroundImage, 0, 0, room.size.width, room.size.height);
    } else {
      ctx.fillStyle = '#1a202c';
      ctx.fillRect(0, 0, room.size.width, room.size.height);
    }

    // 2. Subtle coordinate grid (every 40px)
    ctx.save();
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.05)';
    ctx.lineWidth = 1;
    for (let x = 0; x < room.size.width; x += 40) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, room.size.height);
      ctx.stroke();
    }
    for (let y = 0; y < room.size.height; y += 40) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(room.size.width, y);
      ctx.stroke();
    }
    ctx.restore();

    // 3. Walkable Areas
    for (let pIdx = 0; pIdx < room.walkable.length; pIdx++) {
      const poly = room.walkable[pIdx];
      if (!poly || poly.points.length === 0) continue;

      this.drawPolygon(
        poly.points,
        'rgba(72, 187, 120, 0.25)',
        '#48bb78',
        'walkable',
        pIdx,
        selection,
        hoveredVertex,
        dragPos
      );
    }

    // 4. Obstacles
    if (room.obstacles) {
      for (let oIdx = 0; oIdx < room.obstacles.length; oIdx++) {
        const poly = room.obstacles[oIdx];
        if (!poly || poly.points.length === 0) continue;

        this.drawPolygon(
          poly.points,
          'rgba(245, 101, 101, 0.3)',
          '#f56565',
          'obstacle',
          oIdx,
          selection,
          hoveredVertex,
          dragPos
        );
      }
    }

    // 5. Highlight Hovered Edge & Split Indicator
    if (hoveredEdge) {
      this.drawEdgeHighlight(room, hoveredEdge);
    }

    // 6. Exits
    if (room.exits) {
      for (const exit of room.exits) {
        this.drawExit(exit, selection, hoveredEntity?.type === 'exit' && hoveredEntity.id === exit.id);
      }
    }

    // 7. Spawns
    for (const spawn of room.spawns) {
      this.drawSpawn(spawn, selection, hoveredEntity?.type === 'spawn' && hoveredEntity.id === spawn.id);
    }

    // 8. Dynamic Mouse cursor readout (jumps away from mouse to never obstruct artwork or cursor)
    if (mousePos) {
      ctx.save();
      const boxWidth = 120;
      const boxHeight = 22;
      const margin = 10;

      // Determine corner furthest away from mouse
      const isMouseBottom = mousePos.y > room.size.height / 2;
      const isMouseLeft = mousePos.x < room.size.width / 2;

      // Position in corner opposite to mouse
      const posX = isMouseLeft ? room.size.width - boxWidth - margin : margin;
      const posY = isMouseBottom ? margin : room.size.height - boxHeight - margin;

      ctx.fillStyle = 'rgba(26, 32, 44, 0.85)';
      ctx.strokeStyle = '#4a5568';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.roundRect(posX, posY, boxWidth, boxHeight, 4);
      ctx.fill();
      ctx.stroke();

      ctx.fillStyle = '#ecc94b';
      ctx.font = 'bold 11px monospace';
      ctx.fillText(`X: ${Math.round(mousePos.x)}, Y: ${Math.round(mousePos.y)}`, posX + 8, posY + 15);
      ctx.restore();
    }
  }

  private drawEdgeHighlight(room: RoomYamlDto, edge: HoveredEdge): void {
    const polys = edge.polyType === 'walkable' ? room.walkable : (room.obstacles ?? []);
    const poly = polys[edge.polyIndex];
    if (!poly || poly.points.length < 2) return;

    const p1 = poly.points[edge.edgeIndex];
    const p2 = poly.points[(edge.edgeIndex + 1) % poly.points.length];
    if (!p1 || !p2) return;

    const { ctx } = this;
    ctx.save();

    // Glowing edge line
    ctx.beginPath();
    ctx.moveTo(p1.x, p1.y);
    ctx.lineTo(p2.x, p2.y);
    ctx.strokeStyle = '#ecc94b';
    ctx.lineWidth = 4;
    ctx.setLineDash([6, 3]);
    ctx.stroke();
    ctx.setLineDash([]);

    // Split handle button at splitPoint
    const { x, y } = edge.splitPoint;
    ctx.beginPath();
    ctx.arc(x, y, 7, 0, Math.PI * 2);
    ctx.fillStyle = '#ecc94b';
    ctx.fill();
    ctx.strokeStyle = '#1a202c';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Plus (+) symbol in handle
    ctx.beginPath();
    ctx.moveTo(x - 3.5, y);
    ctx.lineTo(x + 3.5, y);
    ctx.moveTo(x, y - 3.5);
    ctx.lineTo(x, y + 3.5);
    ctx.strokeStyle = '#1a202c';
    ctx.lineWidth = 1.8;
    ctx.stroke();

    // Split hint tooltip
    ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
    ctx.fillRect(x + 10, y - 22, 130, 18);
    ctx.fillStyle = '#ecc94b';
    ctx.font = 'bold 10px sans-serif';
    ctx.fillText('Kante teilen (+ Punkt)', x + 14, y - 10);

    ctx.restore();
  }

  private drawPolygon(
    points: RoomPointDto[],
    fillColor: string,
    strokeColor: string,
    polyType: 'walkable' | 'obstacle',
    polyIndex: number,
    selection: SelectedEntity,
    hoveredVertex: { polyType: 'walkable' | 'obstacle'; polyIndex: number; vertexIndex: number } | null,
    dragPos: RoomPointDto | null
  ): void {
    const { ctx } = this;
    if (points.length === 0) return;

    ctx.save();
    ctx.beginPath();
    for (let i = 0; i < points.length; i++) {
      let pt = points[i]!;
      if (
        dragPos &&
        selection.type === 'vertex' &&
        selection.polyType === polyType &&
        selection.polyIndex === polyIndex &&
        selection.vertexIndex === i
      ) {
        pt = dragPos;
      }

      if (i === 0) {
        ctx.moveTo(pt.x, pt.y);
      } else {
        ctx.lineTo(pt.x, pt.y);
      }
    }
    ctx.closePath();
    ctx.fillStyle = fillColor;
    ctx.fill();
    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = 2;
    ctx.stroke();

    // Draw vertex handles
    for (let i = 0; i < points.length; i++) {
      let pt = points[i]!;
      const isSelected =
        selection.type === 'vertex' &&
        selection.polyType === polyType &&
        selection.polyIndex === polyIndex &&
        selection.vertexIndex === i;

      if (isSelected && dragPos) {
        pt = dragPos;
      }

      const isHovered =
        hoveredVertex &&
        hoveredVertex.polyType === polyType &&
        hoveredVertex.polyIndex === polyIndex &&
        hoveredVertex.vertexIndex === i;

      ctx.beginPath();
      const radius = isSelected ? 6 : isHovered ? 5 : 4;
      ctx.arc(pt.x, pt.y, radius, 0, Math.PI * 2);
      ctx.fillStyle = isSelected ? '#ecc94b' : isHovered ? '#63b3ed' : '#ffffff';
      ctx.fill();
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 1.5;
      ctx.stroke();

      // Vertex index number
      ctx.fillStyle = '#ffffff';
      ctx.font = '10px sans-serif';
      ctx.fillText(`${i + 1}`, pt.x + 6, pt.y - 6);
    }

    ctx.restore();
  }

  private drawSpawn(
    spawn: { id: string; position: RoomPointDto },
    selection: SelectedEntity,
    isHovered = false
  ): void {
    const { ctx } = this;
    const isSelected = selection.type === 'spawn' && selection.spawnId === spawn.id;

    ctx.save();
    const { x, y } = spawn.position;

    // Outer glow / circle
    ctx.beginPath();
    ctx.arc(x, y, isSelected ? 12 : isHovered ? 11 : 9, 0, Math.PI * 2);
    ctx.fillStyle = isSelected ? '#ecc94b' : isHovered ? '#63b3ed' : '#3182ce';
    ctx.fill();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Crosshair
    ctx.beginPath();
    ctx.moveTo(x - 5, y);
    ctx.lineTo(x + 5, y);
    ctx.moveTo(x, y - 5);
    ctx.lineTo(x, y + 5);
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Label
    ctx.fillStyle = isSelected ? '#ecc94b' : '#ffffff';
    ctx.font = 'bold 11px sans-serif';
    ctx.fillText(`Spawn: ${spawn.id}`, x + 12, y + 4);
    ctx.restore();
  }

  private drawExit(
    exit: { id: string; position: RoomPointDto; radius: number; bounds?: { x: number; y: number; width: number; height: number } | null; label?: string | null; target: { room: string; spawn: string } },
    selection: SelectedEntity,
    isHovered = false
  ): void {
    const { ctx } = this;
    const isSelected = selection.type === 'exit' && selection.exitId === exit.id;

    ctx.save();
    const { x, y } = exit.position;

    // Radius circle
    ctx.beginPath();
    ctx.arc(x, y, exit.radius || 30, 0, Math.PI * 2);
    ctx.fillStyle = isSelected ? 'rgba(236, 201, 75, 0.25)' : isHovered ? 'rgba(99, 179, 237, 0.25)' : 'rgba(214, 158, 46, 0.2)';
    ctx.fill();
    ctx.strokeStyle = isSelected ? '#ecc94b' : isHovered ? '#63b3ed' : '#d69e2e';
    ctx.lineWidth = isSelected ? 3 : 2;
    ctx.setLineDash([4, 4]);
    ctx.stroke();
    ctx.setLineDash([]);

    // Bounds rectangle if defined
    if (exit.bounds) {
      ctx.strokeStyle = isSelected ? '#ecc94b' : '#d69e2e';
      ctx.lineWidth = 1;
      ctx.strokeRect(exit.bounds.x, exit.bounds.y, exit.bounds.width, exit.bounds.height);
    }

    // Center marker
    ctx.beginPath();
    ctx.arc(x, y, isSelected ? 8 : isHovered ? 7 : 6, 0, Math.PI * 2);
    ctx.fillStyle = isSelected ? '#ecc94b' : '#d69e2e';
    ctx.fill();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Label
    ctx.fillStyle = isSelected ? '#ecc94b' : '#f6e05e';
    ctx.font = 'bold 11px sans-serif';
    const text = exit.label ? `Exit: ${exit.label}` : `Exit -> ${exit.target.room}:${exit.target.spawn}`;
    ctx.fillText(text, x + 10, y - 6);
    ctx.restore();
  }
}
