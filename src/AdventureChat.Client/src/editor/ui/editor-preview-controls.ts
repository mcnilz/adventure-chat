import { ACTION_VERBS } from '../../input/object-action.js';
import type { InteractionVerb } from '../../types.js';

export class EditorPreviewControls {
  public readonly element = document.querySelector<HTMLElement>('#preview-controls')!;
  public readonly verb = document.querySelector<HTMLSelectElement>('#preview-verb')!;
  private readonly status = document.querySelector<HTMLElement>('#preview-result')!;

  public constructor(onVerbChanged: () => void) {
    this.verb.append(new Option('Automatisch', ''), new Option('Gehe zu', 'walk'),
      ...ACTION_VERBS.map(([value, label]) => new Option(label, value)), new Option('Spreche mit', 'talk'));
    this.verb.onchange = () => { onVerbChanged(); this.result(''); };
  }

  public open(roomName: string): void {
    this.element.hidden = false;
    this.verb.value = '';
    this.result(roomName);
  }

  public close(): void { this.element.hidden = true; }
  public selectedVerb(): InteractionVerb | '' { return this.verb.value as InteractionVerb | ''; }
  public result(message: string): void { this.status.textContent = message; }
}
