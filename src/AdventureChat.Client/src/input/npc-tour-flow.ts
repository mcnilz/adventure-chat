import type { NpcTourStatus, RoomSnapshotDto } from '../types.js';

export class NpcTourFlow {
  public active = false;
  public changingRoom = false;

  public constructor(
    private readonly sessionId: () => string,
    private readonly show: (status: NpcTourStatus, starting: boolean) => void,
    private readonly enter: (snapshot: RoomSnapshotDto) => void,
  ) {}

  public update(status: NpcTourStatus): void {
    if (status.sessionId !== this.sessionId()) return;
    const starting = status.active && !this.active;
    this.active = status.active;
    this.show(status, starting);
  }

  public beginRoomChange(): void {
    this.changingRoom = this.active;
  }

  public finishRoomChange(snapshot: RoomSnapshotDto): void {
    if (!this.changingRoom || snapshot.yourSessionId !== this.sessionId()) return;
    this.changingRoom = false;
    this.enter(snapshot);
  }

  public interrupt(): void {
    this.changingRoom = false;
    if (this.active) this.update({
      sessionId: this.sessionId(), active: false,
      message: 'Die Führung endete, weil die Verbindung unterbrochen wurde.', speech: null,
    });
  }
}
