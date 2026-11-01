import { isPointWalkable } from '../math.js';
import { RoomRenderer } from '../rendering/renderer.js';
import { assignPathToCharacter, updateCharacterMovement } from '../simulation/movement.js';
export class EditorPreview {
    canvas;
    previewRenderer = null;
    previewCharacter = null;
    previewAnimationId = null;
    lastFrameTime = performance.now();
    isRunning = false;
    constructor(canvas) {
        this.canvas = canvas;
    }
    isActive() {
        return this.isRunning;
    }
    start(roomDef) {
        this.isRunning = true;
        this.previewRenderer = new RoomRenderer(this.canvas, roomDef);
        // Initial character spawn
        const defaultSpawn = roomDef.spawnPoint ?? { x: 320, y: 300 };
        this.previewCharacter = {
            id: 'preview-player',
            name: 'Tester',
            position: { ...defaultSpawn },
            facing: 'down',
            state: 'idle',
            target: null,
            waypoints: [],
            speed: 120,
            walkFrame: 0,
            frameTimer: 0,
            speechBubble: {
                text: 'Vorschaumodus aktiv! Klicke, um zu laufen.',
                startTime: performance.now(),
                duration: 4000
            }
        };
        this.lastFrameTime = performance.now();
        this.loop();
    }
    stop() {
        this.isRunning = false;
        if (this.previewAnimationId !== null) {
            cancelAnimationFrame(this.previewAnimationId);
            this.previewAnimationId = null;
        }
        this.previewRenderer = null;
        this.previewCharacter = null;
    }
    handleCanvasClick(logicalX, logicalY, roomDef) {
        if (!this.previewCharacter || !this.isRunning)
            return;
        const target = { x: logicalX, y: logicalY };
        if (isPointWalkable(target, roomDef.geometry.outerBoundary, roomDef.geometry.obstacles)) {
            this.previewRenderer?.addClickMarker(logicalX, logicalY, 'valid');
            assignPathToCharacter(this.previewCharacter, [this.previewCharacter.position, target]);
        }
        else {
            this.previewRenderer?.addClickMarker(logicalX, logicalY, 'invalid');
        }
    }
    loop = () => {
        if (!this.isRunning)
            return;
        const now = performance.now();
        const dt = Math.min((now - this.lastFrameTime) / 1000, 0.1);
        this.lastFrameTime = now;
        if (this.previewCharacter) {
            updateCharacterMovement(this.previewCharacter, dt);
        }
        if (this.previewRenderer && this.previewCharacter) {
            this.previewRenderer.render(this.previewCharacter, []);
        }
        this.previewAnimationId = requestAnimationFrame(this.loop);
    };
}
//# sourceMappingURL=editor-preview.js.map