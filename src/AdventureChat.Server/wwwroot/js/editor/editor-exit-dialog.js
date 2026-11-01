import { EditorApi } from './editor-api.js';
export class EditorExitDialog {
    elements;
    onStatusMessage;
    roomCache = new Map();
    roomSummaries = [];
    pendingPos = null;
    onConfirmCallback = null;
    constructor(elements, onStatusMessage) {
        this.elements = elements;
        this.onStatusMessage = onStatusMessage;
        this.bindEvents();
    }
    async initRooms() {
        try {
            this.roomSummaries = await EditorApi.fetchRooms();
            for (const r of this.roomSummaries) {
                if (!this.roomCache.has(r.id)) {
                    const fullRoom = await EditorApi.fetchRoom(r.id).catch(() => null);
                    if (fullRoom)
                        this.roomCache.set(r.id, fullRoom);
                }
            }
        }
        catch {
            // Ignore initial load failure; will retry on open
        }
    }
    async populateRoomsSelect(selectEl, selectedRoomId, excludeRoomId) {
        if (this.roomSummaries.length === 0) {
            await this.initRooms();
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
        }
        else if (selectEl.options.length > 0) {
            selectEl.selectedIndex = 0;
        }
    }
    async populateSpawnsSelect(selectEl, targetRoomId, selectedSpawnId) {
        selectEl.innerHTML = '';
        let room = this.roomCache.get(targetRoomId);
        if (!room) {
            room = await EditorApi.fetchRoom(targetRoomId).catch(() => null) ?? undefined;
            if (room)
                this.roomCache.set(targetRoomId, room);
        }
        const spawns = room?.spawns ?? [{ id: 'entry', position: { x: 320, y: 240 } }];
        for (const sp of spawns) {
            const opt = document.createElement('option');
            opt.value = sp.id;
            opt.textContent = `📍 ${sp.id} (${sp.position.x}, ${sp.position.y})`;
            selectEl.appendChild(opt);
        }
        if (selectedSpawnId && Array.from(selectEl.options).some(o => o.value === selectedSpawnId)) {
            selectEl.value = selectedSpawnId;
        }
        else if (selectEl.options.length > 0) {
            selectEl.selectedIndex = 0;
        }
    }
    async openForNewExit(pos, currentRoomId, onConfirm) {
        this.pendingPos = pos;
        this.onConfirmCallback = onConfirm;
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
    close() {
        this.elements.modalExitDialog.close();
        this.pendingPos = null;
        this.onConfirmCallback = null;
    }
    bindEvents() {
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
            if (!this.pendingPos || !this.onConfirmCallback)
                return;
            const exitId = el.modalExitId.value.trim();
            if (!exitId) {
                this.onStatusMessage('Bitte eine Ausgangs-ID angeben.', 'error');
                return;
            }
            const targetRoom = el.modalExitRoom.value.trim();
            const targetSpawn = el.modalExitTargetSpawn.value.trim() || 'entry';
            const label = el.modalExitLabel.value.trim() || null;
            const radius = parseInt(el.modalExitRadius.value, 10) || 35;
            const newExit = {
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
//# sourceMappingURL=editor-exit-dialog.js.map