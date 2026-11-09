import { EditorGeometry } from '../core/editor-geometry.js';
import { screenToRoom } from '../../rendering/camera.js';
import type { EditorState } from '../core/editor-state.js';
import type {
  EditorTool,
  HoveredEdge,
  RoomPointDto
} from '../core/editor-types.js';

export interface CanvasInputCallbacks {
  onStateChange: () => void;
  onToolAutoReset: () => void;
  onHoverChange: () => void;
  onAddExitRequest: (pos: RoomPointDto) => void;
}

export class EditorCanvasInput {
  private mouseLogicalPos: RoomPointDto | null = null;
  private isDragging = false;
  private dragPos: RoomPointDto | null = null;
  private dragStartLogicalPos: RoomPointDto | null = null;
  private dragOffset: { dx: number; dy: number } | null = null;
  private hoveredEdge: HoveredEdge | null = null;
  private hoveredEntity: { type: 'spawn' | 'exit'; id: string } | null = null;

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly state: EditorState,
    private readonly callbacks: CanvasInputCallbacks,
    private readonly getTool: () => EditorTool,
    private readonly isPreviewActive: () => boolean,
    private readonly onPreviewClick: (logicalX: number, logicalY: number) => void,
    private readonly getCameraOffset: () => number = () => 0
  ) {
    this.bindEvents();
  }

  public getMouseLogicalPos(): RoomPointDto | null {
    return this.mouseLogicalPos;
  }

  public getDragPos(): RoomPointDto | null {
    return this.dragPos;
  }

  public getDragOffset(): { dx: number; dy: number } | null {
    return this.dragOffset;
  }

  public getHoveredEdge(): HoveredEdge | null {
    return this.hoveredEdge;
  }

  public getHoveredEntity(): { type: 'spawn' | 'exit'; id: string } | null {
    return this.hoveredEntity;
  }

  public getHoveredVertex(): { polyType: 'walkable' | 'obstacle'; polyIndex: number; vertexIndex: number } | null {
    if (!this.mouseLogicalPos || this.isDragging) return null;
    return EditorGeometry.findHoveredVertex(this.state.getRoom(), this.mouseLogicalPos, 8);
  }

  public screenToLogical(evt: MouseEvent): RoomPointDto {
    const rect = this.canvas.getBoundingClientRect();
    const room = this.state.getRoom();
    const point = screenToRoom(evt.clientX, evt.clientY, rect,
      { ...room.size, viewport: room.viewport ?? null }, this.getCameraOffset());

    return {
      x: Math.max(0, Math.min(room.size.width, Math.round(point.x))),
      y: Math.max(0, Math.min(room.size.height, Math.round(point.y)))
    };
  }

  private bindEvents(): void {
    this.canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e));
    this.canvas.addEventListener('mousedown', (e) => this.handleMouseDown(e));
    window.addEventListener('mouseup', () => this.handleMouseUp());
    this.canvas.addEventListener('mouseleave', () => this.handleMouseLeave());
  }

  private handleMouseMove(evt: MouseEvent): void {
    const pos = this.screenToLogical(evt);
    this.mouseLogicalPos = pos;

    if (this.isPreviewActive()) {
      return;
    }

    if (this.isDragging) {
      const sel = this.state.getSelection();
      if (sel.type === 'polygon' && this.dragStartLogicalPos) {
        this.dragOffset = {
          dx: pos.x - this.dragStartLogicalPos.x,
          dy: pos.y - this.dragStartLogicalPos.y
        };
      } else {
        this.dragPos = pos;
      }
      this.callbacks.onHoverChange();
      return;
    }

    const room = this.state.getRoom();

    // Check hovered edge
    const tool = this.getTool();
    if (tool === 'select') {
      const hitEdge = EditorGeometry.findHoveredEdge(room, pos);
      this.hoveredEdge = hitEdge;
      this.canvas.style.cursor = hitEdge ? 'crosshair' : 'default';

      if (!hitEdge) {
        const hitSpawn = EditorGeometry.findHoveredSpawn(room, pos);
        if (hitSpawn) {
          this.hoveredEntity = { type: 'spawn', id: hitSpawn.id };
          this.canvas.style.cursor = 'grab';
        } else {
          const hitExit = EditorGeometry.findHoveredExit(room, pos);
          if (hitExit) {
            this.hoveredEntity = { type: 'exit', id: hitExit.id };
            this.canvas.style.cursor = 'grab';
          } else {
            const hitPoly = EditorGeometry.findHoveredPolygon(room, pos);
            if (hitPoly) {
              this.hoveredEntity = null;
              this.canvas.style.cursor = 'move';
            } else {
              this.hoveredEntity = null;
            }
          }
        }
      } else {
        this.hoveredEntity = null;
      }
    } else {
      this.hoveredEdge = null;
      this.hoveredEntity = null;
      this.canvas.style.cursor = 'crosshair';
    }

    this.callbacks.onHoverChange();
  }

  private handleMouseDown(evt: MouseEvent): void {
    if (evt.button !== 0) return; // Only left click
    const pos = this.screenToLogical(evt);

    if (this.isPreviewActive()) {
      this.onPreviewClick(pos.x, pos.y);
      return;
    }

    const tool = this.getTool();
    const room = this.state.getRoom();
    this.dragStartLogicalPos = pos;

    if (tool === 'add-walkable') {
      this.state.addWalkablePolygon(pos.x, pos.y);
      this.callbacks.onStateChange();
      this.callbacks.onToolAutoReset();
      return;
    }

    if (tool === 'add-obstacle') {
      this.state.addObstaclePolygon(pos.x, pos.y);
      this.callbacks.onStateChange();
      this.callbacks.onToolAutoReset();
      return;
    }

    if (tool === 'add-spawn') {
      this.state.addSpawn(pos);
      this.callbacks.onStateChange();
      this.callbacks.onToolAutoReset();
      return;
    }

    if (tool === 'add-exit') {
      this.callbacks.onAddExitRequest(pos);
      return;
    }

    // Select mode
    if (tool === 'select') {
      const object = [...(room.objects ?? [])].reverse().find(o => pos.x >= o.position.x && pos.x <= o.position.x+o.size.width && pos.y >= o.position.y && pos.y <= o.position.y+o.size.height);
      if (object && !evt.altKey) {
        this.state.setSelection({type:'object', objectId:object.id});
        this.isDragging = true; this.dragStartLogicalPos = pos; this.dragPos = pos;
        this.callbacks.onStateChange(); return;
      }
      // 1. Edge click -> Split edge into new vertex
      if (this.hoveredEdge) {
        const edgeToSplit = this.hoveredEdge;
        this.state.splitEdge(edgeToSplit);
        this.hoveredEdge = null;
        this.isDragging = true;
        this.dragPos = pos;
        this.callbacks.onStateChange();
        return;
      }

      // 2. Vertex Hit Check
      const hitVertex = EditorGeometry.findHoveredVertex(room, pos, 8);
      if (hitVertex) {
        this.state.setSelection({
          type: 'vertex',
          polyType: hitVertex.polyType,
          polyIndex: hitVertex.polyIndex,
          vertexIndex: hitVertex.vertexIndex
        });
        this.isDragging = true;
        this.dragPos = pos;
        this.callbacks.onStateChange();
        return;
      }

      // 3. Spawn Hit Check
      const hitSpawn = EditorGeometry.findHoveredSpawn(room, pos);
      if (hitSpawn) {
        this.state.setSelection({ type: 'spawn', spawnId: hitSpawn.id });
        this.isDragging = true;
        this.dragPos = pos;
        this.callbacks.onStateChange();
        return;
      }

      // 4. Exit Hit Check
      const hitExit = EditorGeometry.findHoveredExit(room, pos);
      if (hitExit) {
        this.state.setSelection({ type: 'exit', exitId: hitExit.id });
        this.isDragging = true;
        this.dragPos = pos;
        this.callbacks.onStateChange();
        return;
      }

      // 5. Entire Polygon Hit Check
      const hitPoly = EditorGeometry.findHoveredPolygon(room, pos);
      if (hitPoly) {
        this.state.setSelection({
          type: 'polygon',
          polyType: hitPoly.polyType,
          polyIndex: hitPoly.polyIndex
        });
        this.isDragging = true;
        this.dragStartLogicalPos = pos;
        this.dragOffset = { dx: 0, dy: 0 };
        this.callbacks.onStateChange();
        return;
      }

      // Clear selection
      this.state.clearSelection();
      this.callbacks.onStateChange();
    }
  }

  private handleMouseUp(): void {
    if (!this.isDragging) return;

    const sel = this.state.getSelection();

    if (sel.type === 'polygon' && sel.polyType && sel.polyIndex !== undefined && this.dragOffset) {
      if (this.dragOffset.dx !== 0 || this.dragOffset.dy !== 0) {
        this.state.movePolygon(sel.polyType, sel.polyIndex, this.dragOffset.dx, this.dragOffset.dy);
      }
    } else if (this.dragPos && this.dragStartLogicalPos &&
      (this.dragPos.x !== this.dragStartLogicalPos.x || this.dragPos.y !== this.dragStartLogicalPos.y)) {
      if (sel.type === 'object' && this.dragStartLogicalPos) {
        if (sel.objectId) this.state.moveObject(sel.objectId, this.dragPos.x-this.dragStartLogicalPos.x, this.dragPos.y-this.dragStartLogicalPos.y);
      } else if (sel.type === 'vertex' && sel.polyType && sel.polyIndex !== undefined && sel.vertexIndex !== undefined) {
        this.state.moveVertex(sel.polyType, sel.polyIndex, sel.vertexIndex, this.dragPos);
      } else if (sel.type === 'spawn' && sel.spawnId) {
        this.state.moveSpawn(sel.spawnId, this.dragPos);
      } else if (sel.type === 'exit' && sel.exitId) {
        this.state.moveExit(sel.exitId, this.dragPos);
      }
    }

    this.isDragging = false;
    this.dragPos = null;
    this.dragStartLogicalPos = null;
    this.dragOffset = null;
    this.callbacks.onStateChange();
  }

  private handleMouseLeave(): void {
    this.mouseLogicalPos = null;
    this.hoveredEdge = null;
    this.hoveredEntity = null;
    this.callbacks.onHoverChange();
  }
}
