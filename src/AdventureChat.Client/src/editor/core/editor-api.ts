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
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(room)
    });

    if (!res.ok) {
      throw new Error(`Validierungsdienst antwortete mit Status ${res.status}`);
    }

    return (await res.json()) as ValidationResponse;
  }

  public static async saveRoom(room: RoomYamlDto): Promise<SaveRoomResponse> {
    const res = await fetch(`/api/editor/rooms/${encodeURIComponent(room.id)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
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
