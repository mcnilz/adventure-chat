import { moveTowards } from './math.js';
import { RoomRenderer } from './renderer.js';
import type { Character, Point, RoomDefinition } from './types.js';

export const DEFAULT_ROOM: RoomDefinition = {
  id: 'lounge',
  name: 'Lounge',
  width: 640,
  height: 360,
  backgroundUrl: 'assets/images/lounge.png',
  spawnPoint: {
    x: 120,
    y: 300,
  },
  geometry: {
    outerBoundary: [
      { x: 40, y: 240 },
      { x: 300, y: 220 },
      { x: 600, y: 240 },
      { x: 600, y: 345 },
      { x: 40, y: 345 },
    ],
    obstacles: [
      [
        { x: 250, y: 255 },
        { x: 390, y: 255 },
        { x: 390, y: 310 },
        { x: 250, y: 310 },
      ],
    ],
  },
};

export class AdventureGame {
  public readonly room: RoomDefinition;
  public readonly character: Character;
  public readonly renderer: RoomRenderer;
  private lastTimestamp = 0;
  private isRunning = false;
  private animationFrameId: number | null = null;
  private currentRequestId = 0;

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
      waypoints: [],
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

  public async handleCanvasClick(
    clientX: number,
    clientY: number
  ): Promise<{ accepted: boolean; point: Point; waypoints?: Point[] }> {
    const logical = this.screenToLogical(clientX, clientY);
    const result = await this.setTarget(logical);
    if (result.waypoints) {
      return { accepted: result.accepted, point: logical, waypoints: result.waypoints };
    }
    return { accepted: result.accepted, point: logical };
  }

  public async setTarget(
    target: Point
  ): Promise<{ accepted: boolean; waypoints?: Point[]; error?: string }> {
    const requestId = ++this.currentRequestId;
    const startPos = { ...this.character.position };

    try {
      const response = await fetch(`/api/rooms/${this.room.id}/path`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          start: startPos,
          target: { x: target.x, y: target.y },
        }),
      });

      if (!response.ok) {
        const errData = await response.json().catch(() => ({}));
        return { accepted: false, error: errData.error || 'Server rejected target' };
      }

      const data = await response.json();
      if (!data.success || !Array.isArray(data.waypoints) || data.waypoints.length === 0) {
        return { accepted: false, error: data.error || 'No path found' };
      }

      // Ignore stale request if a newer target was initiated
      if (requestId !== this.currentRequestId) {
        return { accepted: false, error: 'Superseded by newer target' };
      }

      const waypoints: Point[] = data.waypoints;
      this.applyPath(waypoints);
      return { accepted: true, waypoints };
    } catch (e) {
      return { accepted: false, error: String(e) };
    }
  }

  public applyPath(waypoints: Point[]): void {
    if (waypoints.length <= 1) {
      this.character.waypoints = [];
      this.character.target = null;
      this.character.state = 'idle';
      return;
    }

    // Skip the first waypoint (current position) and queue remaining waypoints
    this.character.waypoints = waypoints.slice(1);
    this.moveToNextWaypoint();
  }

  private moveToNextWaypoint(): void {
    if (this.character.waypoints.length === 0) {
      this.character.target = null;
      this.character.state = 'idle';
      this.character.walkFrame = 0;
      this.character.frameTimer = 0;
      return;
    }

    const nextTarget = this.character.waypoints.shift()!;
    this.character.target = nextTarget;
    this.character.state = 'walk';

    const dx = nextTarget.x - this.character.position.x;
    const dy = nextTarget.y - this.character.position.y;

    if (Math.abs(dx) >= Math.abs(dy)) {
      if (dx < -1) {
        this.character.facing = 'left';
      } else if (dx > 1) {
        this.character.facing = 'right';
      }
    } else {
      if (dy < -1) {
        this.character.facing = 'up';
      } else if (dy > 1) {
        this.character.facing = 'down';
      }
    }
  }

  public update(deltaSeconds: number): void {
    if (deltaSeconds <= 0) return;

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
        if (this.character.waypoints.length > 0) {
          this.moveToNextWaypoint();
        } else {
          this.character.target = null;
          this.character.state = 'idle';
          this.character.walkFrame = 0;
          this.character.frameTimer = 0;
        }
      } else {
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
