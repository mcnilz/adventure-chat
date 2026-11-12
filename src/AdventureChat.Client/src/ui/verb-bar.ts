import type { InteractionVerb } from '../types.js';

export class VerbBar {
  private readonly buttons = Array.from(document.querySelectorAll<HTMLButtonElement>('#verb-bar button[data-verb]'));
  private readonly choices = document.getElementById('action-choices')!;

  public constructor(onSelect: (verb: InteractionVerb) => void) {
    for (const button of this.buttons) button.addEventListener('click', () => {
      this.setSelected(button.dataset.verb as InteractionVerb);
      this.clearChoices();
      onSelect(button.dataset.verb as InteractionVerb);
    });
  }

  public setSelected(verb: InteractionVerb): void {
    for (const button of this.buttons) button.setAttribute('aria-pressed', String(button.dataset.verb === verb));
  }

  public clearChoices(): void { this.choices.replaceChildren(); this.choices.hidden = true; }

  public showChoices(actions: ReadonlyArray<{ readonly id: string; readonly name: string }>, choose: (id: string) => void): void {
    this.choices.replaceChildren(...actions.map(action => {
      const button = document.createElement('button');
      button.type = 'button'; button.textContent = action.name;
      button.addEventListener('click', () => { this.clearChoices(); choose(action.id); });
      return button;
    }));
    this.choices.hidden = false;
  }
}
