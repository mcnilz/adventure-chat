import { EditorApi, type PreviewLocation, type PreviewNpcPatrols } from '../core/editor-api.js';
import type { DialogueOption, NpcDialogueResult, NpcPresence, Point } from '../../types.js';

export class PreviewNpcPlayback {
  private state: PreviewNpcPatrols | undefined;
  private npcs: readonly NpcPresence[] = [];
  private sampledAt = 0;
  private nextUpdate = Infinity;
  private pendingRefresh: object | undefined;
  private approachId: string | undefined;
  private speed = 130;

  public constructor(
    private readonly location: () => PreviewLocation,
    private readonly update: (npcs: readonly NpcPresence[]) => void,
    private readonly feedback: (message: string) => void
  ) {}

  public get checkpoint(): PreviewNpcPatrols | undefined {
    return this.state && { ...this.state, elapsedMs: Math.round(performance.now() - this.sampledAt) };
  }

  public reset(): void {
    this.state = undefined;
    this.npcs = [];
    this.pendingRefresh = undefined;
    this.approachId = undefined;
    this.nextUpdate = Infinity;
  }

  public apply(patrols: PreviewNpcPatrols | null | undefined, npcs: readonly NpcPresence[], speed = this.speed): void {
    this.pendingRefresh = undefined;
    this.state = patrols ?? undefined;
    this.npcs = npcs;
    this.speed = speed;
    this.sampledAt = performance.now();
    this.schedule();
    this.update(npcs);
  }

  private schedule(): void {
    const checkpoints = this.state?.checkpoints ?? [];
    const remaining = checkpoints
      .filter(checkpoint => !checkpoint.blocked && checkpoint.id !== this.state?.pausedNpc)
      .map(checkpoint => checkpoint.waitMs ||
        this.duration(this.npcs.find(npc => npc.id === checkpoint.id)?.motion?.route ?? []));
    this.nextUpdate = this.sampledAt + Math.max(60, Math.min(...remaining));
  }

  private duration(route: readonly Point[]): number {
    let distance = 0;
    for (let index = 1; index < route.length; index++) {
      const from = route[index - 1]!;
      const to = route[index]!;
      distance += Math.hypot(to.x - from.x, to.y - from.y);
    }
    return distance / this.speed * 1000;
  }

  public tick(now: number): void {
    if (now < this.nextUpdate || this.pendingRefresh) return;
    const request = {};
    this.pendingRefresh = request;
    void EditorApi.previewSnapshot(this.location()).then(result => {
      if (this.pendingRefresh === request)
        this.apply(result.patrols, result.snapshot.room?.npcs ?? []);
    }).catch(error => {
      if (this.pendingRefresh !== request) return;
      this.feedback(error.message);
      this.nextUpdate = performance.now() + 1000;
    }).finally(() => {
      if (this.pendingRefresh === request) this.pendingRefresh = undefined;
    });
  }

  public async dialogue(npcId: string, choice?: DialogueOption, approachId?: string, prepare = false): Promise<NpcDialogueResult> {
    if (prepare) this.approachId = approachId;
    const result = await EditorApi.previewDialogue(this.location(), npcId, choice, prepare);
    if (prepare && this.approachId === approachId && result.dialogue.talkPoint)
      this.apply(result.patrols, result.npcs ?? []);
    return result.dialogue;
  }

  public release(id: string): void {
    if (this.approachId !== id) return;
    this.approachId = undefined;
    this.pendingRefresh = undefined;
    const now = performance.now();
    if (this.state?.pausedNpc) {
      this.state = { ...this.state, resumeAfterMs: Math.round(now - this.sampledAt) };
      this.nextUpdate = now;
    }
  }
}
