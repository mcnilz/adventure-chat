import type { DialogueOption } from '../types.js';

export class NpcDialoguePanel {
  private readonly panel = document.querySelector<HTMLDialogElement>('#npc-dialogue')!;
  private readonly name = document.getElementById('npc-dialogue-name')!;
  private readonly choices = document.getElementById('npc-choices')!;
  private readonly reply = document.getElementById('npc-reply')!;
  private readonly talk = document.querySelector<HTMLButtonElement>('#verb-bar button[data-verb="talk"]')!;

  public constructor() {
    document.getElementById('npc-dialogue-close')!.addEventListener('click', () => this.dismiss());
    this.panel.addEventListener('keydown', event => {
      if (event.key !== 'Escape') return;
      event.preventDefault();
      this.dismiss();
    });
  }

  public open(name: string, options: readonly DialogueOption[]): void {
    this.name.textContent = name;
    this.reply.textContent = '';
    this.choices.replaceChildren();
    for (const option of options) {
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = option.text;
      button.addEventListener('click', () => { this.reply.textContent = option.reply; });
      this.choices.append(button);
    }
    this.panel.show();
    this.choices.querySelector('button')?.focus();
  }

  public close(): void {
    if (!this.panel.open) return;
    this.panel.close();
    this.choices.replaceChildren();
    this.reply.textContent = '';
  }

  private dismiss(): void {
    this.close();
    this.talk.focus();
  }
}
