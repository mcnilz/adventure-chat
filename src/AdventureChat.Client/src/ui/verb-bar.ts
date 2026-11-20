import type { InteractionVerb } from '../types.js';

export class VerbBar {
  private readonly buttons = Array.from(document.querySelectorAll<HTMLButtonElement>('#verb-bar button[data-verb]'));

  public constructor(onSelect: (verb: InteractionVerb) => void) {
    for (const button of this.buttons) button.addEventListener('click', () => {
      this.setSelected(button.dataset.verb as InteractionVerb);
      onSelect(button.dataset.verb as InteractionVerb);
    });
  }

  public setSelected(verb: InteractionVerb): void {
    for (const button of this.buttons) button.setAttribute('aria-pressed', String(button.dataset.verb === verb));
  }

}
