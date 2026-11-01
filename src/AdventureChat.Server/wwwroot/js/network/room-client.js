export class RoomNetworkClient {
    events;
    connectionStatus = 'disconnected';
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    hubConnection = null;
    constructor(events) {
        this.events = events;
    }
    async connect() {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
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
            this.hubConnection.on('UserJoined', (dto) => {
                this.events.onUserJoined(dto);
            });
            this.hubConnection.on('UserLeft', (dto) => {
                this.events.onUserLeft(dto);
            });
            this.hubConnection.on('PlayerMoved', (dto) => {
                this.events.onPlayerMoved(dto);
            });
            this.hubConnection.on('ChatMessageReceived', (dto) => {
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
        }
        catch (e) {
            console.error('Failed to initialize SignalR connection:', e);
            this.setStatus('disconnected');
            return false;
        }
    }
    disconnect() {
        if (this.hubConnection) {
            try {
                this.hubConnection.stop();
            }
            catch {
                // ignore disconnect errors
            }
        }
    }
    setStatus(status) {
        this.connectionStatus = status;
        this.events.onConnectionStatusChanged(status);
    }
    async joinRoom(roomId, displayName) {
        if (!this.hubConnection) {
            return { success: false, error: 'No connection', snapshot: null };
        }
        try {
            const result = await this.hubConnection.invoke('JoinRoom', {
                roomId,
                displayName,
            });
            if (result.success) {
                this.setStatus('connected');
            }
            else {
                this.setStatus('disconnected');
            }
            return result;
        }
        catch (e) {
            this.setStatus('disconnected');
            return { success: false, error: String(e), snapshot: null };
        }
    }
    async changeRoom(targetRoomId, targetSpawnId) {
        if (!this.hubConnection || this.connectionStatus !== 'connected') {
            return { success: false, error: 'Not connected', snapshot: null };
        }
        try {
            const result = await this.hubConnection.invoke('ChangeRoom', {
                targetRoomId,
                targetSpawnId: targetSpawnId || null,
            });
            return result;
        }
        catch (e) {
            return { success: false, error: String(e), snapshot: null };
        }
    }
    async requestMove(start, target) {
        if (!this.hubConnection || this.connectionStatus !== 'connected') {
            return { success: false, error: 'Not connected' };
        }
        try {
            const result = await this.hubConnection.invoke('RequestMove', {
                start: { x: start.x, y: start.y },
                target: { x: target.x, y: target.y },
            });
            return result;
        }
        catch (e) {
            return { success: false, error: String(e) };
        }
    }
    async sendChatMessage(text) {
        if (!this.hubConnection || this.connectionStatus !== 'connected') {
            return { success: false, error: 'Nicht mit dem Server verbunden.', message: null };
        }
        try {
            const result = await this.hubConnection.invoke('SendChatMessage', {
                text,
            });
            return result;
        }
        catch (e) {
            return { success: false, error: String(e), message: null };
        }
    }
    async calculatePathFallback(roomId, start, target) {
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
        }
        catch (e) {
            return { success: false, error: String(e) };
        }
    }
}
//# sourceMappingURL=room-client.js.map