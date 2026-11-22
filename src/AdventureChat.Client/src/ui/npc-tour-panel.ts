import type { NpcTourStatus } from '../types.js';

export class NpcTourPanel {
  private readonly panel = document.getElementById('npc-tour')!;
  private readonly message = document.getElementById('npc-tour-message')!;
  private readonly stop = document.querySelector<HTMLButtonElement>('#npc-tour-stop')!;
  private readonly verbs = document.getElementById('verb-bar')!;

  public constructor(cancel: () => Promise<boolean>, private readonly feedback: (message: string) => void) {
    this.stop.addEventListener('click', async () => {
      this.stop.disabled = true;
      if (await cancel() || this.panel.hidden) return;
      this.stop.disabled = false;
      this.message.textContent = 'Die Führung konnte nicht beendet werden. Versuche es erneut.';
    });
  }

  public get active(): boolean { return !this.panel.hidden; }

  public show({ active, message }: NpcTourStatus): void {
    const wasActive = this.active;
    this.panel.hidden = !active;
    this.verbs.hidden = active;
    this.message.textContent = active ? message : '';
    if (active && !wasActive) {
      this.stop.disabled = false;
      this.stop.focus();
    } else if (!active && wasActive) {
      this.feedback(message);
      if (document.activeElement === this.stop || document.activeElement === document.body)
        this.verbs.querySelector<HTMLButtonElement>('[data-verb="walk"]')?.focus();
    }
  }
}
