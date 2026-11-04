import { AdventureGame } from './game.js';
import { ChatUiController } from './ui/chat-ui.js';
import type { ChatMessageDto } from './types.js';
import { chooseWorld, loadWorlds, showStartupError } from './ui/world-picker.js';

declare global {
  interface Window {
    __ADVENTURE_GAME__?: AdventureGame;
  }
}

async function initialize(): Promise<void> {
  const container = document.getElementById('game-container')!;
  container.hidden = true;
  const worlds = await loadWorlds();
  const selection = worlds.length ? await chooseWorld(worlds) : undefined;
  const worldId = selection?.worldId;
  const catalog = selection?.catalog;
  const participant = selection?.participant;
  const canvas = document.getElementById('game-canvas') as HTMLCanvasElement | null;
  if (!canvas) {
    throw new Error('Canvas element #game-canvas not found');
  }

  const game = new AdventureGame(canvas, undefined, participant?.name, worldId, participant?.characterId, catalog);
  window.__ADVENTURE_GAME__ = game;

  const chatUi = new ChatUiController(
    {},
    async (text: string) => {
      const result = await game.sendChatMessage(text);
      return { success: result.success, error: result.error };
    }
  );

  game.onConnectionStatusChanged = (status) => {
    chatUi.setConnectionStatus(status);
  };

  game.onChatMessage = (msg: ChatMessageDto) => {
    chatUi.appendChatMessage(msg, game.sessionId);
  };

  game.onRoomChanged = (room) => {
    chatUi.appendSystemMessage(`--- Raum gewechselt: ${room.name} ---`);
  };

  game.onActionChanged = (text) => {
    chatUi.setActionText(text);
  };

  chatUi.setConnectionStatus(game.connectionStatus);

  game.onWorldUnavailable = message => {
    sessionStorage.setItem('world-selection-error', message);
    location.reload();
  };
  if (worldId) {
    const leave = document.createElement('button');
    leave.id = 'world-leave';
    leave.textContent = 'Zur Weltwahl';
    leave.addEventListener('click', async () => {
      leave.disabled = true;
      try { await game.networkClient.leaveWorld(); }
      finally { location.reload(); }
    });
    container.prepend(leave);
  }
  container.hidden = false;
  try { await game.start(); }
  catch (error) { game.stop(); container.hidden = true; showStartupError(error); }

  console.log('Adventure Chat game initialized with modular architecture.');
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => { void initialize().catch(showStartupError); });
} else {
  void initialize().catch(showStartupError);
}
