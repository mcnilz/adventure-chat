import type {
  ChangeRoomResultDto,
  ChatMessageDto,
  JoinRoomResultDto,
  MoveResultDto,
  PlayerMovedDto,
  Point,
  SendChatMessageResultDto,
  UserJoinedDto,
  UserLeftDto,
} from '../types.js';
import type { HubConnection } from '@microsoft/signalr';

declare global {
  interface Window { signalR?: typeof import('@microsoft/signalr'); }
}

export interface RoomClientEvents {
  onUserJoined: (dto: UserJoinedDto) => void;
  onUserLeft: (dto: UserLeftDto) => void;
  onPlayerMoved: (dto: PlayerMovedDto) => void;
  onChatMessageReceived: (dto: ChatMessageDto) => void;
  onConnectionStatusChanged: (status: 'connected' | 'connecting' | 'disconnected') => void;
  onReconnected: () => Promise<void>;
}

export class RoomNetworkClient {
  public connectionStatus: 'connected' | 'connecting' | 'disconnected' = 'disconnected';
  private hubConnection: HubConnection | null = null;

  public constructor(private readonly events: RoomClientEvents) {}

  public async connect(): Promise<boolean> {
    const signalR = typeof window !== 'undefined' ? window.signalR : null;
    if (!signalR) {
      console.warn('SignalR library not found on window. Running in standalone mode.');
      return false;
    }

    try {
      this.setStatus('connecting');
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('/hub/room')
        .withAutomaticReconnect()
        .build();

      this.hubConnection.on('UserJoined', (dto: UserJoinedDto) => {
        this.events.onUserJoined(dto);
      });

      this.hubConnection.on('UserLeft', (dto: UserLeftDto) => {
        this.events.onUserLeft(dto);
      });

      this.hubConnection.on('PlayerMoved', (dto: PlayerMovedDto) => {
        this.events.onPlayerMoved(dto);
      });

      this.hubConnection.on('ChatMessageReceived', (dto: ChatMessageDto) => {
        this.events.onChatMessageReceived(dto);
      });

      this.hubConnection.onreconnecting(() => {
        this.setStatus('connecting');
      });

      this.hubConnection.onreconnected(async () => {
        await this.events.onReconnected();
      });

      this.hubConnection.onclose(() => {
        this.setStatus('disconnected');
      });

      await this.hubConnection.start();
      return true;
    } catch (e) {
      console.error('Failed to initialize SignalR connection:', e);
      this.setStatus('disconnected');
      return false;
    }
  }

  public disconnect(): void {
    if (this.hubConnection) {
      try {
        this.hubConnection.stop();
      } catch {
        // ignore disconnect errors
      }
    }
  }

  public setStatus(status: 'connected' | 'connecting' | 'disconnected'): void {
    this.connectionStatus = status;
    this.events.onConnectionStatusChanged(status);
  }

  public async joinRoom(roomId: string, displayName: string): Promise<JoinRoomResultDto> {
    if (!this.hubConnection) {
      return { success: false, error: 'No connection', snapshot: null };
    }

    try {
      const result: JoinRoomResultDto = await this.hubConnection.invoke('JoinRoom', {
        roomId,
        displayName,
      });

      if (result.success) {
        this.setStatus('connected');
      } else {
        this.setStatus('disconnected');
      }
      return result;
    } catch (e) {
      this.setStatus('disconnected');
      return { success: false, error: String(e), snapshot: null };
    }
  }

  public async changeRoom(exitId: string): Promise<ChangeRoomResultDto> {
    if (!this.hubConnection || this.connectionStatus !== 'connected') {
      return { success: false, error: 'Not connected', snapshot: null };
    }

    try {
      const result: ChangeRoomResultDto = await this.hubConnection.invoke('ChangeRoom', {
        exitId,
      });
      return result;
    } catch (e) {
      return { success: false, error: String(e), snapshot: null };
    }
  }

  public async requestMove(start: Point, target: Point): Promise<MoveResultDto> {
    if (!this.hubConnection || this.connectionStatus !== 'connected') {
      return { success: false, error: 'Not connected' };
    }

    try {
      const result: MoveResultDto = await this.hubConnection.invoke('RequestMove', {
        start: { x: start.x, y: start.y },
        target: { x: target.x, y: target.y },
      });
      return result;
    } catch (e) {
      return { success: false, error: String(e) };
    }
  }

  public async sendChatMessage(text: string): Promise<SendChatMessageResultDto> {
    if (!this.hubConnection || this.connectionStatus !== 'connected') {
      return { success: false, error: 'Nicht mit dem Server verbunden.', message: null };
    }

    try {
      const result: SendChatMessageResultDto = await this.hubConnection.invoke('SendChatMessage', {
        text,
      });
      return result;
    } catch (e) {
      return { success: false, error: String(e), message: null };
    }
  }

  public async calculatePathFallback(roomId: string, start: Point, target: Point): Promise<{ success: boolean; waypoints?: Point[]; error?: string }> {
    try {
      const response = await fetch(`/api/rooms/${roomId}/path`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          start,
          target,
        }),
      });

      if (!response.ok) {
        const errData = await response.json().catch(() => ({}));
        return { success: false, error: errData.error || 'Server rejected target' };
      }

      const data = await response.json();
      if (!data.success || !Array.isArray(data.waypoints) || data.waypoints.length === 0) {
        return { success: false, error: data.error || 'No path found' };
      }

      return { success: true, waypoints: data.waypoints };
    } catch (e) {
      return { success: false, error: String(e) };
    }
  }
}
