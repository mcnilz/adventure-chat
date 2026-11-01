import { CLICK_MARKER_DURATION } from '../constants.js';
import { CharacterRenderer } from './character-renderer.js';
import { OverlayRenderer } from './overlay-renderer.js';
import { SpeechBubbleRenderer } from './speech-bubble-renderer.js';
export class RoomRenderer {
    canvas;
    room;
    ctx;
    backgroundImage;
    characterRenderer;
    speechBubbleRenderer;
    overlayRenderer;
    backgroundLoaded = false;
    clickMarkers = [];
    hoveredExit = null;
    showDebugOverlay = false;
    constructor(canvas, room) {
        this.canvas = canvas;
        this.room = room;
        const context = this.canvas.getContext('2d');
        if (!context) {
            throw new Error('Canvas 2D context is not supported');
        }
        this.ctx = context;
        this.ctx.imageSmoothingEnabled = false;
        this.characterRenderer = new CharacterRenderer();
        this.speechBubbleRenderer = new SpeechBubbleRenderer();
        this.overlayRenderer = new OverlayRenderer();
        this.backgroundImage = new Image();
        this.backgroundImage.src = this.room.backgroundUrl;
        this.backgroundImage.onload = () => {
            this.backgroundLoaded = true;
        };
    }
    setHoveredExit(exit) {
        this.hoveredExit = exit;
    }
    setRoom(newRoom) {
        this.room = newRoom;
        if (!this.backgroundImage.src.endsWith(newRoom.backgroundUrl)) {
            this.backgroundLoaded = false;
            this.backgroundImage.src = newRoom.backgroundUrl;
        }
    }
    addClickMarker(x, y, type) {
        this.clickMarkers.push({
            x,
            y,
            type,
            startTime: performance.now(),
            duration: CLICK_MARKER_DURATION,
        });
    }
    toggleDebugOverlay() {
        this.showDebugOverlay = !this.showDebugOverlay;
        return this.showDebugOverlay;
    }
    render(localCharacter, remoteCharacters = [], connectionStatus = 'connected') {
        const { ctx, canvas, room } = this;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        // 1. Background Image
        if (this.backgroundLoaded) {
            ctx.drawImage(this.backgroundImage, 0, 0, room.width, room.height);
        }
        else {
            ctx.fillStyle = '#2d3748';
            ctx.fillRect(0, 0, room.width, room.height);
        }
        // 1b. Exit doorway debug rendering (only in debug mode)
        if (this.showDebugOverlay) {
            this.overlayRenderer.drawExits(ctx, room);
        }
        const allCharacters = [localCharacter, ...remoteCharacters];
        // 2. Debug Overlay
        if (this.showDebugOverlay) {
            this.overlayRenderer.drawDebugOverlay(ctx, room, allCharacters);
        }
        // 3. Target Position Marker
        if (localCharacter.target && localCharacter.state === 'walk') {
            this.overlayRenderer.drawTargetMarker(ctx, localCharacter.target.x, localCharacter.target.y);
        }
        // 4. Click Feedback Ripples
        const now = performance.now();
        this.clickMarkers = this.overlayRenderer.drawClickMarkers(ctx, this.clickMarkers, now);
        // 5. Draw Characters sorted by Y position (painter's algorithm)
        allCharacters.sort((a, b) => a.position.y - b.position.y);
        for (const char of allCharacters) {
            const isLocal = char.id === localCharacter.id;
            this.characterRenderer.drawCharacter(ctx, char, isLocal);
        }
        // 6. Draw Speech Bubbles
        for (const char of allCharacters) {
            this.speechBubbleRenderer.drawSpeechBubble(ctx, char, room.width, now);
        }
        // 7. Hovered Exit Highlight (only in debug mode to keep screen clean)
        if (this.hoveredExit && this.showDebugOverlay) {
            this.overlayRenderer.drawHoveredExitAction(ctx, this.hoveredExit);
        }
        // 8. Connection Status Indicator
        if (connectionStatus !== 'connected') {
            this.overlayRenderer.drawConnectionStatus(ctx, connectionStatus);
        }
    }
}
//# sourceMappingURL=renderer.js.map