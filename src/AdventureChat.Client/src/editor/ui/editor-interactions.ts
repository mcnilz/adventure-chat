import type { ObjectAction, ObjectAppearance, ObjectInteraction, Point } from '../../types.js';
import { EditorApi } from '../core/editor-api.js';
import { hasEffectReference, referencesState } from '../core/editor-interaction-references.js';
import type { EditorState } from '../core/editor-state.js';
import type { RoomObjectDto, RoomView } from '../core/editor-types.js';
import { editObjectAction } from './editor-action-fields.js';
import { editActorAnimation } from './editor-actor-animation-fields.js';
import { editObjectState, unusedId } from './editor-state-fields.js';
import { editObjectTransitions } from './editor-transition-fields.js';
import { InteractionFields, updateInteractionForm } from './interaction-fields.js';

export class EditorInteractions {
  private readonly root = document.createElement('section');
  private readonly error = document.createElement('p');
  private object: RoomObjectDto | undefined;
  private roomId: string | undefined;
  private exitIds: string | undefined;
  private savedRooms: readonly RoomView[] = [];
  private stateId = '';
  private actionIndex = 0;

  public constructor(parent: HTMLElement, private readonly state: EditorState, private readonly changed: () => void) {
    this.root.className = 'interaction-panel';
    this.error.setAttribute('role', 'alert');
    this.error.id = 'interaction-field-error';
    parent.append(this.root, this.error);
  }

  public async refreshReferences(): Promise<void> {
    const summaries = await EditorApi.fetchRooms();
    this.savedRooms = await Promise.all(summaries.map(room => EditorApi.fetchRoom(room.id)));
    this.render();
  }

  public isReferenced(objectId: string, stateId?: string): boolean {
    return hasEffectReference(this.rooms(), this.state.getRoom().id, objectId, stateId);
  }

  public activeAppearance(): ObjectAppearance | undefined {
    return this.object?.interaction?.states.find(state => state.id === this.stateId);
  }

  public update(object: RoomObjectDto | undefined): void {
    const room = this.state.getRoom();
    const exits = JSON.stringify(room.exits ?? []);
    if (this.object === object && this.roomId === room.id && this.exitIds === exits) return;
    if (this.object?.id !== object?.id || this.roomId !== room.id) {
      this.stateId = object?.interaction?.initialState ?? '';
      this.actionIndex = 0;
    }
    this.object = object;
    this.roomId = room.id;
    this.exitIds = exits;
    this.report('');
    this.render();
  }

  public report(message: string, field?: string, group?: string): void {
    this.error.textContent = message;
    this.error.hidden = message.length === 0;
    this.root.querySelectorAll('[aria-describedby="interaction-field-error"]').forEach(input => {
      input.removeAttribute('aria-describedby');
      input.removeAttribute('aria-invalid');
    });
    const target = Array.from(this.root.querySelectorAll<HTMLElement>('[aria-label], [data-validation-key]'))
      .find(input => (field && input.getAttribute('aria-label') === field) || (group && input.dataset.validationKey === group));
    if (message && target) {
      target.setAttribute('aria-describedby', this.error.id);
      target.setAttribute('aria-invalid', 'true');
      (target.closest('label') ?? target).after(this.error);
    } else this.root.after(this.error);
  }

  public reportValidation(errors: readonly string[]): void {
    const details = errors.flatMap(error => error.split(/\r?\n/));
    const relevant = details.filter(error => {
      const objectId = /\bObject '([^']+)'/i.exec(error)?.[1];
      const objectIndex = /\/objects\/(\d+)/i.exec(error)?.[1];
      const roomId = /\bRoom '([^']+)'/i.exec(error)?.[1];
      if (roomId && roomId !== this.roomId) return false;
      if (objectId && objectId !== this.object?.id) return false;
      if (objectIndex && this.state.getRoom().objects?.[Number(objectIndex)]?.id !== this.object?.id) return false;
      return /objects?\b|states?\b|use points|blockedexits|transitions?/i.test(error);
    });
    const detail = relevant.find(error => /initial state|initialState|obstacle|blockedexits|use radius|useRadius|use points|target state|duplicate target/i.test(error))
      ?? relevant[0] ?? '';
    if (/transitions?/i.test(detail)) { this.report(relevant.join(' '), undefined, 'object-transitions'); return; }
    if (/actorAnimation|Actor sprite/i.test(detail)) { this.report(relevant.join(' '), undefined, 'actor-animation'); return; }
    const state = /\bstate '([^']+)'/i.exec(detail)?.[1];
    if (state && this.stateId !== state) { this.report(relevant.join(' ')); return; }
    const obstacle = /\bobstacle '([^']+)'/i.exec(detail)?.[1];
    if (obstacle || /blockedexits|obstacles/i.test(detail)) {
      this.report(relevant.join(' '), undefined, obstacle ? `obstacle/${obstacle}` : 'state-navigation');
      return;
    }
    const fields: readonly (readonly [RegExp, string])[] = [
      [/initial state|initialState/i, 'Anfangszustand'],
      [/use radius|useRadius/i, 'Bedienradius'],
      [/use points|usePoint/i, 'Bedienpunkt 1 X'],
      [/target state|duplicate target/i, 'Verknüpfter Zustand'],
      [/state ID/i, 'Zustands-ID'],
      [/actions?\/\d+\/id|action and source state/i, 'Aktions-ID']
    ];
    const field = fields.find(([pattern]) => pattern.test(detail))?.[1];
    this.report(relevant.join(' '), field);
  }

  public validateFields(): boolean {
    return this.root.querySelector<HTMLInputElement>('input:invalid')?.reportValidity() ?? true;
  }

  private rooms(): RoomView[] {
    const room = this.state.getRoom();
    return [room, ...this.savedRooms.filter(saved => saved.id !== room.id)];
  }

  private save(object: RoomObjectDto): void {
    if (this.state.updateObject(object.id, object)) this.changed();
  }

  private render(): void {
    const next = document.createElement('section');
    this.renderForm(next);
    updateInteractionForm(this.root, next);
  }

  private renderForm(parent: HTMLElement): void {
    const object = this.object;
    if (!object) return;
    const room = this.state.getRoom();
    const fields = new InteractionFields(parent).group('Interaktion');
    const interaction = object.interaction;
    if (!interaction) {
      fields.button('Interaktion anlegen', () => {
        const spawn = room.spawns[0]?.position ?? { x: 0, y: 0 };
        const first = this.createAppearance(object, []);
        this.stateId = first.id;
        this.save({ ...object, interaction: {
          name: object.id, initialState: first.id, usePoint: object.usePoints?.length ? null : spawn, useRadius: 30,
          states: [first], actions: [this.createAction(first.id, [])]
        } });
      });
      return;
    }

    const update = (next: ObjectInteraction) => this.save({ ...object, interaction: next });
    fields.text('Objektname in der Verbenleiste', interaction.name, name => update({ ...interaction, name }));
    this.editUsePoints(fields, object, interaction);
    const states = interaction.states.map(state => [state.id, state.name] as const);
    fields.select('Anfangszustand', interaction.initialState, states, initialState => update({ ...interaction, initialState }));
    editObjectTransitions(fields, interaction, update);
    this.editStates(fields, object, interaction, update);
    this.editActions(fields, object, interaction, update);
    fields.button('Interaktion entfernen', () => {
      if (this.isReferenced(object.id)) { this.report('Die Interaktion ist Ziel einer Verknüpfung.'); return; }
      this.save({ ...object, interaction: null });
    });
  }

  private editUsePoints(fields: InteractionFields, object: RoomObjectDto, interaction: ObjectInteraction): void {
    const points = object.usePoints ?? (interaction.usePoint ? [interaction.usePoint] : []);
    const group = fields.group('Bedienpunkte');
    group.number('Bedienradius', interaction.useRadius, useRadius => this.save({ ...object, interaction: { ...interaction, useRadius } }));
    const assign = (next: readonly Point[], actions = interaction.actions) => this.save({
      ...object, usePoints: object.usePoints != null || next.length > 1 ? next : null,
      interaction: { ...interaction, actions, usePoint: (object.usePoints != null || next.length > 1 ? interaction.usePoint : next[0]) ?? null }
    });
    points.forEach((point, index) => {
      const edit = (next: Point) => assign(points.map((p, i) => i === index ? next : p));
      group.number(`Bedienpunkt ${index + 1} X`, point.x, x => edit({ ...point, x }));
      group.number(`Bedienpunkt ${index + 1} Y`, point.y, y => edit({ ...point, y }));
      if (points.length > 1) group.button(`Bedienpunkt ${index + 1} löschen`, () => {
        if (interaction.actions.some(action => action.actorAnimation?.point === index)) {
          this.report('Dieser Bedienpunkt wird von einer Figurenanimation verwendet.');
          return;
        }
        const actions = interaction.actions.map(action => {
          const animation = action.actorAnimation;
          return animation && animation.point > index ?
            { ...action, actorAnimation: { ...animation, point: animation.point - 1 } } : action;
        });
        assign(points.filter((_, i) => i !== index), actions);
      });
    });
    if (points.length < 8) group.button('Bedienpunkt hinzufügen', () => {
      const start = points.at(-1) ?? this.state.getRoom().spawns[0]?.position ?? { x: 0, y: 0 };
      assign([...points, { ...start, x: start.x + 1 }]);
    });
  }

  private editStates(fields: InteractionFields, object: RoomObjectDto, interaction: ObjectInteraction,
    update: (next: ObjectInteraction) => void): void {
    const states = interaction.states;
    const group = fields.group('Zustände');
    const active = states.find(state => state.id === this.stateId) ?? states[0];
    this.stateId = active?.id ?? '';
    group.select('Zustand ansehen', this.stateId, states.map(state => [state.id, state.name] as const), value => {
      this.stateId = value;
      this.report('');
      this.render();
      this.changed();
    });
    if (states.length < 32) group.button('Zustand hinzufügen', () => {
      const state = this.createAppearance(active ?? object, states);
      this.stateId = state.id;
      update({ ...interaction, states: [...states, state] });
    });
    if (!active) return;
    const replace = (next: ObjectAppearance) => update({ ...interaction, states: states.map(state => state === active ? next : state) });
    const referenced = () => interaction.initialState === active.id || interaction.actions.some(action => referencesState(action, active.id)) ||
      interaction.transitions?.some(transition => transition.from === active.id || transition.to === active.id) ||
      this.isReferenced(object.id, active.id);
    group.text('Zustands-ID', active.id, id => {
      if (id === active.id) return;
      if (!id || states.some(state => state.id === id)) { this.report('Die Zustands-ID muss eindeutig sein.', 'Zustands-ID'); return; }
      if (referenced()) { this.report('Der Zustand wird als Anfangszustand, in einer Aktion, einer Verknüpfung oder einem Übergang verwendet.', 'Zustands-ID'); return; }
      this.stateId = id;
      replace({ ...active, id });
    });
    editObjectState(group, active, this.state.getRoom(), replace);
    if (states.length > 1) group.button('Zustand löschen', () => {
      if (referenced()) { this.report('Der Zustand wird als Anfangszustand, in einer Aktion, einer Verknüpfung oder einem Übergang verwendet.'); return; }
      update({ ...interaction, states: states.filter(state => state !== active) });
    });
  }

  private editActions(fields: InteractionFields, object: RoomObjectDto, interaction: ObjectInteraction,
    update: (next: ObjectInteraction) => void): void {
    const actions = interaction.actions;
    const group = fields.group('Aktionen');
    const index = Math.min(this.actionIndex, actions.length - 1);
    group.select('Aktion bearbeiten', String(index), actions.map((action, i) => [String(i), `${action.name} (${action.from})`] as const), value => {
      this.actionIndex = Number(value);
      this.report('');
      this.render();
    });
    if (actions.length < 64) group.button('Aktion hinzufügen', () => {
      this.actionIndex = actions.length;
      update({ ...interaction, actions: [...actions, this.createAction(interaction.initialState, actions)] });
    });
    const action = actions[index];
    if (!action) return;
    const replace = (next: ObjectAction) => update({ ...interaction, actions: actions.map((item, i) => i === index ? next : item) });
    const report = (message: string, field: string) => this.report(message, field);
    editObjectAction(group, action, interaction, this.state.getRoom().id, object.id, this.rooms(), replace, report);
    editActorAnimation(group, action.actorAnimation, object.usePoints ?? (interaction.usePoint ? [interaction.usePoint] : []),
      actorAnimation => replace({ ...action, actorAnimation }), report);
    if (actions.length > 1 || interaction.transitions?.length) group.button('Aktion löschen', () =>
      update({ ...interaction, actions: actions.filter((_, i) => i !== index) }));
  }

  private createAppearance(source: Pick<ObjectAppearance, 'image' | 'position' | 'size' | 'depthY'>,
    states: readonly ObjectAppearance[]): ObjectAppearance {
    const id = unusedId('zustand', states.map(state => state.id));
    return { id, name: `Zustand ${id.split('-').at(-1)}`, image: source.image,
      position: source.position, size: source.size, depthY: source.depthY };
  }

  private createAction(state: string, actions: readonly ObjectAction[]): ObjectAction {
    return { id: unusedId('aktion', actions.map(action => action.id)), name: 'Benutzen', from: state, to: state };
  }
}
