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
export function chooseWorld(worlds: WorldSummary[]): Promise<string> {
  const panel = document.createElement('section');
  panel.id = 'world-picker';
  const title = document.createElement('h1');
  title.textContent = 'Welt auswählen';
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
  panel.append(title, label, select, button);
  document.body.prepend(panel);
  return new Promise(resolve => {
    button.addEventListener('click', () => { panel.remove(); resolve(select.value); }, { once: true });
  });
}
