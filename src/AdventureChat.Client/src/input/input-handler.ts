import { EXIT_TRIGGER_DISTANCE } from '../constants.js';
import type { Point, RoomDefinition, RoomExit } from '../types.js';

export interface InputHandlerCallbacks {
  onHoverExitChanged: (exit: RoomExit | null, actionText: string) => void;
  onNavigateToExit: (exit: RoomExit, isImmediate: boolean) => Promise<void>;
  onNavigateToPoint: (point: Point) => Promise<void>;
  onToggleDebug: () => void;
}

export class InputHandler {
  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly callbacks: InputHandlerCallbacks,
    private getRoom: () => RoomDefinition,
    private getPlayerPosition: () => Point
  ) {
    this.setupListeners();
  }

  public screenToLogical(clientX: number, clientY: number): Point {
    const room = this.getRoom();
    const rect = this.canvas.getBoundingClientRect();
    const scaleX = room.width / rect.width;
    const scaleY = room.height / rect.height;
    return {
      x: (clientX - rect.left) * scaleX,
      y: (clientY - rect.top) * scaleY,
    };
  }

  public findClickedExit(point: Point): RoomExit | null {
    const room = this.getRoom();
    if (!room.exits || room.exits.length === 0) return null;

    for (const exit of room.exits) {
      if (exit.bounds) {
        if (
          point.x >= exit.bounds.x &&
          point.x <= exit.bounds.x + exit.bounds.width &&
          point.y >= exit.bounds.y &&
          point.y <= exit.bounds.y + exit.bounds.height
        ) {
          return exit;
        }
      }
      const dist = Math.hypot(point.x - exit.position.x, point.y - exit.position.y);
      if (dist <= (exit.radius || EXIT_TRIGGER_DISTANCE)) {
        return exit;
      }
    }
    return null;
  }

  public handleMouseMove(clientX: number, clientY: number): void {
    const logical = this.screenToLogical(clientX, clientY);
    const exit = this.findClickedExit(logical);

    if (exit) {
      this.canvas.style.cursor = 'pointer';
      const label = exit.label || exit.targetRoomId;
      this.callbacks.onHoverExitChanged(exit, `Go to ${label}`);
    } else {
      this.canvas.style.cursor = 'default';
      this.callbacks.onHoverExitChanged(null, 'Walk to');
    }
  }

  public handleMouseLeave(): void {
    this.canvas.style.cursor = 'default';
    this.callbacks.onHoverExitChanged(null, '');
  }

  public async handleCanvasClick(clientX: number, clientY: number): Promise<{ point: Point; exit: RoomExit | null }> {
    const logical = this.screenToLogical(clientX, clientY);
    const clickedExit = this.findClickedExit(logical);

    if (clickedExit) {
      const playerPos = this.getPlayerPosition();
      const currentDist = Math.hypot(
        playerPos.x - clickedExit.position.x,
        playerPos.y - clickedExit.position.y
      );
      const isImmediate = currentDist <= EXIT_TRIGGER_DISTANCE;
      await this.callbacks.onNavigateToExit(clickedExit, isImmediate);
      return { point: logical, exit: clickedExit };
    } else {
      await this.callbacks.onNavigateToPoint(logical);
      return { point: logical, exit: null };
    }
  }

  private setupListeners(): void {
    this.canvas.addEventListener('mousemove', (e: MouseEvent) => {
      this.handleMouseMove(e.clientX, e.clientY);
    });

    this.canvas.addEventListener('mouseleave', () => {
      this.handleMouseLeave();
    });

    this.canvas.addEventListener('click', (e: MouseEvent) => {
      void this.handleCanvasClick(e.clientX, e.clientY);
    });

    window.addEventListener('keydown', (e: KeyboardEvent) => {
      if (e.key === 'd' || e.key === 'D') {
        this.callbacks.onToggleDebug();
      }
    });
  }
}
