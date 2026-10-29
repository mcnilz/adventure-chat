import type { Character, RoomDefinition } from './types.js';

export class RoomRenderer {
  private readonly ctx: CanvasRenderingContext2D;
  private readonly backgroundImage: HTMLImageElement;
  private readonly characterSprite: HTMLImageElement;
  private backgroundLoaded = false;
  private spriteLoaded = false;

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

  public render(character: Character): void {
    const { ctx, canvas, room } = this;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // 1. Draw Background
    if (this.backgroundLoaded) {
      ctx.drawImage(this.backgroundImage, 0, 0, room.width, room.height);
    } else {
      ctx.fillStyle = '#2d3748';
      ctx.fillRect(0, 0, room.width, room.height);
    }

    // 2. Target indicator (if moving)
    if (character.target && character.state === 'walk') {
      this.drawTargetMarker(character.target.x, character.target.y);
    }

    // 3. Draw Character
    this.drawCharacter(character);
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

  private drawCharacter(character: Character): void {
    const { ctx } = this;
    const { position, facing, state, walkFrame, name } = character;

    const frameWidth = 32;
    const frameHeight = 48;
    const footX = Math.round(position.x);
    const footY = Math.round(position.y);

    // Frame calculation
    let frameIndex = 0;
    if (state === 'walk') {
      // frames 0, 1, 2, 3 cycle
      frameIndex = walkFrame % 4;
    }

    ctx.save();

    if (this.spriteLoaded) {
      if (facing === 'left') {
        ctx.translate(footX, footY);
        ctx.scale(-1, 1);
        ctx.drawImage(
          this.characterSprite,
          frameIndex * frameWidth,
          0,
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
          0,
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
      ctx.fillStyle = '#3182ce';
      ctx.fillRect(footX - 12, footY - 44, 24, 44);
    }

    ctx.restore();

    // Draw Name Tag
    ctx.save();
    ctx.font = '10px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'bottom';

    const textY = footY - frameHeight - 4;
    ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
    ctx.fillRect(footX - 24, textY - 11, 48, 13);

    ctx.fillStyle = '#ffffff';
    ctx.fillText(name, footX, textY);
    ctx.restore();
  }
}
