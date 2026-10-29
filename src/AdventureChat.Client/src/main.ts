import { AdventureGame } from './game.js';

declare global {
  interface Window {
    __ADVENTURE_GAME__?: AdventureGame;
  }
}

function initialize(): void {
  const canvas = document.getElementById('game-canvas') as HTMLCanvasElement | null;
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
} else {
  initialize();
}
