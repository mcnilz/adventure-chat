import { AdventureGame } from './game.js';
function initialize() {
    const canvas = document.getElementById('game-canvas');
    if (!canvas) {
        throw new Error('Canvas element #game-canvas not found');
    }
    const chatContainer = document.getElementById('chat-container');
    const chatMessages = document.getElementById('chat-messages');
    const chatForm = document.getElementById('chat-form');
    const chatInput = document.getElementById('chat-input');
    const chatSend = document.getElementById('chat-send');
    const actionText = document.getElementById('action-text');
    const game = new AdventureGame(canvas);
    window.__ADVENTURE_GAME__ = game;
    // Prevent chat interactions from triggering canvas movement or keyboard shortcuts
    if (chatContainer) {
        const stopEvent = (e) => {
            e.stopPropagation();
        };
        chatContainer.addEventListener('click', stopEvent);
        chatContainer.addEventListener('mousedown', stopEvent);
        chatContainer.addEventListener('pointerdown', stopEvent);
        chatContainer.addEventListener('keydown', stopEvent);
        chatContainer.addEventListener('keyup', stopEvent);
    }
    const updateInputState = (status) => {
        const isConnected = status === 'connected';
        if (chatInput) {
            chatInput.disabled = !isConnected;
            chatInput.placeholder = isConnected ? 'Nachricht eingeben...' : 'Verbindung zum Server wird aufgebaut...';
        }
        if (chatSend) {
            chatSend.disabled = !isConnected;
        }
    };
    const appendChatMessage = (msg) => {
        if (!chatMessages)
            return;
        const msgEl = document.createElement('div');
        msgEl.className = 'chat-message';
        const senderEl = document.createElement('span');
        senderEl.className = 'chat-sender';
        if (msg.sessionId === game.sessionId) {
            senderEl.classList.add('local');
        }
        senderEl.textContent = `${msg.senderName}: `;
        const textEl = document.createElement('span');
        textEl.className = 'chat-text';
        // Use textContent exclusively to guarantee XSS protection
        textEl.textContent = msg.text;
        msgEl.appendChild(senderEl);
        msgEl.appendChild(textEl);
        chatMessages.appendChild(msgEl);
        // Auto-scroll to bottom
        chatMessages.scrollTop = chatMessages.scrollHeight;
    };
    const appendSystemMessage = (text) => {
        if (!chatMessages)
            return;
        const msgEl = document.createElement('div');
        msgEl.className = 'chat-system-message';
        msgEl.textContent = text;
        chatMessages.appendChild(msgEl);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    };
    const appendSystemError = (errorMessage) => {
        if (!chatMessages)
            return;
        const errorEl = document.createElement('div');
        errorEl.className = 'chat-system-error';
        errorEl.textContent = errorMessage;
        chatMessages.appendChild(errorEl);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    };
    game.onConnectionStatusChanged = (status) => {
        updateInputState(status);
    };
    game.onChatMessage = (msg) => {
        appendChatMessage(msg);
    };
    game.onRoomChanged = (room) => {
        appendSystemMessage(`--- Raum gewechselt: ${room.name} ---`);
    };
    game.onActionChanged = (text) => {
        if (actionText) {
            actionText.textContent = text;
        }
    };
    updateInputState(game.connectionStatus);
    if (chatForm && chatInput) {
        chatForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const rawText = chatInput.value;
            if (!rawText || !rawText.trim())
                return;
            const trimmed = rawText.trim();
            chatInput.value = '';
            const result = await game.sendChatMessage(trimmed);
            if (!result.success) {
                appendSystemError(result.error || 'Nachricht konnte nicht gesendet werden.');
            }
        });
    }
    game.start();
    console.log('Adventure Chat game initialized with Room Chat support.');
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initialize);
}
else {
    initialize();
}
//# sourceMappingURL=main.js.map