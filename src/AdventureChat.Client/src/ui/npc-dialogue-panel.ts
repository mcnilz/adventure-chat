import type { DialogueOption, NpcPresence, SpeechBubble } from '../types.js';

const TURN_PAUSE_MS = 250;
const REPLY_PAGE_LENGTH = 110;
type DialogueBeat = { readonly duration: number; readonly text?: string; readonly npc?: NpcPresence };

function replyPages(text: string): string[] {
  const pages: string[] = [];
  const graphemes = new Intl.Segmenter(undefined, { granularity: 'grapheme' });
  for (const { segment } of new Intl.Segmenter('de', { granularity: 'sentence' }).segment(text)) {
    let characters = Array.from(graphemes.segment(segment.trim().replace(/\s+/gu, ' ')), part => part.segment);
    while (characters.length > REPLY_PAGE_LENGTH) {
      let breakAt = REPLY_PAGE_LENGTH;
      while (breakAt > 0 && !/\s/u.test(characters[breakAt]!)) breakAt--;
      const end = breakAt > 0 ? breakAt : REPLY_PAGE_LENGTH;
      pages.push(characters.slice(0, end).join('').trim());
      characters = characters.slice(breakAt > 0 ? end + 1 : end);
    }
    if (characters.length) pages.push(characters.join('').trim());
  }
  return pages;
}

export class NpcDialoguePanel {
  private readonly panel = document.getElementById('npc-dialogue')!;
  private readonly verbs = document.getElementById('verb-bar')!;
  private readonly name = document.getElementById('npc-dialogue-name')!;
  private readonly choices = document.getElementById('npc-choices')!;
  private readonly reply = document.getElementById('npc-reply')!;
  private readonly fallback = document.getElementById('npc-dialogue-bubble')!;
  private readonly talk = document.querySelector<HTMLButtonElement>('#verb-bar button[data-verb="talk"]')!;
  private timer: number | undefined;
  private beats: DialogueBeat[] = [];

  public constructor(
    canvas: HTMLCanvasElement,
    private readonly showBubble: (bubble: SpeechBubble | null, npcId?: string) => boolean,
    private readonly onModeChanged: (npcName?: string) => void
  ) {
    document.getElementById('npc-dialogue-close')!.addEventListener('click', () => this.dismiss());
    document.addEventListener('keydown', event => {
      if (!this.isOpen) return;
      if (event.key === 'Escape') {
        event.preventDefault();
        this.dismiss();
      } else if (event.key === '.' && !event.repeat && !event.ctrlKey && !event.metaKey &&
        !(event.target instanceof HTMLElement && (event.target.isContentEditable || event.target.closest('input, textarea, select'))) &&
        this.advance()) {
        event.preventDefault();
      }
    });
    canvas.addEventListener('click', event => {
      if (event.button === 0 && this.advance()) event.stopImmediatePropagation();
    }, { capture: true });
    this.fallback.addEventListener('click', event => {
      if (event.button === 0) this.advance();
    });
  }

  public get isOpen(): boolean { return !this.panel.hidden; }

  public open(npc: NpcPresence, options: readonly DialogueOption[]): void {
    this.close();
    this.name.textContent = npc.name;
    for (const option of options) {
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = option.text;
      button.addEventListener('click', () => this.exchange(npc, option));
      this.choices.append(button);
    }
    this.verbs.hidden = true;
    this.panel.hidden = false;
    this.onModeChanged(npc.name);
    this.choices.querySelector('button')?.focus();
  }

  public close(): void {
    if (this.panel.hidden) return;
    window.clearTimeout(this.timer);
    this.timer = undefined;
    this.beats = [];
    this.setLine(null);
    this.panel.hidden = true;
    this.verbs.hidden = false;
    this.choices.replaceChildren();
    this.reply.textContent = '';
    this.onModeChanged();
  }

  private exchange(npc: NpcPresence, option: DialogueOption): void {
    if (this.timer !== undefined) return;
    this.setChoicesAvailable(false);
    const questionTime = Math.min(3800, 950 + option.text.length * 32);
    this.beats = [
      { text: option.text, duration: questionTime },
      { duration: TURN_PAUSE_MS },
      ...replyPages(option.reply).flatMap((text, index) => {
        const line = { text, npc, duration: 1600 + text.length * 32 };
        return index === 0 ? [line] : [{ duration: TURN_PAUSE_MS }, line];
      })
    ];
    this.playNext();
  }

  private advance(): boolean {
    if (this.timer === undefined) return false;
    window.clearTimeout(this.timer);
    if (this.beats[0]?.text === undefined) this.beats.shift();
    this.playNext();
    return true;
  }

  private playNext(): void {
    const beat = this.beats.shift();
    const bubble = beat?.text ? { text: beat.text, startTime: performance.now(), duration: beat.duration } : null;
    this.setLine(bubble, beat?.npc);
    this.reply.textContent = beat?.npc ? beat.text ?? '' : '';
    if (beat) this.timer = window.setTimeout(() => this.playNext(), beat.duration);
    else {
      this.timer = undefined;
      this.setChoicesAvailable(true);
    }
  }

  private setLine(bubble: SpeechBubble | null, npc?: NpcPresence): void {
    const drawn = this.showBubble(bubble, npc?.id);
    this.fallback.hidden = drawn || !bubble;
    this.fallback.textContent = bubble && !drawn ? `${npc?.name ?? 'Du'} · ${bubble.text}` : '';
  }

  private setChoicesAvailable(available: boolean): void {
    for (const button of this.choices.querySelectorAll('button')) {
      button.setAttribute('aria-disabled', String(!available));
    }
  }

  private dismiss(): void {
    this.close();
    this.talk.focus();
  }
}
