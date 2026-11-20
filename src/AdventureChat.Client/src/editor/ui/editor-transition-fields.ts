import type { ObjectInteraction, ObjectTransition } from '../../types.js';
import type { InteractionFields } from './interaction-fields.js';

export function editObjectTransitions(fields: InteractionFields, interaction: ObjectInteraction,
  update: (value: ObjectInteraction) => void): void {
  const transitions = interaction.transitions ?? [];
  if (interaction.states.length < 2 && !transitions.length) return;
  const group = fields.group('Sichtbare Übergänge', 'object-transitions');
  const states = interaction.states.map(state => [state.id, state.name] as const);
  const assign = (next: readonly ObjectTransition[]) => update({ ...interaction, transitions: next.length ? next : null });
  transitions.forEach((transition, index) => {
    const row = group.group(`Übergang ${index + 1}`);
    const replace = (next: ObjectTransition) => assign(transitions.map(item => item === transition ? next : item));
    row.select(`Übergang ${index + 1} von`, transition.from, states, from => replace({ ...transition, from }));
    row.select(`Übergang ${index + 1} nach`, transition.to, states, to => replace({ ...transition, to }));
    row.number(`Übergang ${index + 1} Dauer (ms)`, transition.durationMs, durationMs => replace({ ...transition, durationMs }));
    row.button(`Übergang ${index + 1} löschen`, () => assign(transitions.filter(item => item !== transition)));
  });
  const available = states.flatMap(([from]) => states
    .filter(([to]) => to !== from && !transitions.some(item => item.from === from && item.to === to))
    .map(([to]) => ({ from, to, durationMs: 600 })));
  const next = available[0];
  if (next && transitions.length < 64) group.button('Übergang hinzufügen', () => assign([...transitions, next]));
}
