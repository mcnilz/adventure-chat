import type { CharacterCatalog } from '../types.js';

export interface Participant { name: string; characterId?: string | undefined; }

export function participantFields(): {
  element: HTMLElement; setCatalog: (catalog: CharacterCatalog) => void;
  valid: () => boolean; value: () => Participant;
} {
  const element = document.createElement('div');
  const nameLabel = document.createElement('label');
  nameLabel.htmlFor = 'player-name'; nameLabel.textContent = 'Dein Name';
  const name = document.createElement('input');
  name.id = 'player-name'; name.maxLength = 24; name.required = true;
  name.setAttribute('autocomplete', 'nickname');
  name.value = new URLSearchParams(location.search).get('name')?.slice(0, 24) || `Player-${Math.floor(1000 + Math.random() * 9000)}`;
  const label = document.createElement('label');
  label.htmlFor = 'character-select'; label.textContent = 'Deine Figur';
  const select = document.createElement('select'); select.id = 'character-select';
  const preview = document.createElement('canvas');
  preview.id = 'character-preview'; preview.width = 128; preview.height = 112;
  const note = document.createElement('p');
  let catalog: CharacterCatalog = { characters: [], defaultCharacterId: null };
  let revision = 0;
  const draw = (): void => {
    const current = ++revision;
    const ctx = preview.getContext('2d')!;
    ctx.clearRect(0, 0, preview.width, preview.height);
    const character = catalog.characters.find(c => c.id === select.value);
    if (!character) return;
    const image = new Image();
    note.textContent = 'Vorschau wird geladen …';
    image.onload = () => {
      if (revision !== current) return;
      ctx.imageSmoothingEnabled = false;
      const width = character.frameWidth * 96 / character.frameHeight;
      ctx.drawImage(image, 0, character.frameHeight * 2, character.frameWidth, character.frameHeight, (128 - width) / 2, 8, width, 96);
      note.textContent = character.name;
    };
    image.onerror = () => { if (revision === current) note.textContent = 'Vorschaubild vorübergehend nicht verfügbar.'; };
    image.src = character.imageUrl;
  };
  select.addEventListener('change', draw);
  element.append(nameLabel, name, label, select, preview, note);
  return {
    element,
    setCatalog: next => {
      catalog = next; select.replaceChildren();
      for (const character of catalog.characters) {
        const option = document.createElement('option'); option.value = character.id; option.textContent = character.name; select.append(option);
      }
      select.value = catalog.defaultCharacterId ?? '';
      label.hidden = select.hidden = preview.hidden = catalog.characters.length === 0;
      note.textContent = catalog.characters.length ? '' : 'Diese Welt verwendet ihre Standarddarstellung.';
      draw();
    },
    valid: () => {
      name.setCustomValidity(name.value.trim() ? '' : 'Bitte einen Namen eingeben.');
      return name.reportValidity();
    },
    value: () => ({ name: name.value.trim(), characterId: select.value || undefined }),
  };
}
