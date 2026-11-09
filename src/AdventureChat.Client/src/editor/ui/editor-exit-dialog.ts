import { EditorApi } from '../core/editor-api.js';
import type { RoomExitDto, RoomPointDto, RoomSummaryDto, RoomView } from '../core/editor-types.js';

export interface ExitDialogElements {
  modalExitDialog: HTMLDialogElement;
  btnCloseExitModal: HTMLButtonElement;
  btnCancelExitModal: HTMLButtonElement;
  btnConfirmExitModal: HTMLButtonElement;
  modalExitId: HTMLInputElement;
  modalExitRoom: HTMLSelectElement;
  modalExitTargetSpawn: HTMLSelectElement;
  modalExitLabel: HTMLInputElement;
  modalExitRadius: HTMLInputElement;
}

export class EditorExitDialog {
  private readonly roomCache = new Map<string, RoomView>();
  private roomSummaries: RoomSummaryDto[] = [];
  private pendingPos: RoomPointDto | null = null;
  private onConfirmCallback: ((exitDto: RoomExitDto) => void) | null = null;

  public constructor(
    private readonly elements: ExitDialogElements,
    private readonly onStatusMessage: (msg: string, type: 'info' | 'success' | 'error') => void
  ) {
    this.bindEvents();
  }

  public async refreshRooms(): Promise<void> {
    try {
      this.roomSummaries = await EditorApi.fetchRooms();
    } catch {
      // Ignore network failure, keep previous summaries
    }
  }

  public updateCachedRoom(room: RoomView): void {
    this.roomCache.set(room.id, JSON.parse(JSON.stringify(room)) as RoomView);
    const existingSummary = this.roomSummaries.find(r => r.id === room.id);
    if (existingSummary) {
      existingSummary.name = room.name;
      existingSummary.background = room.background;
      existingSummary.width = room.size.width;
      existingSummary.height = room.size.height;
    } else {
      this.roomSummaries.push({
        id: room.id,
        name: room.name,
        background: room.background,
        width: room.size.width,
        height: room.size.height
      });
    }
  }

  public async initRooms(): Promise<void> {
    await this.refreshRooms();
  }

  public async populateRoomsSelect(
    selectEl: HTMLSelectElement,
    selectedRoomId?: string,
    excludeRoomId?: string
  ): Promise<void> {
    if (this.roomSummaries.length === 0) {
      await this.refreshRooms();
    }

    selectEl.innerHTML = '';
    const filtered = this.roomSummaries.filter(r => r.id !== excludeRoomId);
    const roomsToShow = filtered.length > 0 ? filtered : this.roomSummaries;

    for (const r of roomsToShow) {
      const opt = document.createElement('option');
      opt.value = r.id;
      opt.textContent = `${r.name} (${r.id})`;
      selectEl.appendChild(opt);
    }

    if (selectedRoomId && Array.from(selectEl.options).some(o => o.value === selectedRoomId)) {
      selectEl.value = selectedRoomId;
    } else if (selectEl.options.length > 0) {
      selectEl.selectedIndex = 0;
    }
  }

  public async populateSpawnsSelect(
    selectEl: HTMLSelectElement,
    targetRoomId: string,
    selectedSpawnId?: string
  ): Promise<void> {
    selectEl.innerHTML = '';
    let room = this.roomCache.get(targetRoomId);

    if (!room) {
      room = await EditorApi.fetchRoom(targetRoomId).catch(() => null) ?? undefined;
      if (room) this.roomCache.set(targetRoomId, room);
    }

    const spawns = (room?.spawns && room.spawns.length > 0)
      ? room.spawns
      : [{ id: 'entry', position: { x: 320, y: 240 } }];

    for (const sp of spawns) {
      const opt = document.createElement('option');
      opt.value = sp.id;
      opt.textContent = `📍 ${sp.id} (${sp.position.x}, ${sp.position.y})`;
      selectEl.appendChild(opt);
    }

    if (selectedSpawnId && Array.from(selectEl.options).some(o => o.value === selectedSpawnId)) {
      selectEl.value = selectedSpawnId;
    } else if (selectEl.options.length > 0) {
      selectEl.selectedIndex = 0;
    }
  }

  public async openForNewExit(
    pos: RoomPointDto,
    currentRoomId: string,
    onConfirm: (exitDto: RoomExitDto) => void
  ): Promise<void> {
    this.pendingPos = pos;
    this.onConfirmCallback = onConfirm;

    // Always fetch latest list of rooms so newly saved rooms are instantly available
    await this.refreshRooms();

    const { elements: el } = this;
    const defaultTarget = currentRoomId === 'lounge' ? 'garden' : 'lounge';

    await this.populateRoomsSelect(el.modalExitRoom, defaultTarget, currentRoomId);
    const targetRoomId = el.modalExitRoom.value || defaultTarget;
    await this.populateSpawnsSelect(el.modalExitTargetSpawn, targetRoomId, 'entry');

    el.modalExitId.value = `to-${targetRoomId}`;
    el.modalExitLabel.value = `Nach ${targetRoomId}`;
    el.modalExitRadius.value = '35';

    el.modalExitDialog.showModal();
  }

  public close(): void {
    this.elements.modalExitDialog.close();
    this.pendingPos = null;
    this.onConfirmCallback = null;
  }

  private bindEvents(): void {
    const { elements: el } = this;

    el.btnCloseExitModal.addEventListener('click', () => this.close());
    el.btnCancelExitModal.addEventListener('click', () => this.close());

    el.modalExitRoom.addEventListener('change', async () => {
      const targetRoom = el.modalExitRoom.value;
      await this.populateSpawnsSelect(el.modalExitTargetSpawn, targetRoom);
      if (!el.modalExitId.value.trim() || el.modalExitId.value.startsWith('to-')) {
        el.modalExitId.value = `to-${targetRoom}`;
      }
      if (!el.modalExitLabel.value.trim() || el.modalExitLabel.value.startsWith('Nach ')) {
        el.modalExitLabel.value = `Nach ${targetRoom}`;
      }
    });

    el.btnConfirmExitModal.addEventListener('click', () => {
      if (!this.pendingPos || !this.onConfirmCallback) return;

      const exitId = el.modalExitId.value.trim();
      if (!exitId) {
        this.onStatusMessage('Bitte eine Ausgangs-ID angeben.', 'error');
        return;
      }

      const targetRoom = el.modalExitRoom.value.trim();
      const targetSpawn = el.modalExitTargetSpawn.value.trim() || 'entry';
      const label = el.modalExitLabel.value.trim() || null;
      const radius = parseInt(el.modalExitRadius.value, 10) || 35;

      const newExit: RoomExitDto = {
        id: exitId,
        position: { x: this.pendingPos.x, y: this.pendingPos.y },
        radius: Math.max(5, Math.min(200, radius)),
        target: {
          room: targetRoom,
          spawn: targetSpawn
        },
        label
      };

      const cb = this.onConfirmCallback;
      this.close();
      cb(newExit);
    });
  }
}
