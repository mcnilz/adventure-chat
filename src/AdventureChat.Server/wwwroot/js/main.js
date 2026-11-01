import { AdventureGame } from './game.js';
import { ChatUiController } from './ui/chat-ui.js';
function initialize() {
    const canvas = document.getElementById('game-canvas');
    if (!canvas) {
        throw new Error('Canvas element #game-canvas not found');
    }
    const game = new AdventureGame(canvas);
    window.__ADVENTURE_GAME__ = game;
    const chatUi = new ChatUiController({}, async (text) => {
        const result = await game.sendChatMessage(text);
        return { success: result.success, error: result.error };
    });
    game.onConnectionStatusChanged = (status) => {
        chatUi.setConnectionStatus(status);
    };
    game.onChatMessage = (msg) => {
        chatUi.appendChatMessage(msg, game.sessionId);
    };
    game.onRoomChanged = (room) => {
        chatUi.appendSystemMessage(`--- Raum gewechselt: ${room.name} ---`);
    };
    game.onActionChanged = (text) => {
        chatUi.setActionText(text);
    };
    chatUi.setConnectionStatus(game.connectionStatus);
    void game.start();
    console.log('Adventure Chat game initialized with modular architecture.');
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initialize);
}
else {
    initialize();
}
//# sourceMappingURL=main.js.map