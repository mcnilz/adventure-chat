import type { DialogueOption, NpcDialogueResult, NpcPresence, Point } from '../types.js';

interface Approach {
  readonly roomId: string;
  readonly npc: NpcPresence;
  readonly id: string | undefined;
  talkPoint: Point;
  ready: boolean;
  inFlight: boolean;
  nextAttempt: number;
  retries: number;
}

type RequestDialogue = (roomId: string, npcId: string, choice?: DialogueOption,
  approachId?: string, prepare?: boolean) => Promise<NpcDialogueResult>;
export type ChooseDialogueOption = (option: DialogueOption) => Promise<NpcDialogueResult>;

export class NpcDialogueFlow {
  private approach: Approach | null = null;
  private sequence = 0;
  private conversationId: string | undefined;

  public constructor(
    private readonly current: () => { roomId: string; position: Point; idle: boolean; changingRoom: boolean },
    private readonly walk: (point: Point) => Promise<{ accepted: boolean }>,
    private readonly request: RequestDialogue,
    private readonly open: (npc: NpcPresence, options: readonly DialogueOption[], choose: ChooseDialogueOption) => void,
    private readonly feedback: (text: string) => void,
    private readonly release: (id: string) => void
  ) {}

  public cancel(): boolean {
    const pending = this.approach !== null;
    const id = this.approach?.id ?? this.conversationId;
    this.approach = null;
    this.conversationId = undefined;
    if (id) this.release(id);
    return pending;
  }

  public close(): void {
    if (this.conversationId) this.release(this.conversationId);
    this.conversationId = undefined;
  }

  public async start(npc: NpcPresence): Promise<void> {
    this.cancel();
    const current = this.current();
    if (current.changingRoom) {
      this.feedback('Das Gespräch ist hier nicht mehr verfügbar.');
      return;
    }
    const id = npc.motion ? String(++this.sequence) : undefined;
    const approach: Approach = { roomId: current.roomId, npc, id, talkPoint: npc.talkPoint,
      ready: false, inFlight: false, nextAttempt: 0, retries: 0 };
    this.approach = approach;
    if (id) {
      const result = await this.request(approach.roomId, npc.id, undefined, id, true);
      if (this.approach !== approach) {
        this.release(id);
        return;
      }
      if (!result.talkPoint) {
        this.cancel();
        this.feedback(result.error ?? 'Der Weg zur Figur ist nicht erreichbar.');
        return;
      }
      approach.talkPoint = result.talkPoint;
    }
    const player = this.current();
    if (!player.idle || this.distance(approach, player.position) > 1) {
      const result = await this.walk(approach.talkPoint);
      if (this.approach !== approach) return;
      if (!result.accepted) {
        this.cancel();
        this.feedback('Der Weg zur Figur ist nicht erreichbar.');
        return;
      }
    }
    approach.ready = true;
    this.update();
  }

  public update(): void {
    const approach = this.approach;
    if (!approach?.ready || approach.inFlight || performance.now() < approach.nextAttempt) return;
    const current = this.current();
    if (current.changingRoom || !current.idle) return;
    if (current.roomId !== approach.roomId || this.distance(approach, current.position) > 1) {
      this.cancel();
      this.feedback('Das Gesprächsziel wurde nicht erreicht.');
      return;
    }
    void this.arrive(approach);
  }

  private async arrive(approach: Approach): Promise<void> {
    approach.inFlight = true;
    const result = await this.request(approach.roomId, approach.npc.id, undefined, approach.id);
    if (this.approach !== approach) return;
    approach.inFlight = false;
    if (this.current().changingRoom) return;
    if (result.tooFar && approach.retries++ < 5) {
      approach.nextAttempt = performance.now() + 150;
      return;
    }
    if (result.options) {
      this.approach = null;
      this.open(approach.npc, result.options,
        option => this.request(approach.roomId, approach.npc.id, option, approach.id));
      this.conversationId = approach.id;
    } else {
      this.cancel();
      this.feedback(result.error ?? 'Das Gespräch konnte nicht geöffnet werden.');
    }
  }

  private distance(approach: Approach, position: Point): number {
    return Math.hypot(approach.talkPoint.x - position.x, approach.talkPoint.y - position.y);
  }
}
