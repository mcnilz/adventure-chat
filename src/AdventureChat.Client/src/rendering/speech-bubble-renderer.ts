import type { Character } from '../types.js';

export class SpeechBubbleRenderer {
  public drawSpeechBubble(
    ctx: CanvasRenderingContext2D,
    character: Character,
    roomWidth: number,
    now: number,
    actorHeight = 64
  ): void {
    const bubble = character.speechBubble;
    if (!bubble) return;

    const elapsed = now - bubble.startTime;
    if (elapsed >= bubble.duration) {
      character.speechBubble = null;
      return;
    }

    const footX = Math.round(character.position.x);
    const footY = Math.round(character.position.y);
    const frameHeight = actorHeight;
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
    if (bubbleX + bubbleWidth > roomWidth - 8) {
      bubbleX = roomWidth - 8 - bubbleWidth;
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
