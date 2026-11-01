import type { Character } from '../types.js';

export class CharacterRenderer {
  private readonly characterSprite: HTMLImageElement;
  private spriteLoaded = false;

  public constructor() {
    this.characterSprite = new Image();
    this.characterSprite.src = '/app-assets/character.png';
    this.characterSprite.onload = () => {
      this.spriteLoaded = true;
    };
  }

  public drawCharacter(ctx: CanvasRenderingContext2D, character: Character, isLocal: boolean): void {
    const { position, facing, state, walkFrame, name } = character;

    const frameWidth = 48;
    const frameHeight = 64;
    const footX = Math.round(position.x);
    const footY = Math.round(position.y);

    // Frame calculation
    let frameIndex = 0;
    if (state === 'walk') {
      frameIndex = walkFrame % 4;
    }

    let sourceY = 0;
    let flipX = false;

    if (facing === 'up') {
      sourceY = frameHeight; // Row 1: Back view
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
}
