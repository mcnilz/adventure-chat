import type { Character, CharacterCatalog, NpcPresence, Point, RoomDefinition } from '../types.js';
import { actorHeightAt } from './depth-scale.js';

export class NpcCast {
  public readonly characters: readonly Character[];
  private readonly entries: { npc: NpcPresence; character: Character }[];

  public constructor(npcs: readonly NpcPresence[], catalog?: CharacterCatalog) {
    this.entries = npcs.map(npc => ({
      npc,
      character: {
        id: `npc:${npc.id}`, name: npc.name, position: npc.position, characterId: npc.characterId,
        appearance: catalog?.characters.find(character => character.id === npc.characterId),
        facing: 'down', state: 'idle', target: null, waypoints: [], speed: 0, walkFrame: 0, frameTimer: 0
      }
    }));
    this.entries.sort((a, b) => b.npc.position.y - a.npc.position.y);
    this.characters = this.entries.map(entry => entry.character).reverse();
  }

  public at(room: RoomDefinition, point: Point, occludingDepth: number): NpcPresence | undefined {
    return this.entries.find(({ npc, character }) => {
      if (occludingDepth > npc.position.y) return false;
      const height = actorHeightAt(room, npc.position.y);
      const aspect = character.appearance ? character.appearance.frameWidth / character.appearance.frameHeight : 0.75;
      return Math.abs(npc.position.x - point.x) <= height * aspect / 2
        && point.y >= npc.position.y - height && point.y <= npc.position.y;
    })?.npc;
  }
}
