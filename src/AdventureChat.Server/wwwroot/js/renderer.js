export class RoomRenderer {
    canvas;
    room;
    ctx;
    backgroundImage;
    characterSprite;
    backgroundLoaded = false;
    spriteLoaded = false;
    clickMarkers = [];
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
        this.backgroundImage = new Image();
        this.backgroundImage.src = this.room.backgroundUrl;
        this.backgroundImage.onload = () => {
            this.backgroundLoaded = true;
        };
        this.characterSprite = new Image();
        this.characterSprite.src = 'assets/images/character.png';
        this.characterSprite.onload = () => {
            this.spriteLoaded = true;
        };
    }
    addClickMarker(x, y, type) {
        this.clickMarkers.push({
            x,
            y,
            type,
            startTime: performance.now(),
            duration: 600,
        });
    }
    toggleDebugOverlay() {
        this.showDebugOverlay = !this.showDebugOverlay;
        return this.showDebugOverlay;
    }
    render(character) {
        const { ctx, canvas, room } = this;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        // 1. Draw Background
        if (this.backgroundLoaded) {
            ctx.drawImage(this.backgroundImage, 0, 0, room.width, room.height);
        }
        else {
            ctx.fillStyle = '#2d3748';
            ctx.fillRect(0, 0, room.width, room.height);
        }
        // 2. Debug Overlay (Polygons, Obstacles, Waypoint path)
        if (this.showDebugOverlay) {
            this.drawDebugOverlay(character);
        }
        // 3. Target indicator (if moving)
        if (character.target && character.state === 'walk') {
            this.drawTargetMarker(character.target.x, character.target.y);
        }
        // 4. Click Markers (Valid / Invalid feedback animations)
        this.drawClickMarkers();
        // 5. Draw Character
        this.drawCharacter(character);
    }
    drawDebugOverlay(character) {
        const { ctx, room } = this;
        ctx.save();
        // A. Outer Boundary
        const outer = room.geometry?.outerBoundary;
        if (outer && outer.length >= 3 && outer[0]) {
            ctx.fillStyle = 'rgba(72, 187, 120, 0.18)';
            ctx.strokeStyle = 'rgba(72, 187, 120, 0.85)';
            ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.moveTo(outer[0].x, outer[0].y);
            for (let i = 1; i < outer.length; i++) {
                const pt = outer[i];
                if (pt) {
                    ctx.lineTo(pt.x, pt.y);
                }
            }
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
        }
        // B. Obstacles
        if (room.geometry?.obstacles) {
            for (const obstacle of room.geometry.obstacles) {
                if (!obstacle || obstacle.length < 3 || !obstacle[0])
                    continue;
                ctx.fillStyle = 'rgba(245, 101, 101, 0.35)';
                ctx.strokeStyle = 'rgba(245, 101, 101, 0.9)';
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(obstacle[0].x, obstacle[0].y);
                for (let i = 1; i < obstacle.length; i++) {
                    const pt = obstacle[i];
                    if (pt) {
                        ctx.lineTo(pt.x, pt.y);
                    }
                }
                ctx.closePath();
                ctx.fill();
                ctx.stroke();
            }
        }
        // C. Active Movement Path
        if (character.target && character.state === 'walk') {
            ctx.strokeStyle = 'rgba(236, 201, 75, 0.85)';
            ctx.lineWidth = 1.5;
            ctx.setLineDash([4, 4]);
            ctx.beginPath();
            ctx.moveTo(character.position.x, character.position.y);
            ctx.lineTo(character.target.x, character.target.y);
            for (const wp of character.waypoints) {
                ctx.lineTo(wp.x, wp.y);
            }
            ctx.stroke();
            ctx.setLineDash([]);
        }
        // D. Character Footpoint Indicator
        ctx.strokeStyle = '#ecc94b';
        ctx.lineWidth = 1;
        const fx = Math.round(character.position.x);
        const fy = Math.round(character.position.y);
        ctx.beginPath();
        ctx.moveTo(fx - 4, fy);
        ctx.lineTo(fx + 4, fy);
        ctx.moveTo(fx, fy - 4);
        ctx.lineTo(fx, fy + 4);
        ctx.stroke();
        // E. Debug Badge
        ctx.font = '10px monospace';
        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
        ctx.fillRect(8, 8, 205, 18);
        ctx.fillStyle = '#68d391';
        ctx.textAlign = 'left';
        ctx.textBaseline = 'middle';
        ctx.fillText('Debug: AN [D = Umschalten]', 14, 17);
        ctx.restore();
    }
    drawClickMarkers() {
        const now = performance.now();
        this.clickMarkers = this.clickMarkers.filter((m) => now - m.startTime < m.duration);
        const { ctx } = this;
        ctx.save();
        for (const marker of this.clickMarkers) {
            const progress = (now - marker.startTime) / marker.duration;
            const alpha = Math.max(0, 1 - progress);
            if (marker.type === 'valid') {
                // Green / Gold walk indicator
                const radius = 3 + progress * 6;
                ctx.strokeStyle = `rgba(72, 187, 120, ${alpha * 0.9})`;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.arc(marker.x, marker.y, radius, 0, Math.PI * 2);
                ctx.stroke();
                ctx.fillStyle = `rgba(72, 187, 120, ${alpha})`;
                ctx.beginPath();
                ctx.arc(marker.x, marker.y, 2, 0, Math.PI * 2);
                ctx.fill();
            }
            else {
                // Red 'X' invalid / blocked indicator
                const size = 5 + progress * 2;
                ctx.strokeStyle = `rgba(245, 101, 101, ${alpha})`;
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.moveTo(marker.x - size, marker.y - size);
                ctx.lineTo(marker.x + size, marker.y + size);
                ctx.moveTo(marker.x + size, marker.y - size);
                ctx.lineTo(marker.x - size, marker.y + size);
                ctx.stroke();
                // Subtle pulsing ring around X
                const ringRadius = 4 + progress * 8;
                ctx.strokeStyle = `rgba(245, 101, 101, ${alpha * 0.6})`;
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.arc(marker.x, marker.y, ringRadius, 0, Math.PI * 2);
                ctx.stroke();
            }
        }
        ctx.restore();
    }
    drawTargetMarker(x, y) {
        const { ctx } = this;
        ctx.save();
        ctx.strokeStyle = 'rgba(255, 230, 100, 0.7)';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.arc(x, y, 5, 0, Math.PI * 2);
        ctx.stroke();
        ctx.fillStyle = 'rgba(255, 230, 100, 0.9)';
        ctx.beginPath();
        ctx.arc(x, y, 2, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    }
    drawCharacter(character) {
        const { ctx } = this;
        const { position, facing, state, walkFrame, name } = character;
        const frameWidth = 48;
        const frameHeight = 64;
        const footX = Math.round(position.x);
        const footY = Math.round(position.y);
        // Frame calculation
        let frameIndex = 0;
        if (state === 'walk') {
            // frames 0, 1, 2, 3 cycle
            frameIndex = walkFrame % 4;
        }
        let sourceY = 0;
        let flipX = false;
        if (facing === 'up') {
            sourceY = frameHeight; // Row 1: Back view (Rückenansicht)
        }
        else if (facing === 'down') {
            sourceY = frameHeight * 2; // Row 2: Front view
        }
        else if (facing === 'left') {
            sourceY = 0; // Row 0: Side view
            flipX = true;
        }
        else {
            sourceY = 0; // Row 0: Side view
            flipX = false;
        }
        ctx.save();
        if (this.spriteLoaded) {
            if (flipX) {
                ctx.translate(footX, footY);
                ctx.scale(-1, 1);
                ctx.drawImage(this.characterSprite, frameIndex * frameWidth, sourceY, frameWidth, frameHeight, -frameWidth / 2, -frameHeight, frameWidth, frameHeight);
            }
            else {
                ctx.drawImage(this.characterSprite, frameIndex * frameWidth, sourceY, frameWidth, frameHeight, footX - frameWidth / 2, footY - frameHeight, frameWidth, frameHeight);
            }
        }
        else {
            // Fallback rectangle if sprite is still loading
            ctx.fillStyle = '#3182ce';
            ctx.fillRect(footX - 16, footY - 58, 32, 58);
        }
        ctx.restore();
        // Draw Name Tag
        ctx.save();
        ctx.font = '10px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'bottom';
        const textY = footY - frameHeight - 4;
        ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
        ctx.fillRect(footX - 28, textY - 11, 56, 13);
        ctx.fillStyle = '#ffffff';
        ctx.fillText(name, footX, textY);
        ctx.restore();
    }
}
//# sourceMappingURL=renderer.js.map