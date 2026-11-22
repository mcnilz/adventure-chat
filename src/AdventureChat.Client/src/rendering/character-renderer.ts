import { actorHeightAt } from './depth-scale.js';
import { drawTopDownFigure } from './top-down-figure.js';
import type { Character, RoomDefinition, SpeechGesture } from '../types.js';

interface SpeechAnimation {
  readonly elapsed: number;
  readonly gesture: SpeechGesture | null;
}

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

  public preload(url: string): void { this.image(url); }

  public async prepare(url: string): Promise<void> {
    try { await this.image(url).decode(); }
    catch { /* A failed image still has the standing-frame fallback. */ }
  }

  private image(url: string): HTMLImageElement {
    let image = this.images.get(url);
    if (!image) {
      image = new Image();
      image.src = url;
      this.images.set(url, image);
    }
    return image;
  }

  private drawActorAnimation(ctx: CanvasRenderingContext2D, character: Character, x: number, y: number, height: number): boolean {
    const animation = character.actorAnimation;
    if (!animation?.image) return false;
    const image = this.image(animation.image);
    if (!image.complete || !image.naturalWidth) return false;
    const frameSize = image.naturalHeight;
    const elapsed = performance.now() - animation.startedAt;
    const frame = animation.durationMs ? Math.min(2, Math.floor(3 * elapsed / animation.durationMs)) : 0;
    ctx.drawImage(image, frame * frameSize, 0, frameSize, frameSize, x - height / 2, y - height, height, height);
    return true;
  }

  public drawCharacter(
    ctx: CanvasRenderingContext2D, character: Character, isLocal: boolean, room?: RoomDefinition, showName = true,
    speech?: SpeechAnimation
  ): void {
    const { appearance, position, facing, state, walkFrame } = character;
    const topDown = room?.perspective === 'top-down';
    const usesTopDownSheet = topDown && !!appearance?.topDownImageUrl;
    const spriteUrl = usesTopDownSheet ? appearance?.topDownImageUrl : appearance?.imageUrl;
    const sprite = spriteUrl ? this.image(spriteUrl) : this.characterSprite;
    const loaded = spriteUrl ? sprite.complete && sprite.naturalWidth > 0 : this.spriteLoaded;
    const frameWidth = appearance?.frameWidth ?? 48;
    const frameHeight = appearance?.frameHeight ?? 64;
    const footX = Math.round(position.x);
    const footY = Math.round(position.y);
    const height = actorHeightAt(room, position.y);

    if (!this.drawActorAnimation(ctx, character, footX, footY, height)) {
      ctx.save();
      ctx.translate(footX, footY);
      const scale = height / frameHeight;
      ctx.scale(scale, scale);

      if (topDown && !appearance) {
        drawTopDownFigure(ctx, character);
      } else if (loaded) {
        if (facing === 'left') ctx.scale(-1, 1);
        const row = facing === 'up' ? 1 : facing === 'down' ? 2 : 0;
        const frame = state === 'walk' ? walkFrame % 4 : 0;
        const drawFrame = (image: HTMLImageElement, column: number): void => {
          ctx.drawImage(image, column * frameWidth, row * frameHeight, frameWidth, frameHeight,
            -frameWidth / 2, -frameHeight, frameWidth, frameHeight);
        };

        const wave = usesTopDownSheet ? null : this.readyImage(appearance?.waveImageUrl);
        const mouth = this.readyImage(usesTopDownSheet ? appearance?.topDownSpeechImageUrl : appearance?.speechImageUrl);
        if (wave && speech?.gesture === 'wave' && state === 'idle' && speech.elapsed < 840) {
          drawFrame(wave, speech.elapsed >= 210 && speech.elapsed < 630 ? 2 : 1);
        } else {
          drawFrame(sprite, frame);
        }
        if (mouth && facing !== 'up' && speech && Math.floor(speech.elapsed / 135) % 2 === 0) {
          drawFrame(mouth, frame);
        }
      } else {
        ctx.fillStyle = isLocal ? '#3182ce' : '#38a169';
        ctx.fillRect(-frameWidth / 2, -frameHeight, frameWidth, frameHeight);
      }
      ctx.restore();
    }

    if (showName) this.drawName(ctx, character, isLocal, room);
  }

  private readyImage(url?: string | null): HTMLImageElement | null {
    if (!url) return null;
    const image = this.image(url);
    return image.complete && image.naturalWidth > 0 ? image : null;
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
