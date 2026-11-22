import type { Character, CharacterCatalog, NpcPresence, RoomDefinition } from '../types.js';
import { assignPathToCharacter, updateCharacterMovement } from '../simulation/movement.js';

export function npcActorId(npcId: string): string {
  return `npc:${npcId}`;
}

export class NpcCast {
  private actors: { npc: NpcPresence; character: Character }[] = [];
  private catalog?: CharacterCatalog;
  private speed = 130;

  public get entries(): readonly { readonly npc: NpcPresence; readonly character: Character }[] { return this.actors; }
  public get characters(): Character[] { return this.actors.map(entry => entry.character).reverse(); }

  public setRoom(room: RoomDefinition): void {
    this.speed = room.walkSpeed ?? 130;
    this.actors = [];
    this.update(room.npcs ?? []);
  }

  public setCatalog(catalog: CharacterCatalog): void {
    this.catalog = catalog;
    for (const { npc, character } of this.actors)
      character.appearance = catalog.characters.find(candidate => candidate.id === npc.characterId);
  }

  public update(npcs: readonly NpcPresence[]): void {
    const previous = new Map(this.actors.map(entry => [entry.npc.id, entry.character]));
    this.actors = npcs.map(npc => {
      const character: Character = previous.get(npc.id) ?? {
        id: npcActorId(npc.id), name: npc.name, position: npc.position, characterId: npc.characterId,
        appearance: this.catalog?.characters.find(character => character.id === npc.characterId),
        facing: 'down', state: 'idle', target: null, waypoints: [], speed: this.speed, walkFrame: 0, frameTimer: 0
      };
      character.position = npc.position;
      assignPathToCharacter(character, [...(npc.motion?.route ?? [])]);
      character.facing = npc.motion?.direction ?? 'down';
      return { npc, character };
    });
  }

  public advance(dt: number): void {
    for (const { character } of this.actors) updateCharacterMovement(character, dt);
  }
}
