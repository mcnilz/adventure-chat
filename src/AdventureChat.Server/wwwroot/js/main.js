import { AdventureGame } from './game.js';
function initialize() {
    const canvas = document.getElementById('game-canvas');
    if (!canvas) {
        throw new Error('Canvas element #game-canvas not found');
    }
    const game = new AdventureGame(canvas);
    game.start();
    window.__ADVENTURE_GAME__ = game;
    const chatContainer = document.getElementById('chat-container');
    if (chatContainer) {
        chatContainer.addEventListener('click', (e) => {
            e.stopPropagation();
        });
    }
    console.log('Adventure Chat game initialized.');
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initialize);
}
else {
    initialize();
}
//# sourceMappingURL=main.js.map