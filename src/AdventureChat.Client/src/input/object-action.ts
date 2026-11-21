import type { InteractionVerb, RoomObject } from '../types.js';

export const VERB_PHRASES: Record<InteractionVerb, string> = {
  walk: 'Gehe zu', open: 'Öffne', close: 'Schließe', pull: 'Ziehe', push: 'Drücke', use: 'Benutzen', talk: 'Spreche mit'
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

export function actionForVerb(object: RoomObject, state: string | undefined, verb: InteractionVerb): string | null {
  const actions = object.interaction?.actions ?? [];
  if (verb === 'walk') return null;
  const matchesVerb = (action: (typeof actions)[number]) =>
    verbForActionName(action.id) === verb || verbForActionName(action.name) === verb;
  const matching = actions.find(action => action.from === state && matchesVerb(action));
  if (matching) return matching.id;
  if (verb === 'use' && !actions.some(matchesVerb))
    return actions.find(action => action.from === state)?.id ?? null;
  return null;
}
