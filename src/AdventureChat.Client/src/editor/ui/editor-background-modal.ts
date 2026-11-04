import { EditorApi } from '../core/editor-api.js';
import type { BackgroundItemDto } from '../core/editor-types.js';

export interface BackgroundModalElements {
  btnOpenBgModal: HTMLButtonElement;
  btnUploadBgTrigger: HTMLButtonElement;
  inputBgFile: HTMLInputElement;
  bgFsHintPath: HTMLElement;
  modalBgPicker: HTMLDialogElement;
  btnCloseBgModal: HTMLButtonElement;
  btnCloseBgModalFooter: HTMLButtonElement;
  modalFsPath: HTMLElement;
  btnCopyFsPath: HTMLButtonElement;
  bgDropZone: HTMLElement;
  btnModalUploadSelect: HTMLButtonElement;
  bgGalleryGrid: HTMLElement;
  bgCount: HTMLElement;
}

export class EditorBackgroundModal {
  public constructor(
    private readonly elements: BackgroundModalElements,
    private readonly onSelectBackground: (path: string) => void,
    private readonly onStatusMessage: (message: string, type: 'info' | 'success' | 'error') => void,
    private readonly getCurrentBackground?: () => string
  ) {
    this.bindEvents();
  }

  public async loadBackgroundsOverview(): Promise<void> {
    try {
      const data = await EditorApi.fetchBackgrounds();
      this.elements.bgFsHintPath.textContent = data.relativePath || 'assets/images';
      this.elements.modalFsPath.textContent = data.filesystemDirectory || data.relativePath;
      this.renderGalleryGrid(data.backgrounds);
    } catch {
      this.elements.bgFsHintPath.textContent = 'assets/images';
    }
  }

  public open(): void {
    this.elements.modalBgPicker.showModal();
    this.loadBackgroundsOverview().catch(() => {});
  }

  public close(): void {
    this.elements.modalBgPicker.close();
  }

  public async uploadFile(file: File): Promise<void> {
    this.onStatusMessage(`Lade '${file.name}' hoch...`, 'info');
    try {
      const res = await EditorApi.uploadBackground(file);
      if (res.success && res.path) {
        this.onStatusMessage(`Hintergrundbild '${res.name}' erfolgreich hochgeladen.`, 'success');
        this.onSelectBackground(res.path);
        await this.loadBackgroundsOverview();
      } else {
        this.onStatusMessage(`Upload fehlgeschlagen: ${res.error ?? 'Unbekannter Fehler'}`, 'error');
      }
    } catch (err) {
      this.onStatusMessage(`Upload-Fehler: ${String(err)}`, 'error');
    }
  }

  private bindEvents(): void {
    const { elements: el } = this;

    el.btnOpenBgModal.addEventListener('click', () => this.open());
    el.btnCloseBgModal.addEventListener('click', () => this.close());
    el.btnCloseBgModalFooter.addEventListener('click', () => this.close());

    // File input triggers
    el.btnUploadBgTrigger.addEventListener('click', () => el.inputBgFile.click());
    el.btnModalUploadSelect.addEventListener('click', () => el.inputBgFile.click());

    el.inputBgFile.addEventListener('change', () => {
      const file = el.inputBgFile.files?.[0];
      if (file) {
        this.uploadFile(file).catch(() => {});
        el.inputBgFile.value = '';
      }
    });

    // Copy FS Path
    el.btnCopyFsPath.addEventListener('click', async () => {
      const pathText = el.modalFsPath.textContent ?? '';
      if (pathText) {
        try {
          await navigator.clipboard.writeText(pathText);
          el.btnCopyFsPath.textContent = '✅ Kopiert!';
          setTimeout(() => {
            el.btnCopyFsPath.textContent = '📋 Pfad kopieren';
          }, 2000);
        } catch {
          this.onStatusMessage('Pfad konnte nicht in die Zwischenablage kopiert werden.', 'error');
        }
      }
    });

    // Drag & Drop on Dropzone
    this.bindDropZone();
  }

  private bindDropZone(): void {
    const { bgDropZone } = this.elements;

    bgDropZone.addEventListener('dragover', (e) => {
      e.preventDefault();
      bgDropZone.classList.add('drag-over');
    });

    bgDropZone.addEventListener('dragleave', () => {
      bgDropZone.classList.remove('drag-over');
    });

    bgDropZone.addEventListener('drop', (e) => {
      e.preventDefault();
      bgDropZone.classList.remove('drag-over');
      const file = e.dataTransfer?.files?.[0];
      if (file) {
        this.uploadFile(file).catch(() => {});
      }
    });
  }

  private renderGalleryGrid(items: BackgroundItemDto[]): void {
    const { bgGalleryGrid, bgCount } = this.elements;
    bgCount.textContent = String(items.length);
    bgGalleryGrid.innerHTML = '';

    if (items.length === 0) {
      bgGalleryGrid.innerHTML = '<p class="text-muted text-center" style="grid-column: 1/-1;">Keine Bilder im Ordner vorhanden.</p>';
      return;
    }

    const currentBg = this.getCurrentBackground?.() ?? '';

    for (const item of items) {
      const card = document.createElement('div');
      card.className = 'bg-gallery-card';
      if (currentBg && (currentBg === item.path || currentBg.endsWith(`/${item.name}`))) {
        card.classList.add('active');
      }
      card.setAttribute('data-path', item.path);
      card.title = `Klicken, um '${item.name}' als Hintergrund zu wählen`;

      const thumbWrapper = document.createElement('div');
      thumbWrapper.className = 'bg-gallery-thumb-wrapper bg-thumbnail-wrap';

      const img = document.createElement('img');
      img.className = 'bg-thumbnail-img';
      img.src = EditorApi.backgroundUrl(item.path);
      img.alt = item.name;
      img.loading = 'lazy';
      thumbWrapper.appendChild(img);

      if (card.classList.contains('active')) {
        const activeTag = document.createElement('div');
        activeTag.className = 'bg-active-tag';
        activeTag.textContent = 'Aktiv';
        thumbWrapper.appendChild(activeTag);
      }

      const infoBox = document.createElement('div');
      infoBox.className = 'bg-card-info';

      const nameLabel = document.createElement('div');
      nameLabel.className = 'bg-gallery-name bg-card-title';
      nameLabel.textContent = item.name;
      infoBox.appendChild(nameLabel);

      if (item.sizeBytes) {
        const metaLabel = document.createElement('div');
        metaLabel.className = 'bg-card-meta';
        const kb = Math.max(1, Math.round(item.sizeBytes / 1024));
        metaLabel.textContent = `${kb} KB`;
        infoBox.appendChild(metaLabel);
      }

      card.appendChild(thumbWrapper);
      card.appendChild(infoBox);

      card.addEventListener('click', () => {
        this.onSelectBackground(item.path);
        this.close();
      });

      bgGalleryGrid.appendChild(card);
    }
  }
}
