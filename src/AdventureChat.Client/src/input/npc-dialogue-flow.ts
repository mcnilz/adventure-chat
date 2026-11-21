import type { DialogueOption, NpcDialogueResult, NpcPresence, Point } from '../types.js';

interface Approach {
  readonly roomId: string;
  readonly npc: NpcPresence;
  ready: boolean;
  inFlight: boolean;
  nextAttempt: number;
  retries: number;
}

export class NpcDialogueFlow {
  private approach: Approach | null = null;

  public constructor(
    private readonly current: () => { roomId: string; position: Point; idle: boolean; changingRoom: boolean },
    private readonly walk: (point: Point) => Promise<{ accepted: boolean }>,
    private readonly request: (roomId: string, npcId: string) => Promise<NpcDialogueResult>,
    private readonly open: (npc: NpcPresence, options: readonly DialogueOption[]) => void,
    private readonly feedback: (text: string) => void
  ) {}

  public cancel(): boolean {
    const pending = this.approach !== null;
    this.approach = null;
    return pending;
  }

  public async start(npc: NpcPresence): Promise<void> {
    const current = this.current();
    if (current.changingRoom) {
      this.feedback('Das Gespräch ist hier nicht mehr verfügbar.');
      return;
    }
    const approach = { roomId: current.roomId, npc, ready: false, inFlight: false, nextAttempt: 0, retries: 0 };
    this.approach = approach;
    if (!current.idle || this.distance(npc, current.position) > 1) {
      const result = await this.walk(npc.talkPoint);
      if (this.approach !== approach) return;
      if (!result.accepted) {
        this.approach = null;
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
    if (current.roomId !== approach.roomId || this.distance(approach.npc, current.position) > 1) {
      this.approach = null;
      this.feedback('Das Gesprächsziel wurde nicht erreicht.');
      return;
    }
    void this.arrive(approach);
  }

  private async arrive(approach: Approach): Promise<void> {
    approach.inFlight = true;
    const result = await this.request(approach.roomId, approach.npc.id);
    if (this.approach !== approach) return;
    approach.inFlight = false;
    if (this.current().changingRoom) return;
    if (result.tooFar && approach.retries++ < 5) {
      approach.nextAttempt = performance.now() + 150;
      return;
    }
    this.approach = null;
    if (result.options) this.open(approach.npc, result.options);
    else this.feedback(result.error ?? 'Das Gespräch konnte nicht geöffnet werden.');
  }

  private distance(npc: NpcPresence, position: Point): number {
    return Math.hypot(npc.talkPoint.x - position.x, npc.talkPoint.y - position.y);
  }
}
