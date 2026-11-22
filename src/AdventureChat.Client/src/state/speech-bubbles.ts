import type { Character, SpeechBubble } from '../types.js';

export function bubbleIsActive(bubble: SpeechBubble, now: number): boolean {
  return now - bubble.startTime < bubble.duration;
}

export function expireSpeechBubble(character: Character, now: number): void {
  if (character.speechBubble && !bubbleIsActive(character.speechBubble, now)) character.speechBubble = null;
}
