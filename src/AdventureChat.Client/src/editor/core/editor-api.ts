import type {
  BackgroundsOverviewDto,
  RoomSummaryDto,
  RoomYamlDto
} from './editor-types.js';

export interface ValidationResponse {
  valid: boolean;
  errors: string[];
}

export interface SaveRoomResponse {
  success: boolean;
  roomId?: string;
  errors: string[];
}

export interface UploadBackgroundResponse {
  success: boolean;
  name?: string;
  path?: string;
  error?: string;
}

export class EditorApi {
  private static async authorToken(): Promise<string> {
    const response = await fetch('/api/editor/status');
    if (!response.ok) throw new Error(`Autorenmodus nicht verfügbar: HTTP ${response.status}`);
    const status = await response.json() as { token: string };
    return status.token;
  }
  public static async fetchRooms(): Promise<RoomSummaryDto[]> {
    const res = await fetch('/api/editor/rooms');
    if (!res.ok) {
      throw new Error(`Fehler beim Laden der Raumliste: HTTP ${res.status}`);
    }
    return (await res.json()) as RoomSummaryDto[];
  }

  public static async fetchRoom(id: string): Promise<RoomYamlDto> {
    const res = await fetch(`/api/editor/rooms/${encodeURIComponent(id)}`);
    if (!res.ok) {
      throw new Error(`Fehler beim Laden des Raumes '${id}': HTTP ${res.status}`);
    }
    return (await res.json()) as RoomYamlDto;
  }

  public static async validateRoom(room: RoomYamlDto): Promise<ValidationResponse> {
    const res = await fetch('/api/editor/validate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify(room)
    });

    if (!res.ok) {
      throw new Error(`Validierungsdienst antwortete mit Status ${res.status}`);
    }

    return (await res.json()) as ValidationResponse;
  }

  public static async validateWorld(): Promise<ValidationResponse> {
    const res = await fetch('/api/editor/validate-world');
    if (!res.ok) {
      throw new Error(`Welt-Validierungsdienst antwortete mit Status ${res.status}`);
    }

    return (await res.json()) as ValidationResponse;
  }

  public static async validateWorldPayload(rooms: RoomYamlDto[]): Promise<ValidationResponse> {
    const res = await fetch('/api/editor/validate-world', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify(rooms)
    });

    if (!res.ok) {
      throw new Error(`Welt-Validierungsdienst antwortete mit Status ${res.status}`);
    }

    return (await res.json()) as ValidationResponse;
  }

  public static async saveRoom(room: RoomYamlDto): Promise<SaveRoomResponse> {
    const res = await fetch(`/api/editor/rooms/${encodeURIComponent(room.id)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify(room)
    });

    if (!res.ok) {
      const errorJson = await res.json().catch(() => ({}));
      const errors = (errorJson.errors as string[]) ?? [`HTTP ${res.status}: Speichern fehlgeschlagen.`];
      return { success: false, errors };
    }

    return (await res.json()) as SaveRoomResponse;
  }

  public static async fetchBackgrounds(): Promise<BackgroundsOverviewDto> {
    const res = await fetch('/api/editor/backgrounds');
    if (!res.ok) {
      throw new Error(`Fehler beim Laden der Hintergrundbilder: HTTP ${res.status}`);
    }
    return (await res.json()) as BackgroundsOverviewDto;
  }

  public static async uploadBackground(file: File): Promise<UploadBackgroundResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const res = await fetch('/api/editor/backgrounds', {
      method: 'POST',
      headers: { 'X-Author-Token': await this.authorToken() },
      body: formData
    });

    if (!res.ok) {
      const errJson = await res.json().catch(() => ({}));
      return {
        success: false,
        error: errJson.error ?? `Upload fehlgeschlagen (HTTP ${res.status})`
      };
    }

    return (await res.json()) as UploadBackgroundResponse;
  }
}
