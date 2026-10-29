import { isInsideRect, moveTowards } from './math.js';
import { RoomRenderer } from './renderer.js';
import type { Character, Point, RoomDefinition } from './types.js';

export const DEFAULT_ROOM: RoomDefinition = {
  id: 'lounge',
  name: 'Lounge',
  width: 640,
  height: 360,
  backgroundUrl: 'assets/images/lounge.png',
  walkableFloor: {
    x: 40,
    y: 245,
    width: 560,
    height: 100,
  },
  spawnPoint: {
    x: 120,
    y: 300,
  },
};

export class AdventureGame {
  public readonly room: RoomDefinition;
  public readonly character: Character;
  public readonly renderer: RoomRenderer;
  private lastTimestamp = 0;
  private isRunning = false;
  private animationFrameId: number | null = null;

  public constructor(
    private readonly canvas: HTMLCanvasElement,
    room: RoomDefinition = DEFAULT_ROOM
  ) {
    this.room = room;
    this.character = {
      id: 'local-player',
      name: 'Player',
      position: { ...this.room.spawnPoint },
      target: null,
      state: 'idle',
      facing: 'right',
      speed: 130, // logical pixels per second
      walkFrame: 0,
      frameTimer: 0,
    };

    this.renderer = new RoomRenderer(this.canvas, this.room);
    this.setupEvents();
  }

  public start(): void {
    if (this.isRunning) return;
    this.isRunning = true;
    this.lastTimestamp = performance.now();
    this.loop = this.loop.bind(this);
    this.animationFrameId = requestAnimationFrame(this.loop);
  }

  public stop(): void {
    this.isRunning = false;
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
  }

  public screenToLogical(clientX: number, clientY: number): Point {
    const rect = this.canvas.getBoundingClientRect();
    const scaleX = this.room.width / rect.width;
    const scaleY = this.room.height / rect.height;
    return {
      x: (clientX - rect.left) * scaleX,
      y: (clientY - rect.top) * scaleY,
    };
  }

  public handleCanvasClick(clientX: number, clientY: number): { accepted: boolean; point: Point } {
    const logical = this.screenToLogical(clientX, clientY);

    if (isInsideRect(logical, this.room.walkableFloor)) {
      this.setTarget(logical);
      return { accepted: true, point: logical };
    }

    return { accepted: false, point: logical };
  }

  public setTarget(target: Point): void {
    this.character.target = { x: target.x, y: target.y };
    this.character.state = 'walk';
    if (target.x < this.character.position.x - 1) {
      this.character.facing = 'left';
    } else if (target.x > this.character.position.x + 1) {
      this.character.facing = 'right';
    }
  }

  public update(deltaSeconds: number): void {
    if (deltaSeconds <= 0) return;

    // Cap delta to prevent huge jumps on tab switch
    const dt = Math.min(deltaSeconds, 0.1);

    if (this.character.target && this.character.state === 'walk') {
      const maxDistance = this.character.speed * dt;
      const { position, reached } = moveTowards(
        this.character.position,
        this.character.target,
        maxDistance
      );

      this.character.position = position;

      if (reached) {
        this.character.target = null;
        this.character.state = 'idle';
        this.character.walkFrame = 0;
        this.character.frameTimer = 0;
      } else {
        // Update walking animation
        this.character.frameTimer += dt;
        if (this.character.frameTimer >= 0.12) {
          this.character.frameTimer = 0;
          this.character.walkFrame = (this.character.walkFrame + 1) % 4;
        }
      }
    } else {
      this.character.state = 'idle';
      this.character.walkFrame = 0;
    }
  }

  private loop(timestamp: number): void {
    if (!this.isRunning) return;

    const deltaSeconds = (timestamp - this.lastTimestamp) / 1000;
    this.lastTimestamp = timestamp;

    this.update(deltaSeconds);
    this.renderer.render(this.character);

    this.animationFrameId = requestAnimationFrame(this.loop);
  }

  private setupEvents(): void {
    this.canvas.addEventListener('click', (e: MouseEvent) => {
      this.handleCanvasClick(e.clientX, e.clientY);
    });
  }
}
