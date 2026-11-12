import { screenToRoom } from '../rendering/camera.js';
import { EXIT_TRIGGER_DISTANCE } from '../constants.js';
import type { InteractionVerb, Point, RoomDefinition, RoomExit, RoomObject } from '../types.js';

const VERB_LABELS: Record<InteractionVerb, string> = { walk: 'Gehe zu', pull: 'Ziehen', push: 'Drücken', use: 'Benutzen' };

export interface InputHandlerCallbacks {
  onHoverExitChanged: (exit: RoomExit | null, actionText: string) => void;
  onNavigateToExit: (exit: RoomExit, isImmediate: boolean) => Promise<void>;
  onNavigateToPoint: (point: Point) => Promise<void>;
  onToggleDebug: () => void;
  onObjectAction: (object: RoomObject) => Promise<void>;
  onNavigateToObject: (object: RoomObject) => Promise<void>;
  onUnavailableTarget: () => void;
  onCanvasAction: () => void;
}

export class InputHandler {
  private pointer: Point | null = null;
  public constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly callbacks: InputHandlerCallbacks,
    private getRoom: () => RoomDefinition,
    private getPlayerPosition: () => Point,
    private getCameraOffset: () => number = () => 0,
    private getVerb: () => InteractionVerb = () => 'walk',
    private getObjectAt: (point: Point) => RoomObject | undefined = () => undefined
  ) {
    this.setupListeners();
  }

  public screenToLogical(clientX: number, clientY: number): Point {
    const room = this.getRoom();
    const rect = this.canvas.getBoundingClientRect();
    return screenToRoom(clientX, clientY, rect, room, this.getCameraOffset());
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
    this.pointer = { x: clientX, y: clientY };
    const logical = this.screenToLogical(clientX, clientY);
    const hit = this.getObjectAt(logical);
    const object = hit;
    const verb = VERB_LABELS[this.getVerb()];
    if (object) {
      this.canvas.style.cursor = 'pointer';
      this.callbacks.onHoverExitChanged(null, `${verb} ${object.interaction?.name ?? object.id}`);
      return;
    }
    const exit = hit ? null : this.findClickedExit(logical);

    if (exit) {
      this.canvas.style.cursor = 'pointer';
      const label = exit.label || exit.targetRoomId;
      this.callbacks.onHoverExitChanged(this.getVerb() === 'walk' ? exit : null, `${verb} ${label}`);
    } else {
      this.canvas.style.cursor = 'default';
      this.callbacks.onHoverExitChanged(null, verb);
    }
  }

  public handleMouseLeave(): void {
    this.pointer = null;
    this.canvas.style.cursor = 'default';
    this.callbacks.onHoverExitChanged(null, VERB_LABELS[this.getVerb()]);
  }

  public refreshHover(): void {
    if (this.pointer) this.handleMouseMove(this.pointer.x, this.pointer.y);
  }

  public updateVerb(): void {
    if (this.pointer) this.refreshHover();
    else this.callbacks.onHoverExitChanged(null, VERB_LABELS[this.getVerb()]);
  }

  public async handleCanvasClick(clientX: number, clientY: number): Promise<{ point: Point; exit: RoomExit | null }> {
    this.callbacks.onCanvasAction();
    const logical = this.screenToLogical(clientX, clientY);
    const hit = this.getObjectAt(logical);
    const object = hit;
    if (object) {
      if (this.getVerb() === 'walk' && !object.interaction && !object.usePoints?.length)
        await this.callbacks.onNavigateToPoint(logical);
      else if (this.getVerb() === 'walk') await this.callbacks.onNavigateToObject(object);
      else await this.callbacks.onObjectAction(object);
      return { point: logical, exit: null };
    }
    if (this.getVerb() !== 'walk') {
      if (hit || this.findClickedExit(logical)) this.callbacks.onUnavailableTarget();
      return { point: logical, exit: null };
    }
    const clickedExit = hit ? null : this.findClickedExit(logical);

    if (clickedExit) {
      const playerPos = this.getPlayerPosition();
      const currentDist = Math.hypot(
        playerPos.x - clickedExit.position.x,
        playerPos.y - clickedExit.position.y
      );
      const isImmediate = currentDist <= (clickedExit.radius ?? EXIT_TRIGGER_DISTANCE) + 5;
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
