import type { DialogueOption, NpcDialogueResult, NpcPresence, SpeechBubble } from '../types.js';

const TURN_PAUSE_MS = 250;
const REPLY_PAGE_LENGTH = 110;
type DialogueBeat = { readonly duration: number; readonly text?: string; readonly npc?: NpcPresence };
type Dialogue = { readonly npc: NpcPresence; readonly choose: (option: DialogueOption) => Promise<NpcDialogueResult> };
interface DialogueControls {
  readonly verbs: HTMLElement;
  readonly returnFocus: HTMLElement;
  readonly advanceEvent?: 'click' | 'mousedown';
  readonly onModeChanged?: (npcName?: string) => void;
}

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
  private readonly name = document.getElementById('npc-dialogue-name')!;
  private readonly choices = document.getElementById('npc-choices')!;
  private readonly notice = document.getElementById('npc-dialogue-notice')!;
  private readonly closeButton = document.getElementById('npc-dialogue-close')!;
  private readonly reply = document.getElementById('npc-reply')!;
  private readonly fallback = document.getElementById('npc-dialogue-bubble')!;
  private timer: number | undefined;
  private beats: DialogueBeat[] = [];
  private dialogue: Dialogue | null = null;
  private options: readonly DialogueOption[] = [];
  private nextOptions: readonly DialogueOption[] | null = null;
  private pending = false;
  private skipPendingQuestion = false;

  public constructor(
    canvas: HTMLCanvasElement,
    private readonly controls: DialogueControls,
    private readonly showBubble: (bubble: SpeechBubble | null, npcId?: string) => boolean
  ) {
    this.closeButton.addEventListener('click', () => this.dismiss());
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
    canvas.addEventListener(controls.advanceEvent ?? 'click', event => {
      if (event.button === 0 && this.advance()) event.stopImmediatePropagation();
    }, { capture: true });
    this.fallback.addEventListener('click', event => {
      if (event.button === 0) this.advance();
    });
  }

  public get isOpen(): boolean { return !this.panel.hidden; }

  public open(npc: NpcPresence, options: readonly DialogueOption[], choose: Dialogue['choose']): void {
    this.close();
    this.dialogue = { npc, choose };
    this.name.textContent = npc.name;
    this.controls.verbs.hidden = true;
    this.panel.hidden = false;
    this.controls.onModeChanged?.(npc.name);
    this.showOptions(options);
  }

  private showOptions(options: readonly DialogueOption[], message?: string): void {
    this.options = options;
    this.setNotice(options.length ? message : 'Im Moment gibt es keine Fragen.');
    this.choices.replaceChildren();
    for (const option of options) {
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = option.text;
      button.addEventListener('click', () => void this.select(option));
      this.choices.append(button);
    }
    (this.choices.querySelector('button') ?? this.closeButton).focus();
  }

  private setNotice(message?: string): void {
    this.notice.textContent = message ?? '';
    this.notice.hidden = !message;
  }

  public close(): void {
    if (this.panel.hidden) return;
    window.clearTimeout(this.timer);
    this.timer = undefined;
    this.beats = [];
    this.dialogue = null;
    this.options = [];
    this.nextOptions = null;
    this.pending = false;
    this.skipPendingQuestion = false;
    this.setLine(null);
    this.panel.hidden = true;
    this.controls.verbs.hidden = false;
    this.choices.replaceChildren();
    this.setNotice();
    this.reply.textContent = '';
    this.controls.onModeChanged?.();
  }

  private async select(option: DialogueOption): Promise<void> {
    const dialogue = this.dialogue;
    if (!dialogue || this.pending || this.timer !== undefined) return;
    this.pending = true;
    this.skipPendingQuestion = false;
    this.setNotice();
    this.setChoicesAvailable(false);
    const result = await dialogue.choose(option);
    if (this.dialogue !== dialogue) return;
    this.pending = false;
    if (result.selected) {
      this.nextOptions = result.options;
      this.exchange(dialogue.npc, result.selected);
      if (this.skipPendingQuestion) this.advance();
    } else if (result.options) {
      this.showOptions(result.options, result.error ?? undefined);
    } else {
      this.setNotice(result.error ?? 'Diese Frage konnte nicht gestellt werden.');
      this.setChoicesAvailable(true);
    }
  }

  private exchange(npc: NpcPresence, option: DialogueOption): void {
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
    if (this.pending) {
      this.skipPendingQuestion = true;
      return true;
    }
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
      const next = this.nextOptions;
      this.nextOptions = null;
      if (next && (next.length !== this.options.length || next.some((option, index) => {
        const previous = this.options[index];
        return !previous || option.id !== previous.id || option.text !== previous.text || option.reply !== previous.reply;
      }))) this.showOptions(next);
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
    this.controls.returnFocus.focus();
  }
}
