import type { InteractionVerb, RoomObject } from '../types.js';

export function actionsForVerb(object: RoomObject, state: string | undefined, verb: InteractionVerb): ReadonlyArray<{ readonly id: string; readonly name: string }> {
  const available = object.interaction?.actions.filter(action => action.from === state) ?? [];
  const aliases: Record<Exclude<InteractionVerb, 'walk'>, string[]> = {
    pull: ['pull', 'ziehen', 'ziehe'],
    push: ['push', 'drücken', 'druecken', 'drücke', 'druecke'],
    use: ['use', 'benutzen', 'benutze'],
  };
  if (verb === 'walk') return [];
  if (verb === 'use') return available;
  const matching = available.filter(action => aliases[verb].includes(action.id.toLowerCase()) || aliases[verb].includes(action.name.toLowerCase()));
  return matching;
}
