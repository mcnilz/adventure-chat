export class EditorApi {
    static async fetchRooms() {
        const res = await fetch('/api/editor/rooms');
        if (!res.ok) {
            throw new Error(`Fehler beim Laden der Raumliste: HTTP ${res.status}`);
        }
        return (await res.json());
    }
    static async fetchRoom(id) {
        const res = await fetch(`/api/editor/rooms/${encodeURIComponent(id)}`);
        if (!res.ok) {
            throw new Error(`Fehler beim Laden des Raumes '${id}': HTTP ${res.status}`);
        }
        return (await res.json());
    }
    static async validateRoom(room) {
        const res = await fetch('/api/editor/validate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(room)
        });
        if (!res.ok) {
            throw new Error(`Validierungsdienst antwortete mit Status ${res.status}`);
        }
        return (await res.json());
    }
    static async saveRoom(room) {
        const res = await fetch(`/api/editor/rooms/${encodeURIComponent(room.id)}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(room)
        });
        if (!res.ok) {
            const errorJson = await res.json().catch(() => ({}));
            const errors = errorJson.errors ?? [`HTTP ${res.status}: Speichern fehlgeschlagen.`];
            return { success: false, errors };
        }
        return (await res.json());
    }
    static async fetchBackgrounds() {
        const res = await fetch('/api/editor/backgrounds');
        if (!res.ok) {
            throw new Error(`Fehler beim Laden der Hintergrundbilder: HTTP ${res.status}`);
        }
        return (await res.json());
    }
    static async uploadBackground(file) {
        const formData = new FormData();
        formData.append('file', file);
        const res = await fetch('/api/editor/backgrounds', {
            method: 'POST',
            body: formData
        });
        if (!res.ok) {
            const errJson = await res.json().catch(() => ({}));
            return {
                success: false,
                error: errJson.error ?? `Upload fehlgeschlagen (HTTP ${res.status})`
            };
        }
        return (await res.json());
    }
}
//# sourceMappingURL=editor-api.js.map