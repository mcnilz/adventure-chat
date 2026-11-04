import { loadCharacterCatalog } from '../network/character-catalog.js';
import { participantFields, type Participant } from './participant-picker.js';
import type { CharacterCatalog } from '../types.js';

export interface WorldSummary { readonly id: string; readonly name: string; }

export function showStartupError(error: unknown): void {
  const panel = document.createElement('section');
  panel.id = 'world-picker';
  const message = document.createElement('p');
  message.textContent = `Start fehlgeschlagen: ${String(error)}`;
  const retry = document.createElement('button');
  retry.textContent = 'Erneut versuchen';
  retry.onclick = () => location.reload();
  panel.append(message, retry);
  document.body.prepend(panel);
}

export async function loadWorlds(): Promise<WorldSummary[]> {
  const response = await fetch('/api/worlds');
  if (!response.ok) throw new Error('Weltliste konnte nicht geladen werden.');
  return await response.json() as WorldSummary[];
}

// Called explicitly by each bootstrap; no DOM work at module import time.
export function chooseWorld(worlds: WorldSummary[], includeParticipant = true): Promise<{ worldId: string; catalog: CharacterCatalog; participant: Participant }> {
  const panel = document.createElement('section');
  panel.id = 'world-picker';
  const title = document.createElement('h1');
  title.textContent = 'Welt auswählen';
  const notice = document.createElement('p');
  const previousError = sessionStorage.getItem('world-selection-error');
  sessionStorage.removeItem('world-selection-error');
  notice.textContent = previousError ? `Bitte Welt und Figur erneut auswählen: ${previousError}` : '';
  const label = document.createElement('label');
  label.htmlFor = 'world-select';
  label.textContent = 'Verfügbare Welten';
  const select = document.createElement('select');
  select.id = 'world-select';
  for (const world of worlds) {
    const option = document.createElement('option');
    option.value = world.id;
    option.textContent = world.name;
    select.append(option);
  }
  const button = document.createElement('button');
  button.id = 'world-enter';
  button.textContent = 'Welt betreten';
  button.disabled = worlds.length === 0;
  const fields = participantFields();
  fields.element.hidden = !includeParticipant;
  const error = document.createElement('p');
  error.setAttribute('role', 'alert');
  let catalog: CharacterCatalog | undefined;
  let revision = 0;
  const load = async (): Promise<void> => {
    const current = ++revision;
    button.disabled = true;
    catalog = undefined;
    error.textContent = '';
    try {
      const loaded = await loadCharacterCatalog(select.value);
      if (current !== revision) return;
      catalog = loaded;
      fields.setCatalog(loaded);
      button.disabled = false;
    } catch (e) { if (current === revision) error.textContent = String(e); }
  };
  select.addEventListener('change', () => { void load(); });
  panel.append(title, notice, label, select, fields.element, error, button);
  if (worlds.length) void load();
  document.body.prepend(panel);
  return new Promise(resolve => {
    button.addEventListener('click', () => {
      if (!catalog || (includeParticipant && !fields.valid())) return;
      panel.remove();
      resolve({ worldId: select.value, catalog, participant: fields.value() });
    });
  });
}
