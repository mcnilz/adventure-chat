import { ACTION_VERBS } from '../../input/object-action.js';
import type { InteractionVerb, ObjectAction } from '../../types.js';

export class EditorPreviewControls {
  private readonly controls = document.querySelector<HTMLElement>('#preview-controls')!;
  private readonly verb = document.querySelector<HTMLSelectElement>('#preview-verb')!;
  private readonly choice = document.querySelector<HTMLSelectElement>('#preview-action-choice')!;
  private readonly status = document.querySelector<HTMLElement>('#preview-result')!;

  public constructor(onVerbChanged: () => void) {
    this.verb.append(new Option('Automatisch', ''), new Option('Gehe zu', 'walk'),
      ...ACTION_VERBS.map(([value, label]) => new Option(label, value)));
    this.verb.onchange = () => { onVerbChanged(); this.result(''); };
  }

  public open(roomName: string): void {
    this.controls.hidden = false;
    this.verb.value = '';
    this.result(roomName);
  }

  public close(): void { this.controls.hidden = true; }
  public selectedVerb(): InteractionVerb | '' { return this.verb.value as InteractionVerb | ''; }
  public clearChoice(): void { this.choice.hidden = true; }
  public result(message: string): void { this.status.textContent = message; }

  public offerActions(actions: readonly Pick<ObjectAction, 'id' | 'name'>[], execute: (id: string) => void): void {
    this.choice.replaceChildren(new Option('Aktion wählen …', ''), ...actions.map(action => new Option(action.name, action.id)));
    this.choice.hidden = false;
    this.choice.onchange = () => {
      if (!this.choice.value) return;
      this.clearChoice();
      execute(this.choice.value);
    };
  }
}
