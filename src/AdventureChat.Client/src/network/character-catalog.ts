import type { CharacterCatalog } from '../types.js';

export async function loadCharacterCatalog(worldId: string): Promise<CharacterCatalog> {
  const response = await fetch(`/api/worlds/${encodeURIComponent(worldId)}/characters`);
  if (!response.ok) throw new Error('Figuren der Welt nicht verfügbar. Bitte Welt erneut auswählen.');
  return await response.json() as CharacterCatalog;
}
