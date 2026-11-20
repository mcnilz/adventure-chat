import type {
  BackgroundsOverviewDto,
  RoomSummaryDto,
  RoomYamlDto,
  RoomView
} from './editor-types.js';
import type { ObjectActionResultDto, ObjectStateDto, Point, PreviewObjectStates, RoomSnapshotDto } from '../../types.js';

interface PreviewLocation {
  room: RoomView;
  position: Point;
  roomId: string;
  states: readonly PreviewObjectStates[];
  targetFormat?: string;
}

export interface PreviewResult extends ObjectActionResultDto {
  snapshot: RoomSnapshotDto;
  states: PreviewObjectStates[];
}

export interface ValidationResponse {
  valid: boolean;
  errors: string[];
}

export interface SaveRoomResponse {
  success: boolean;
  roomId?: string;
  errors: string[];
  formatVersion?: string | null;
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
    return (await this.status()).token;
  }

  public static async status(): Promise<{ token: string; formatVersion?: string | null }> {
    const response = await this.request('/api/editor/status');
    if (!response.ok) throw new Error(`Autorenmodus nicht verfügbar: HTTP ${response.status}`);
    return response.json();
  }
  public static async previewPath(room: RoomView, start: Point, target: Point,
    states: readonly ObjectStateDto[] = [], targetFormat?: string): Promise<{ success: boolean; waypoints: Point[] | null }> {
    const response = await this.request('/api/editor/preview-path', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify({ room, start, target, states, targetFormat })
    });
    if (!response.ok) return { success: false, waypoints: null };
    return response.json();
  }

  public static previewAction(location: PreviewLocation, objectId: string, actionId: string): Promise<PreviewResult> {
    return this.previewCommand('action', { ...location, objectId, actionId });
  }

  public static previewExit(location: PreviewLocation, exitId: string): Promise<PreviewResult> {
    return this.previewCommand('exit', { ...location, exitId });
  }

  public static previewSnapshot(location: PreviewLocation): Promise<PreviewResult> {
    return this.previewCommand('snapshot', location);
  }

  private static async previewCommand(command: 'action' | 'exit' | 'snapshot', body: object): Promise<PreviewResult> {
    const response = await this.request(`/api/editor/preview-${command}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Author-Token': await this.authorToken() },
      body: JSON.stringify(body)
    });
    if (!response.ok) {
      const result = await response.json() as { errors?: string[] };
      throw new Error(result.errors?.join(' ') ?? 'Der Vorschau-Befehl konnte nicht geprüft werden.');
    }
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

  public static async validateRoom(room: RoomView, targetFormat?: string): Promise<ValidationResponse> {
    const query = new URLSearchParams({ includeWorld: 'true' });
    if (targetFormat) query.set('targetFormat', targetFormat);
    const res = await this.request(`/api/editor/validate?${query}`, {
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

  public static async saveRoom(room: RoomView, upgradeFrom?: string, targetFormat?: string): Promise<SaveRoomResponse> {
    const params = new URLSearchParams();
    if (upgradeFrom) params.set('upgradeFrom', upgradeFrom);
    if (targetFormat) params.set('targetFormat', targetFormat);
    const query = params.size ? `?${params}` : '';
    const res = await this.request(`/api/editor/rooms/${encodeURIComponent(room.id)}${query}`, {
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
