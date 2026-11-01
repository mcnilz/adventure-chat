import { EditorGeometry } from '../core/editor-geometry.js';
export class EditorCanvasInput {
    canvas;
    state;
    callbacks;
    getTool;
    isPreviewActive;
    onPreviewClick;
    mouseLogicalPos = null;
    isDragging = false;
    dragPos = null;
    dragStartLogicalPos = null;
    dragOffset = null;
    hoveredEdge = null;
    hoveredEntity = null;
    constructor(canvas, state, callbacks, getTool, isPreviewActive, onPreviewClick) {
        this.canvas = canvas;
        this.state = state;
        this.callbacks = callbacks;
        this.getTool = getTool;
        this.isPreviewActive = isPreviewActive;
        this.onPreviewClick = onPreviewClick;
        this.bindEvents();
    }
    getMouseLogicalPos() {
        return this.mouseLogicalPos;
    }
    getDragPos() {
        return this.dragPos;
    }
    getDragOffset() {
        return this.dragOffset;
    }
    getHoveredEdge() {
        return this.hoveredEdge;
    }
    getHoveredEntity() {
        return this.hoveredEntity;
    }
    getHoveredVertex() {
        if (!this.mouseLogicalPos || this.isDragging)
            return null;
        return EditorGeometry.findHoveredVertex(this.state.getRoom(), this.mouseLogicalPos, 8);
    }
    screenToLogical(evt) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        const rawX = (evt.clientX - rect.left) * scaleX;
        const rawY = (evt.clientY - rect.top) * scaleY;
        return {
            x: Math.max(0, Math.min(this.canvas.width, Math.round(rawX))),
            y: Math.max(0, Math.min(this.canvas.height, Math.round(rawY)))
        };
    }
    bindEvents() {
        this.canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e));
        this.canvas.addEventListener('mousedown', (e) => this.handleMouseDown(e));
        window.addEventListener('mouseup', () => this.handleMouseUp());
        this.canvas.addEventListener('mouseleave', () => this.handleMouseLeave());
    }
    handleMouseMove(evt) {
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
            }
            else {
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
                }
                else {
                    const hitExit = EditorGeometry.findHoveredExit(room, pos);
                    if (hitExit) {
                        this.hoveredEntity = { type: 'exit', id: hitExit.id };
                        this.canvas.style.cursor = 'grab';
                    }
                    else {
                        const hitPoly = EditorGeometry.findHoveredPolygon(room, pos);
                        if (hitPoly) {
                            this.hoveredEntity = null;
                            this.canvas.style.cursor = 'move';
                        }
                        else {
                            this.hoveredEntity = null;
                        }
                    }
                }
            }
            else {
                this.hoveredEntity = null;
            }
        }
        else {
            this.hoveredEdge = null;
            this.hoveredEntity = null;
            this.canvas.style.cursor = 'crosshair';
        }
        this.callbacks.onHoverChange();
    }
    handleMouseDown(evt) {
        if (evt.button !== 0)
            return; // Only left click
        const pos = this.screenToLogical(evt);
        if (this.isPreviewActive()) {
            this.onPreviewClick(pos.x, pos.y);
            return;
        }
        const tool = this.getTool();
        const room = this.state.getRoom();
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
    handleMouseUp() {
        if (!this.isDragging)
            return;
        const sel = this.state.getSelection();
        if (sel.type === 'polygon' && sel.polyType && sel.polyIndex !== undefined && this.dragOffset) {
            if (this.dragOffset.dx !== 0 || this.dragOffset.dy !== 0) {
                this.state.movePolygon(sel.polyType, sel.polyIndex, this.dragOffset.dx, this.dragOffset.dy);
            }
        }
        else if (this.dragPos) {
            if (sel.type === 'vertex' && sel.polyType && sel.polyIndex !== undefined && sel.vertexIndex !== undefined) {
                this.state.moveVertex(sel.polyType, sel.polyIndex, sel.vertexIndex, this.dragPos);
            }
            else if (sel.type === 'spawn' && sel.spawnId) {
                this.state.moveSpawn(sel.spawnId, this.dragPos);
            }
            else if (sel.type === 'exit' && sel.exitId) {
                this.state.moveExit(sel.exitId, this.dragPos);
            }
        }
        this.isDragging = false;
        this.dragPos = null;
        this.dragStartLogicalPos = null;
        this.dragOffset = null;
        this.callbacks.onStateChange();
    }
    handleMouseLeave() {
        this.mouseLogicalPos = null;
        this.hoveredEdge = null;
        this.hoveredEntity = null;
        this.callbacks.onHoverChange();
    }
}
//# sourceMappingURL=editor-canvas-input.js.map