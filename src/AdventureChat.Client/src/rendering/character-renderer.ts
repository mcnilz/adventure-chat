import { actorHeightAt } from './depth-scale.js';
import { drawTopDownFigure } from './top-down-figure.js';
import type { Character, RoomDefinition } from '../types.js';

export class CharacterRenderer {
  private readonly images = new Map<string, HTMLImageElement>();
  private readonly characterSprite: HTMLImageElement;
  private spriteLoaded = false;

  public constructor() {
    this.characterSprite = new Image();
    this.characterSprite.src = '/app-assets/character.png';
    this.characterSprite.onload = () => {
      this.spriteLoaded = true;
    };
  }

  public drawCharacter(ctx: CanvasRenderingContext2D, character: Character, isLocal: boolean, room?: RoomDefinition, showName = true): void {
    const { position, facing, state, walkFrame } = character;

    const appearance = character.appearance;
    let sprite = this.characterSprite;
    if (appearance) {
      const imageUrl = room?.perspective === 'top-down' ? appearance.topDownImageUrl ?? appearance.imageUrl : appearance.imageUrl;
      let image = this.images.get(imageUrl);
      if (!image) { image = new Image(); image.src = imageUrl; this.images.set(imageUrl, image); }
      sprite = image;
    }
    const loaded = appearance ? sprite.complete && sprite.naturalWidth > 0 : this.spriteLoaded;
    const frameWidth = appearance?.frameWidth ?? 48;
    const frameHeight = appearance?.frameHeight ?? 64;
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

    const height = actorHeightAt(room, position.y);
    ctx.translate(footX, footY);
    ctx.scale(height / frameHeight, height / frameHeight);
    ctx.translate(-footX, -footY);
    if (room?.perspective === 'top-down' && !appearance) {
      ctx.translate(footX, footY);
      drawTopDownFigure(ctx, character);
    } else if (loaded) {
      if (flipX) {
        ctx.translate(footX, footY);
        ctx.scale(-1, 1);
        ctx.drawImage(
          sprite,
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
          sprite,
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
      ctx.fillRect(footX - frameWidth / 2, footY - frameHeight, frameWidth, frameHeight);
    }

    ctx.restore();

    if (showName) this.drawName(ctx, character, isLocal, room);
  }

  public drawName(ctx: CanvasRenderingContext2D, character: Character, isLocal: boolean, room?: RoomDefinition): void {
    const { name, position } = character;
    const footX = Math.round(position.x), footY = Math.round(position.y);
    const height = actorHeightAt(room, position.y);
    // Draw Name Tag
    ctx.save();
    ctx.font = '10px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'bottom';

    const textWidth = ctx.measureText(name).width;
    const badgeWidth = Math.max(textWidth + 10, 36);
    const badgeHeight = 14;
    const textY = room?.depthScale ? Math.max(badgeHeight - 2, footY - height - 4) : footY - height - 4;

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
