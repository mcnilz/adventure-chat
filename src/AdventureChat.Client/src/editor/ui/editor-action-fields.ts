import { ACTION_VERBS, verbForActionName } from '../../input/object-action.js';
import type { ObjectAction, ObjectInteraction } from '../../types.js';
import type { RoomView } from '../core/editor-types.js';
import { InteractionFields } from './interaction-fields.js';

export function editObjectAction(fields: InteractionFields, action: ObjectAction, interaction: ObjectInteraction,
  roomId: string, objectId: string, rooms: readonly RoomView[], update: (next: ObjectAction) => void,
  error: (message: string, field: string) => void): void {
  const conflicts = (id: string, name: string) => {
    const idVerb = verbForActionName(id);
    const namedVerb = verbForActionName(name);
    return Boolean(idVerb && idVerb !== 'use' && namedVerb && idVerb !== namedVerb);
  };
  const reportConflict = (field: string) => {
    error('Die Aktions-ID wird als anderes Verb erkannt. Wähle eine neutrale Aktions-ID.', field);
    return false;
  };
  fields.text('Aktions-ID', action.id, id => {
    if (id !== action.id && conflicts(id, action.name)) return reportConflict('Aktions-ID');
    update({ ...action, id });
    return true;
  });
  fields.select('Verb', verbForActionName(action.name), [['', 'Eigener Aktionsname'], ...ACTION_VERBS], verb => {
    const name = ACTION_VERBS.find(([key]) => key === verb)?.[1];
    if (name && conflicts(action.id, name)) return reportConflict('Aktions-ID');
    if (name) update({ ...action, name });
    return true;
  });
  fields.text('Aktionsname', action.name, name => {
    if (name !== action.name && conflicts(action.id, name)) return reportConflict('Aktionsname');
    update({ ...action, name });
    return true;
  });
  const states = interaction.states.map(s => [s.id, s.name] as const);
  fields.select('Ausgangszustand', action.from, states, from => update({ ...action, from }));
  fields.select('Zielzustand', action.to, states, to => update({ ...action, to }));

  const effects = action.effects ?? [];
  const candidates = rooms.flatMap(room => (room.objects ?? []).flatMap(object => {
    if (!object.interaction?.states.length || (room.id === roomId && object.id === objectId)) return [];
    return [{ room, object, states: object.interaction.states }];
  }));
  const targetFor = (room: string, object: string) => candidates.find(item => item.room.id === room && item.object.id === object);
  const key = (target: { room: string; object: string }) => `${target.room}/${target.object}`;
  const eligible = (index: number) => candidates.filter(candidate => !effects.some((e, i) =>
    i !== index && e.room === candidate.room.id && e.object === candidate.object.id));
  const replace = (index: number, next: NonNullable<ObjectAction['effects']>[number]) =>
    update({ ...action, effects: effects.map((effect, i) => i === index ? next : effect) });

  effects.forEach((effect, index) => {
    const group = fields.group(`Verknüpfung ${index + 1}`);
    const options = eligible(index);
    const selected = targetFor(effect.room, effect.object);
    const targets: Array<readonly [string, string]> = options.map(candidate => [
      key({ room: candidate.room.id, object: candidate.object.id }), `${candidate.room.name} / ${candidate.object.interaction!.name}`
    ]);
    if (!selected) targets.push([key(effect), `${key(effect)} (fehlt)`]);
    group.select('Zielobjekt', key(effect), targets, value => {
      const target = options.find(candidate => key({ room: candidate.room.id, object: candidate.object.id }) === value);
      if (target) replace(index, { room: target.room.id, object: target.object.id, state: target.states[0]!.id });
    });
    const states = selected?.states.map(state => [state.id, state.name] as const) ?? [];
    if (!states.some(([id]) => id === effect.state)) states.push([effect.state, `${effect.state} (fehlt)`]);
    group.select('Verknüpfter Zustand', effect.state, states, state => replace(index, { ...effect, state }));
    group.button('Verknüpfung löschen', () => update({ ...action, effects: effects.filter((_, i) => i !== index) }));
  });
  const next = eligible(-1)[0];
  if (next && effects.length < 32) fields.button('Verknüpfung hinzufügen', () => {
    update({ ...action, effects: [...effects, { room: next.room.id, object: next.object.id, state: next.states[0]!.id }] });
  });
}

export function editNpcReaction(fields: InteractionFields, room: RoomView, action: ObjectAction,
  update: (next: ObjectAction) => void): void {
  const npcs = room.npcs ?? [];
  const reaction = action.npcReaction;
  if (!reaction && npcs.length === 0) return;

  const group = fields.group('NPC-Reaktion');
  const options: [string, string][] = [['', 'Keine'], ...npcs.map(npc => [npc.id, npc.name] as [string, string])];
  if (reaction && !npcs.some(npc => npc.id === reaction.npc))
    options.push([reaction.npc, `${reaction.npc} (fehlt)`]);
  group.select('Reagierender NPC', reaction?.npc ?? '', options, id => {
    const npc = npcs.find(candidate => candidate.id === id);
    update({ ...action, npcReaction: npc ? { npc: npc.id, position: reaction?.position ?? npc.position } : null });
  });
  if (!reaction) return;
  group.number('Laufziel X', reaction.position.x, x =>
    update({ ...action, npcReaction: { ...reaction, position: { ...reaction.position, x } } }));
  group.number('Laufziel Y', reaction.position.y, y =>
    update({ ...action, npcReaction: { ...reaction, position: { ...reaction.position, y } } }));
}
