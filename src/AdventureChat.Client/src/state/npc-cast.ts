import type { Character, CharacterCatalog, NpcPresence, RoomDefinition } from '../types.js';
import { assignPathToCharacter, updateCharacterMovement } from '../simulation/movement.js';

export function npcActorId(npcId: string): string {
  return `npc:${npcId}`;
}

export class NpcCast {
  private actors: { npc: NpcPresence; character: Character }[] = [];
  private catalog?: CharacterCatalog;

  public get entries(): readonly { readonly npc: NpcPresence; readonly character: Character }[] { return this.actors; }
  public get characters(): Character[] { return this.actors.map(entry => entry.character).reverse(); }

  public setRoom(room: RoomDefinition): void {
    const npcs = room.npcs ?? [];
    this.actors = npcs.map(npc => ({
      npc,
      character: {
        id: npcActorId(npc.id), name: npc.name, position: npc.position, characterId: npc.characterId,
        appearance: this.catalog?.characters.find(character => character.id === npc.characterId),
        facing: 'down', state: 'idle', target: null, waypoints: [], speed: room.walkSpeed ?? 130, walkFrame: 0, frameTimer: 0
      }
    }));
    this.update(npcs);
  }

  public setCatalog(catalog: CharacterCatalog): void {
    this.catalog = catalog;
    for (const { npc, character } of this.actors)
      character.appearance = catalog.characters.find(candidate => candidate.id === npc.characterId);
  }

  public update(npcs: readonly NpcPresence[]): void {
    for (const entry of this.actors) {
      const npc = npcs.find(candidate => candidate.id === entry.npc.id);
      if (!npc) continue;
      entry.npc = npc;
      const character = entry.character;
      character.position = npc.position;
      assignPathToCharacter(character, [...(npc.motion?.route ?? [])]);
      character.facing = npc.motion?.direction ?? 'down';
    }
  }

  public advance(dt: number): void {
    for (const { character } of this.actors) updateCharacterMovement(character, dt);
  }
}
