export class ChatUiController {
    onSendMessage;
    elements;
    constructor(elements, onSendMessage) {
        this.onSendMessage = onSendMessage;
        this.elements = {
            chatContainer: elements?.chatContainer ?? document.getElementById('chat-container'),
            chatMessages: elements?.chatMessages ?? document.getElementById('chat-messages'),
            chatForm: elements?.chatForm ?? document.getElementById('chat-form'),
            chatInput: elements?.chatInput ?? document.getElementById('chat-input'),
            chatSend: elements?.chatSend ?? document.getElementById('chat-send'),
            actionText: elements?.actionText ?? document.getElementById('action-text'),
        };
        this.setupListeners();
    }
    setConnectionStatus(status) {
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
    setActionText(text) {
        if (this.elements.actionText) {
            this.elements.actionText.textContent = text;
        }
    }
    appendChatMessage(msg, localSessionId) {
        if (!this.elements.chatMessages)
            return;
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
    appendSystemMessage(text) {
        if (!this.elements.chatMessages)
            return;
        const msgEl = document.createElement('div');
        msgEl.className = 'chat-system-message';
        msgEl.textContent = text;
        this.elements.chatMessages.appendChild(msgEl);
        this.scrollToBottom();
    }
    appendSystemError(errorMessage) {
        if (!this.elements.chatMessages)
            return;
        const errorEl = document.createElement('div');
        errorEl.className = 'chat-system-error';
        errorEl.textContent = errorMessage;
        this.elements.chatMessages.appendChild(errorEl);
        this.scrollToBottom();
    }
    scrollToBottom() {
        if (this.elements.chatMessages) {
            this.elements.chatMessages.scrollTop = this.elements.chatMessages.scrollHeight;
        }
    }
    setupListeners() {
        // Prevent chat interactions from bubbling to canvas or triggering movement/shortcuts
        if (this.elements.chatContainer) {
            const stopEvent = (e) => {
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
                if (!input)
                    return;
                const rawText = input.value;
                if (!rawText || !rawText.trim())
                    return;
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
//# sourceMappingURL=chat-ui.js.map