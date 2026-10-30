import type { Character, RoomDefinition } from './types.js';

export interface ClickMarker {
  x: number;
  y: number;
  type: 'valid' | 'invalid';
  startTime: number;
  duration: number;
}

export class RoomRenderer {
  private readonly ctx: CanvasRenderingContext2D;
  private readonly backgroundImage: HTMLImageElement;
  private readonly characterSprite: HTMLImageElement;
  private backgroundLoaded = false;
  private spriteLoaded = false;
  private clickMarkers: ClickMarker[] = [];
  public showDebugOverlay = false;

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly room: RoomDefinition
  ) {
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

  public addClickMarker(x: number, y: number, type: 'valid' | 'invalid'): void {
    this.clickMarkers.push({
      x,
      y,
      type,
      startTime: performance.now(),
      duration: 600,
    });
  }

  public toggleDebugOverlay(): boolean {
    this.showDebugOverlay = !this.showDebugOverlay;
    return this.showDebugOverlay;
  }

  public render(localCharacter: Character, remoteCharacters: Character[] = [], connectionStatus: 'connected' | 'connecting' | 'disconnected' = 'connected'): void {
    const { ctx, canvas, room } = this;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // 1. Draw Background
    if (this.backgroundLoaded) {
      ctx.drawImage(this.backgroundImage, 0, 0, room.width, room.height);
    } else {
      ctx.fillStyle = '#2d3748';
      ctx.fillRect(0, 0, room.width, room.height);
    }

    const allCharacters = [localCharacter, ...remoteCharacters];

    // 2. Debug Overlay (Polygons, Obstacles, Waypoint paths)
    if (this.showDebugOverlay) {
      this.drawDebugOverlay(allCharacters);
    }

    // 3. Target indicator (if local character is moving)
    if (localCharacter.target && localCharacter.state === 'walk') {
      this.drawTargetMarker(localCharacter.target.x, localCharacter.target.y);
    }

    // 4. Click Markers (Valid / Invalid feedback animations)
    this.drawClickMarkers();

    // 5. Draw All Characters sorted by Y position (painter's algorithm)
    allCharacters.sort((a, b) => a.position.y - b.position.y);
    for (const char of allCharacters) {
      const isLocal = char.id === localCharacter.id;
      this.drawCharacter(char, isLocal);
    }

    // 6. Draw Speech Bubbles above characters
    const now = performance.now();
    for (const char of allCharacters) {
      this.drawSpeechBubble(char, now);
    }

    // 7. Connection Status Indicator (if not connected)
    if (connectionStatus !== 'connected') {
      this.drawConnectionStatus(connectionStatus);
    }
  }

  private drawDebugOverlay(characters: Character[]): void {
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
        if (!obstacle || obstacle.length < 3 || !obstacle[0]) continue;

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

    // C. Active Movement Paths for all moving characters
    for (const character of characters) {
      if (character.target && character.state === 'walk') {
        ctx.strokeStyle = character.id.startsWith('local') || character.id === characters[0]?.id
          ? 'rgba(236, 201, 75, 0.85)'
          : 'rgba(99, 179, 237, 0.85)';
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

      // Footpoint Indicator
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
    }

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

  private drawConnectionStatus(status: 'connecting' | 'disconnected'): void {
    const { ctx } = this;
    ctx.save();
    ctx.font = '11px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    const text = status === 'connecting' ? 'Verbindung wird hergestellt...' : 'Verbindung getrennt. Wiederverbindung...';
    const textWidth = ctx.measureText(text).width;
    const badgeWidth = textWidth + 24;
    const badgeHeight = 22;
    const x = 320;
    const y = 20;

    ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
    ctx.fillRect(x - badgeWidth / 2, y - badgeHeight / 2, badgeWidth, badgeHeight);

    ctx.fillStyle = status === 'connecting' ? '#ecc94b' : '#fc8181';
    ctx.fillText(text, x, y);
    ctx.restore();
  }

  private drawClickMarkers(): void {
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
      } else {
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

  private drawTargetMarker(x: number, y: number): void {
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

  private drawCharacter(character: Character, isLocal: boolean): void {
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
    } else if (facing === 'down') {
      sourceY = frameHeight * 2; // Row 2: Front view
    } else if (facing === 'left') {
      sourceY = 0; // Row 0: Side view
      flipX = true;
    } else {
      sourceY = 0; // Row 0: Side view
      flipX = false;
    }

    ctx.save();

    if (this.spriteLoaded) {
      if (flipX) {
        ctx.translate(footX, footY);
        ctx.scale(-1, 1);
        ctx.drawImage(
          this.characterSprite,
          frameIndex * frameWidth,
          sourceY,
          frameWidth,
          frameHeight,
          -frameWidth / 2,
          -frameHeight,
          frameWidth,
          frameHeight
        );
      } else {
        ctx.drawImage(
          this.characterSprite,
          frameIndex * frameWidth,
          sourceY,
          frameWidth,
          frameHeight,
          footX - frameWidth / 2,
          footY - frameHeight,
          frameWidth,
          frameHeight
        );
      }
    } else {
      // Fallback rectangle if sprite is still loading
      ctx.fillStyle = isLocal ? '#3182ce' : '#38a169';
      ctx.fillRect(footX - 16, footY - 58, 32, 58);
    }

    ctx.restore();

    // Draw Name Tag
    ctx.save();
    ctx.font = '10px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'bottom';

    const textWidth = ctx.measureText(name).width;
    const badgeWidth = Math.max(textWidth + 10, 36);
    const badgeHeight = 14;
    const textY = footY - frameHeight - 4;

    ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
    ctx.fillRect(footX - badgeWidth / 2, textY - badgeHeight + 2, badgeWidth, badgeHeight);

    if (isLocal) {
      ctx.strokeStyle = 'rgba(236, 201, 75, 0.8)';
      ctx.lineWidth = 1;
      ctx.strokeRect(footX - badgeWidth / 2, textY - badgeHeight + 2, badgeWidth, badgeHeight);
      ctx.fillStyle = '#ecc94b';
    } else {
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.3)';
      ctx.lineWidth = 1;
      ctx.strokeRect(footX - badgeWidth / 2, textY - badgeHeight + 2, badgeWidth, badgeHeight);
      ctx.fillStyle = '#ffffff';
    }

    ctx.fillText(name, footX, textY);
    ctx.restore();
  }

  private drawSpeechBubble(character: Character, now: number): void {
    const bubble = character.speechBubble;
    if (!bubble) return;

    const elapsed = now - bubble.startTime;
    if (elapsed >= bubble.duration) {
      character.speechBubble = null;
      return;
    }

    const { ctx } = this;
    const footX = Math.round(character.position.x);
    const footY = Math.round(character.position.y);
    const frameHeight = 64;
    const nameBadgeHeight = 14;
    const baseAnchorY = footY - frameHeight - nameBadgeHeight - 8;

    ctx.save();
    ctx.font = 'bold 11px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';

    const maxBubbleWidth = 180;
    const words = bubble.text.split(' ');
    const lines: string[] = [];
    let currentLine = '';

    for (const word of words) {
      const testLine = currentLine ? `${currentLine} ${word}` : word;
      const metrics = ctx.measureText(testLine);
      if (metrics.width > maxBubbleWidth && currentLine) {
        lines.push(currentLine);
        currentLine = word;
      } else {
        currentLine = testLine;
      }
    }
    if (currentLine) {
      lines.push(currentLine);
    }

    const lineHeight = 14;
    let measuredMaxWidth = 0;
    for (const line of lines) {
      const w = ctx.measureText(line).width;
      if (w > measuredMaxWidth) {
        measuredMaxWidth = w;
      }
    }

    const paddingX = 8;
    const paddingY = 6;
    const bubbleWidth = Math.max(measuredMaxWidth + paddingX * 2, 40);
    const bubbleHeight = lines.length * lineHeight + paddingY * 2;

    // Clamp horizontally within room
    let bubbleX = footX - bubbleWidth / 2;
    if (bubbleX < 8) bubbleX = 8;
    if (bubbleX + bubbleWidth > this.room.width - 8) {
      bubbleX = this.room.width - 8 - bubbleWidth;
    }
    const bubbleY = baseAnchorY - bubbleHeight;

    // Fade out in last 400ms
    const remaining = bubble.duration - elapsed;
    let alpha = 1.0;
    if (remaining < 400) {
      alpha = Math.max(0, remaining / 400);
    }
    ctx.globalAlpha = alpha;

    // Draw speech bubble with pointer
    const radius = 5;
    ctx.beginPath();
    ctx.moveTo(bubbleX + radius, bubbleY);
    ctx.lineTo(bubbleX + bubbleWidth - radius, bubbleY);
    ctx.quadraticCurveTo(bubbleX + bubbleWidth, bubbleY, bubbleX + bubbleWidth, bubbleY + radius);
    ctx.lineTo(bubbleX + bubbleWidth, bubbleY + bubbleHeight - radius);
    ctx.quadraticCurveTo(bubbleX + bubbleWidth, bubbleY + bubbleHeight, bubbleX + bubbleWidth - radius, bubbleY + bubbleHeight);

    const pointerX = Math.min(Math.max(footX, bubbleX + 12), bubbleX + bubbleWidth - 12);
    ctx.lineTo(pointerX + 5, bubbleY + bubbleHeight);
    ctx.lineTo(pointerX, bubbleY + bubbleHeight + 6);
    ctx.lineTo(pointerX - 5, bubbleY + bubbleHeight);

    ctx.lineTo(bubbleX + radius, bubbleY + bubbleHeight);
    ctx.quadraticCurveTo(bubbleX, bubbleY + bubbleHeight, bubbleX, bubbleY + bubbleHeight - radius);
    ctx.lineTo(bubbleX, bubbleY + radius);
    ctx.quadraticCurveTo(bubbleX, bubbleY, bubbleX + radius, bubbleY);
    ctx.closePath();

    ctx.fillStyle = '#ffffff';
    ctx.fill();
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Draw text lines
    ctx.fillStyle = '#0f172a';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    for (let i = 0; i < lines.length; i++) {
      const lineText = lines[i];
      if (lineText !== undefined) {
        const lineY = bubbleY + paddingY + (i + 0.5) * lineHeight;
        ctx.fillText(lineText, bubbleX + bubbleWidth / 2, lineY);
      }
    }

    ctx.restore();
  }
}
