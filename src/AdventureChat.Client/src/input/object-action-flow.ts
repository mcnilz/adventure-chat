import type { ObjectActionResultDto, Point, RoomObject } from '../types.js';

interface PendingAction {
  readonly roomId: string;
  readonly objectId: string;
  readonly actionId: string;
  readonly expectedState: string | undefined;
  readonly point: Point;
  readonly radius: number;
  ready: boolean;
  inFlight: boolean;
  retries: number;
  nextAttempt: number;
  approached: boolean;
}

export class ObjectActionFlow {
  private pending: PendingAction | null = null;

  public constructor(
    private readonly current: () => { roomId: string; position: Point; idle: boolean; revision: number; stateOf: (objectId: string) => string | undefined },
    private readonly walk: (point: Point) => Promise<{ accepted: boolean; error?: string }>,
    private readonly execute: (roomId: string, objectId: string, actionId: string, revision: number) => Promise<ObjectActionResultDto>,
    private readonly finish: (error: string | null) => void
  ) {}

  public cancel(): void { this.pending = null; }

  public async start(roomId: string, object: RoomObject, actionId: string, expectedState: string | undefined): Promise<void> {
    const interaction = object.interaction;
    if (!interaction) return;
    const intent: PendingAction = { roomId, objectId: object.id, actionId, expectedState,
      point: interaction.usePoint, radius: interaction.useRadius, ready: false, inFlight: false, retries: 0, nextAttempt: 0, approached: false };
    this.pending = intent;
    const current = this.current(), position = current.position;
    if (current.idle && Math.hypot(position.x - intent.point.x, position.y - intent.point.y) <= intent.radius) {
      await this.tryExecute(intent);
      return;
    }
    await this.approach(intent);
  }

  private async approach(intent: PendingAction): Promise<void> {
    intent.approached = true;
    const result = await this.walk(intent.point);
    if (this.pending !== intent) return;
    if (!result.accepted) { this.pending = null; this.finish(result.error ?? 'Der Weg zum Objekt ist nicht erreichbar.'); return; }
    intent.ready = true;
  }

  public update(): void {
    const intent = this.pending;
    if (!intent || !intent.ready || intent.inFlight || performance.now() < intent.nextAttempt) return;
    const current = this.current();
    if (current.roomId !== intent.roomId) { this.cancel(); return; }
    if (!current.idle || Math.hypot(current.position.x - intent.point.x, current.position.y - intent.point.y) > intent.radius) return;
    void this.tryExecute(intent);
  }

  private async tryExecute(intent: PendingAction): Promise<void> {
    const current = this.current();
    if (current.roomId !== intent.roomId) { this.cancel(); return; }
    if (current.stateOf(intent.objectId) !== intent.expectedState) {
      this.pending = null; this.finish('Der Objektzustand hat sich geändert. Bitte erneut versuchen.'); return;
    }
    intent.inFlight = true;
    const result = await this.execute(intent.roomId, intent.objectId, intent.actionId, current.revision);
    if (this.pending !== intent) return;
    if (!result.success && result.errorCode === 'too_far' && !intent.approached) {
      intent.inFlight = false;
      await this.approach(intent);
      return;
    }
    if (!result.success && result.errorCode === 'too_far' && intent.retries++ < 5) {
      intent.inFlight = false; intent.ready = true; intent.nextAttempt = performance.now() + 150;
      return;
    }
    this.pending = null;
    this.finish(result.success ? null : result.error ?? 'Aktion abgelehnt.');
  }
}
