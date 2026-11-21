import type { Character } from '../types.js';

export interface SpeechLayout {
  readonly font: string;
  readonly lineHeight: number;
  readonly lines: readonly string[];
}

const font = (size: number): string =>
  `bold ${size}px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`;

export class SpeechBubbleRenderer {
  public prepareDialogue(ctx: CanvasRenderingContext2D, text: string, width: number, height: number): SpeechLayout | null {
    if (width <= 32) return null;
    const maxTextWidth = Math.min(240, width - 32);
    ctx.save();
    try {
      for (let size = 11; size >= 8; size--) {
        ctx.font = font(size);
        const lineHeight = Math.ceil(size * 14 / 11);
        const lines = this.wrap(ctx, text, maxTextWidth, true);
        if (lines.length * lineHeight + 34 <= height && lines.every(line => ctx.measureText(line).width <= maxTextWidth))
          return { font: ctx.font, lineHeight, lines };
      }
      return null;
    } finally {
      ctx.restore();
    }
  }

  public drawSpeechBubble(
    ctx: CanvasRenderingContext2D,
    character: Character,
    viewportRight: number,
    now: number,
    actorHeight = 64,
    viewportLeft = 0,
    keepVisibleVertically = false,
    dialogue?: SpeechLayout
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
    const offscreenLeft = footX < viewportLeft;
    const offscreenRight = footX > viewportRight;
    const frameHeight = actorHeight;
    const nameBadgeHeight = 14;
    const baseAnchorY = footY - frameHeight - nameBadgeHeight - 8;

    ctx.save();
    const paddingX = 8;
    const paddingY = 6;
    ctx.font = dialogue?.font ?? font(11);
    const lineHeight = dialogue?.lineHeight ?? 14;
    const lines = dialogue?.lines ?? this.wrap(ctx, bubble.text, 180, false);
    let measuredMaxWidth = 0;
    for (const line of lines) {
      const w = ctx.measureText(line).width;
      if (w > measuredMaxWidth) {
        measuredMaxWidth = w;
      }
    }

    const minBubbleWidth = dialogue ? Math.min(40, viewportRight - viewportLeft - 16) : 40;
    const bubbleWidth = Math.max(measuredMaxWidth + paddingX * 2, minBubbleWidth);
    const bubbleHeight = lines.length * lineHeight + paddingY * 2;

    // Clamp horizontally within the visible viewport
    let bubbleX = footX - bubbleWidth / 2;
    if (bubbleX < viewportLeft + 8) bubbleX = viewportLeft + 8;
    if (bubbleX + bubbleWidth > viewportRight - 8) {
      bubbleX = viewportRight - 8 - bubbleWidth;
    }
    const preferredY = baseAnchorY - bubbleHeight;
    const bubbleY = dialogue ? Math.max(8, Math.min(preferredY, ctx.canvas.height - bubbleHeight - 14))
      : keepVisibleVertically ? Math.max(24, preferredY) : preferredY;

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
    const sidePointerY = bubbleY + bubbleHeight / 2;
    if (offscreenRight) {
      ctx.lineTo(bubbleX + bubbleWidth, sidePointerY - 5);
      ctx.lineTo(viewportRight - 2, sidePointerY);
      ctx.lineTo(bubbleX + bubbleWidth, sidePointerY + 5);
    }
    ctx.lineTo(bubbleX + bubbleWidth, bubbleY + bubbleHeight - radius);
    ctx.quadraticCurveTo(bubbleX + bubbleWidth, bubbleY + bubbleHeight, bubbleX + bubbleWidth - radius, bubbleY + bubbleHeight);

    if (!offscreenLeft && !offscreenRight) {
      const pointerX = Math.min(Math.max(footX, bubbleX + 12), bubbleX + bubbleWidth - 12);
      ctx.lineTo(pointerX + 5, bubbleY + bubbleHeight);
      ctx.lineTo(pointerX, bubbleY + bubbleHeight + 6);
      ctx.lineTo(pointerX - 5, bubbleY + bubbleHeight);
    }

    ctx.lineTo(bubbleX + radius, bubbleY + bubbleHeight);
    ctx.quadraticCurveTo(bubbleX, bubbleY + bubbleHeight, bubbleX, bubbleY + bubbleHeight - radius);
    if (offscreenLeft) {
      ctx.lineTo(bubbleX, sidePointerY + 5);
      ctx.lineTo(viewportLeft + 2, sidePointerY);
      ctx.lineTo(bubbleX, sidePointerY - 5);
    }
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

  private wrap(ctx: CanvasRenderingContext2D, text: string, width: number, splitWideWords: boolean): string[] {
    const lines: string[] = [];
    let line = '';
    for (const word of splitWideWords ? text.trim().split(/\s+/u) : text.split(' ')) {
      const withWord = line ? `${line} ${word}` : word;
      if (ctx.measureText(withWord).width <= width) {
        line = withWord;
        continue;
      }
      if (line) lines.push(line);
      if (!splitWideWords || ctx.measureText(word).width <= width) {
        line = word;
        continue;
      }
      line = '';
      for (const { segment: character } of new Intl.Segmenter(undefined, { granularity: 'grapheme' }).segment(word)) {
        if (line && ctx.measureText(line + character).width > width) {
          lines.push(line);
          line = '';
        }
        line += character;
      }
    }
    if (line) lines.push(line);
    return lines;
  }
}
