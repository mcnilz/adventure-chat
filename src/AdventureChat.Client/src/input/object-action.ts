import type { InteractionVerb, RoomObject } from '../types.js';

export const VERB_PHRASES: Record<InteractionVerb, string> = {
  walk: 'Gehe zu', open: 'Öffne', close: 'Schließe', pull: 'Ziehe', push: 'Drücke', use: 'Benutzen'
};

const aliases = {
  open: ['open', 'öffnen', 'oeffnen', 'öffne', 'oeffne'],
  close: ['close', 'schließen', 'schliessen', 'schließe', 'schliesse'],
  pull: ['pull', 'ziehen', 'ziehe'],
  push: ['push', 'drücken', 'druecken', 'drücke', 'druecke'],
};

export const ACTION_VERBS = [
  ['open', 'Öffnen'], ['close', 'Schließen'], ['pull', 'Ziehen'], ['push', 'Drücken'], ['use', 'Benutzen']
] as const;

export function verbForActionName(name: string): string {
  return Object.entries(aliases).find(([, names]) => names.includes(name.toLowerCase()))?.[0]
    ?? (['use', 'benutzen', 'benutze'].includes(name.toLowerCase()) ? 'use' : '');
}

export function actionsForVerb(object: RoomObject, state: string | undefined, verb: InteractionVerb): ReadonlyArray<{ readonly id: string; readonly name: string }> {
  const available = object.interaction?.actions.filter(action => action.from === state) ?? [];
  if (verb === 'walk') return [];
  if (verb === 'use') return available;
  return available.filter(action => aliases[verb].includes(action.id.toLowerCase()) || aliases[verb].includes(action.name.toLowerCase()));
}
