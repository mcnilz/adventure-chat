import { screenToRoom } from '../rendering/camera.js';
import type { InteractionVerb, Point, RoomDefinition, RoomExit, RoomObject } from '../types.js';
import { actionsForVerb, VERB_PHRASES } from './object-action.js';
import { exitAt, withinExitReach } from './exit-hit.js';

export interface InputHandlerCallbacks {
  onHoverExitChanged: (exit: RoomExit | null, actionText: string) => void;
  onNavigateToExit: (exit: RoomExit, isImmediate: boolean) => Promise<void>;
  onNavigateToPoint: (point: Point) => Promise<void>;
  onToggleDebug: () => void;
  onObjectAction: (object: RoomObject) => Promise<void>;
  onNavigateToObject: (object: RoomObject) => Promise<void>;
  onUnavailableTarget: () => void;
  onCanvasAction: (objectId: string | undefined) => void;
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
    private getObjectAt: (point: Point) => RoomObject | undefined = () => undefined,
    private getObjectState: (objectId: string) => string | undefined = () => undefined
  ) {
    this.setupListeners();
  }

  public screenToLogical(clientX: number, clientY: number): Point {
    const room = this.getRoom();
    const rect = this.canvas.getBoundingClientRect();
    return screenToRoom(clientX, clientY, rect, room, this.getCameraOffset());
  }

  public findClickedExit(point: Point): RoomExit | null {
    return exitAt(this.getRoom().exits ?? [], point) ?? null;
  }

  public handleMouseMove(clientX: number, clientY: number): void {
    this.pointer = { x: clientX, y: clientY };
    const logical = this.screenToLogical(clientX, clientY);
    const verb = this.getVerb();
    const phrase = VERB_PHRASES[verb];
    const exit = this.findClickedExit(logical);
    const object = verb === 'walk' && exit ? undefined : this.getObjectAt(logical);
    if (object) {
      const available = verb === 'walk' || actionsForVerb(object, this.getObjectState(object.id), verb).length > 0;
      this.canvas.style.cursor = 'pointer';
      this.callbacks.onHoverExitChanged(null, `${phrase} ${object.interaction?.name ?? object.id}${available ? '' : ' (derzeit nicht möglich)'}`);
      return;
    }

    if (exit) {
      this.canvas.style.cursor = 'pointer';
      const label = exit.label || exit.targetRoomId;
      this.callbacks.onHoverExitChanged(verb === 'walk' ? exit : null, `${phrase} ${label}${verb === 'walk' ? '' : ' (derzeit nicht möglich)'}`);
    } else {
      this.canvas.style.cursor = 'default';
      this.callbacks.onHoverExitChanged(null, phrase);
    }
  }

  public handleMouseLeave(): void {
    this.pointer = null;
    this.canvas.style.cursor = 'default';
    this.callbacks.onHoverExitChanged(null, VERB_PHRASES[this.getVerb()]);
  }

  public refreshHover(): void {
    if (this.pointer) this.handleMouseMove(this.pointer.x, this.pointer.y);
  }

  public updateVerb(): void {
    if (this.pointer) this.refreshHover();
    else this.callbacks.onHoverExitChanged(null, VERB_PHRASES[this.getVerb()]);
  }

  public async handleCanvasClick(clientX: number, clientY: number): Promise<{ point: Point; exit: RoomExit | null }> {
    const logical = this.screenToLogical(clientX, clientY);
    const clickedExit = this.findClickedExit(logical);
    const object = this.getVerb() === 'walk' && clickedExit ? undefined : this.getObjectAt(logical);
    this.callbacks.onCanvasAction(object?.id);
    if (object) {
      if (this.getVerb() === 'walk' && !object.interaction && !object.usePoints?.length)
        await this.callbacks.onNavigateToPoint(logical);
      else if (this.getVerb() === 'walk') await this.callbacks.onNavigateToObject(object);
      else await this.callbacks.onObjectAction(object);
      return { point: logical, exit: null };
    }
    if (this.getVerb() !== 'walk') {
      if (clickedExit) this.callbacks.onUnavailableTarget();
      return { point: logical, exit: null };
    }
    if (clickedExit) {
      const isImmediate = withinExitReach(clickedExit, this.getPlayerPosition());
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
