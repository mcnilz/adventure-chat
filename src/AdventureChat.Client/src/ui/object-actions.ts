import type { MoveResultDto, RoomDefinition } from '../types.js';

export class ObjectActions {
  private readonly object = document.getElementById('interaction-object') as HTMLSelectElement;
  private readonly action = document.getElementById('interaction-action') as HTMLSelectElement;
  private readonly submit = document.getElementById('interaction-submit') as HTMLButtonElement;
  private readonly feedback = document.getElementById('interaction-feedback')!;
  private readonly container = document.getElementById('interactions')!;

  public constructor(private readonly getRoom: () => RoomDefinition,
    private readonly getStates: () => ReadonlyMap<string, string>,
    private readonly request: (objectId: string, actionId: string) => Promise<MoveResultDto>,
    private readonly showMessage: (message: string) => void) {
    this.object.addEventListener('change', () => this.updateActions());
    this.submit.addEventListener('click', () => { void this.execute(); });
  }

  public refresh(): void {
    const selected = this.object.value;
    const objects = (this.getRoom().objects ?? []).filter(o => o.interaction);
    this.container.hidden = objects.length === 0;
    this.object.replaceChildren(...objects.map(o => new Option(o.interaction!.name, o.id)));
    if (objects.some(o => o.id === selected)) this.object.value = selected;
    this.updateActions();
  }

  private updateActions(): void {
    const obj = this.getRoom().objects?.find(o => o.id === this.object.value);
    const current = obj ? this.getStates().get(obj.id) : undefined;
    const actions = obj?.interaction?.actions.filter(a => a.from === current) ?? [];
    this.action.replaceChildren(...actions.map(a => new Option(a.name, a.id)));
    this.submit.disabled = actions.length === 0;
  }

  private async execute(): Promise<void> {
    const name = this.action.selectedOptions[0]?.textContent ?? '';
    this.submit.disabled = true;
    const result = await this.request(this.object.value, this.action.value);
    this.feedback.textContent = result.success ? `${name} ausgeführt.` : result.error ?? 'Aktion abgelehnt.';
    this.feedback.title = this.feedback.textContent;
    if (!result.success) this.showMessage(this.feedback.textContent);
    this.updateActions();
  }
}
