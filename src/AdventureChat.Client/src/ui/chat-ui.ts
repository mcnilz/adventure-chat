import type { ChatMessageDto } from '../types.js';

export interface ChatUiElements {
  chatContainer: HTMLElement | null;
  chatMessages: HTMLElement | null;
  chatForm: HTMLFormElement | null;
  chatInput: HTMLInputElement | null;
  chatSend: HTMLButtonElement | null;
  actionText: HTMLElement | null;
}

export class ChatUiController {
  private readonly elements: ChatUiElements;

  public constructor(
    elements?: Partial<ChatUiElements>,
    private readonly onSendMessage?: (text: string) => Promise<{ success: boolean; error?: string | null }>
  ) {
    this.elements = {
      chatContainer: elements?.chatContainer ?? document.getElementById('chat-container'),
      chatMessages: elements?.chatMessages ?? document.getElementById('chat-messages'),
      chatForm: elements?.chatForm ?? (document.getElementById('chat-form') as HTMLFormElement | null),
      chatInput: elements?.chatInput ?? (document.getElementById('chat-input') as HTMLInputElement | null),
      chatSend: elements?.chatSend ?? (document.getElementById('chat-send') as HTMLButtonElement | null),
      actionText: elements?.actionText ?? document.getElementById('action-text'),
    };

    this.setupListeners();
  }

  public setConnectionStatus(status: 'connected' | 'connecting' | 'disconnected'): void {
    const isConnected = status === 'connected';
    if (this.elements.chatInput) {
      this.elements.chatInput.disabled = !isConnected;
      this.elements.chatInput.placeholder = isConnected
        ? 'Nachricht eingeben...'
        : 'Verbindung zum Server wird aufgebaut...';
    }
    if (this.elements.chatSend) {
      this.elements.chatSend.disabled = !isConnected;
    }
  }

  public setActionText(text: string): void {
    if (this.elements.actionText) {
      this.elements.actionText.textContent = text;
    }
  }

  public appendChatMessage(msg: ChatMessageDto, localSessionId: string): void {
    if (!this.elements.chatMessages) return;

    const msgEl = document.createElement('div');
    msgEl.className = 'chat-message';

    const senderEl = document.createElement('span');
    senderEl.className = 'chat-sender';
    if (msg.sessionId === localSessionId) {
      senderEl.classList.add('local');
    }
    senderEl.textContent = `${msg.senderName}: `;

    const textEl = document.createElement('span');
    textEl.className = 'chat-text';
    // Use textContent exclusively to guarantee XSS protection
    textEl.textContent = msg.text;

    msgEl.appendChild(senderEl);
    msgEl.appendChild(textEl);
    this.elements.chatMessages.appendChild(msgEl);

    this.scrollToBottom();
  }

  public appendSystemMessage(text: string): void {
    if (!this.elements.chatMessages) return;

    const msgEl = document.createElement('div');
    msgEl.className = 'chat-system-message';
    msgEl.textContent = text;
    this.elements.chatMessages.appendChild(msgEl);

    this.scrollToBottom();
  }

  public appendSystemError(errorMessage: string): void {
    if (!this.elements.chatMessages) return;

    const errorEl = document.createElement('div');
    errorEl.className = 'chat-system-error';
    errorEl.textContent = errorMessage;
    this.elements.chatMessages.appendChild(errorEl);

    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    if (this.elements.chatMessages) {
      this.elements.chatMessages.scrollTop = this.elements.chatMessages.scrollHeight;
    }
  }

  private setupListeners(): void {
    // Prevent chat interactions from bubbling to canvas or triggering movement/shortcuts
    if (this.elements.chatContainer) {
      const stopEvent = (e: Event) => {
        e.stopPropagation();
      };
      this.elements.chatContainer.addEventListener('click', stopEvent);
      this.elements.chatContainer.addEventListener('mousedown', stopEvent);
      this.elements.chatContainer.addEventListener('pointerdown', stopEvent);
      this.elements.chatContainer.addEventListener('keydown', stopEvent);
      this.elements.chatContainer.addEventListener('keyup', stopEvent);
    }

    if (this.elements.chatForm && this.elements.chatInput) {
      this.elements.chatForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const input = this.elements.chatInput;
        if (!input) return;

        const rawText = input.value;
        if (!rawText || !rawText.trim()) return;

        const trimmed = rawText.trim();
        input.value = '';

        if (this.onSendMessage) {
          const result = await this.onSendMessage(trimmed);
          if (!result.success) {
            this.appendSystemError(result.error || 'Nachricht konnte nicht gesendet werden.');
          }
        }
      });
    }
  }
}
