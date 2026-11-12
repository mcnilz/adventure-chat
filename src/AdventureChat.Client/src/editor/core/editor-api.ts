import type {
  BackgroundsOverviewDto,
  RoomSummaryDto,
  RoomYamlDto,
  RoomView
} from './editor-types.js';
import type { ObjectStateDto, PreviewObjectStates, RoomSnapshotDto } from '../../types.js';

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
  public static worldId: string | undefined;

  private static request(path: string, init?: RequestInit): Promise<Response> {
    const url = new URL(path, location.origin);
    if (this.worldId) url.searchParams.set("worldId", this.worldId);
    return fetch(url, init);
  }

  public static backgroundUrl(path: string): string {
    return this.worldId && path.startsWith("assets/images/")
      ? `/world-assets/${encodeURIComponent(this.worldId)}/images/${path.slice(14)}` : path;
  }

  private static async authorToken(): Promise<string> {
    const response = await this.request('/api/editor/status');
    if (!response.ok) throw new Error(`Autorenmodus nicht verfügbar: HTTP ${response.status}`);
    const status = await response.json() as { token: string };
    return status.token;
  }
  public static async previewPath(room: RoomView, start: { x: number; y: number }, target: { x: number; y: number }, states: ReadonlyArray<ObjectStateDto> = []): Promise<{ success: boolean; waypoints: { x: number; y: number }[] | null }> {
    const response = await this.request('/api/editor/preview-path', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify({ room, start, target, states })
    });
    if (!response.ok) return { success: false, waypoints: null };
    return response.json();
  }

  public static async previewAction(room: RoomView, position: { x: number; y: number }, objectId: string, actionId: string,
    states: ReadonlyArray<PreviewObjectStates>): Promise<{ success: boolean; error: string | null; snapshot: RoomSnapshotDto; states: PreviewObjectStates[] }> {
    const response = await this.request('/api/editor/preview-action', {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify({ room, position, objectId, actionId, states })
    });
    if (!response.ok) throw new Error('Die Vorschauaktion konnte nicht geprüft werden.');
    return response.json();
  }

  public static async fetchRooms(): Promise<RoomSummaryDto[]> {
    const res = await this.request('/api/editor/rooms');
    if (!res.ok) {
      throw new Error(`Fehler beim Laden der Raumliste: HTTP ${res.status}`);
    }
    return (await res.json()) as RoomSummaryDto[];
  }

  public static async fetchRoom(id: string): Promise<RoomYamlDto> {
    const res = await this.request(`/api/editor/rooms/${encodeURIComponent(id)}`);
    if (!res.ok) {
      throw new Error(`Fehler beim Laden des Raumes '${id}': HTTP ${res.status}`);
    }
    return (await res.json()) as RoomYamlDto;
  }

  public static async validateRoom(room: RoomView): Promise<ValidationResponse> {
    const res = await this.request('/api/editor/validate', {
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
    const res = await this.request('/api/editor/validate-world');
    if (!res.ok) {
      throw new Error(`Welt-Validierungsdienst antwortete mit Status ${res.status}`);
    }

    return (await res.json()) as ValidationResponse;
  }

  public static async validateWorldPayload(rooms: readonly RoomView[]): Promise<ValidationResponse> {
    const res = await this.request('/api/editor/validate-world', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify(rooms)
    });

    if (!res.ok) {
      throw new Error(`Welt-Validierungsdienst antwortete mit Status ${res.status}`);
    }

    return (await res.json()) as ValidationResponse;
  }

  public static async saveRoom(room: RoomView): Promise<SaveRoomResponse> {
    const res = await this.request(`/api/editor/rooms/${encodeURIComponent(room.id)}`, {
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
    const res = await this.request('/api/editor/backgrounds');
    if (!res.ok) {
      throw new Error(`Fehler beim Laden der Hintergrundbilder: HTTP ${res.status}`);
    }
    return (await res.json()) as BackgroundsOverviewDto;
  }

  public static async uploadBackground(file: File): Promise<UploadBackgroundResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const res = await this.request('/api/editor/backgrounds', {
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
