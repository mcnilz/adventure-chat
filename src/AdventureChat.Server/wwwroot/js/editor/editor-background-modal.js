import { EditorApi } from './editor-api.js';
export class EditorBackgroundModal {
    elements;
    onSelectBackground;
    onStatusMessage;
    constructor(elements, onSelectBackground, onStatusMessage) {
        this.elements = elements;
        this.onSelectBackground = onSelectBackground;
        this.onStatusMessage = onStatusMessage;
        this.bindEvents();
    }
    async loadBackgroundsOverview() {
        try {
            const data = await EditorApi.fetchBackgrounds();
            this.elements.bgFsHintPath.textContent = data.relativePath || 'assets/images';
            this.elements.modalFsPath.textContent = data.filesystemDirectory || data.relativePath;
            this.renderGalleryGrid(data.backgrounds);
        }
        catch {
            this.elements.bgFsHintPath.textContent = 'assets/images';
        }
    }
    open() {
        this.elements.modalBgPicker.showModal();
        this.loadBackgroundsOverview().catch(() => { });
    }
    close() {
        this.elements.modalBgPicker.close();
    }
    async uploadFile(file) {
        this.onStatusMessage(`Lade '${file.name}' hoch...`, 'info');
        try {
            const res = await EditorApi.uploadBackground(file);
            if (res.success && res.path) {
                this.onStatusMessage(`Hintergrundbild '${res.name}' erfolgreich hochgeladen.`, 'success');
                this.onSelectBackground(res.path);
                await this.loadBackgroundsOverview();
            }
            else {
                this.onStatusMessage(`Upload fehlgeschlagen: ${res.error ?? 'Unbekannter Fehler'}`, 'error');
            }
        }
        catch (err) {
            this.onStatusMessage(`Upload-Fehler: ${String(err)}`, 'error');
        }
    }
    bindEvents() {
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
                this.uploadFile(file).catch(() => { });
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
                }
                catch {
                    this.onStatusMessage('Pfad konnte nicht in die Zwischenablage kopiert werden.', 'error');
                }
            }
        });
        // Drag & Drop on Dropzone
        this.bindDropZone();
    }
    bindDropZone() {
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
                this.uploadFile(file).catch(() => { });
            }
        });
    }
    renderGalleryGrid(items) {
        const { bgGalleryGrid, bgCount } = this.elements;
        bgCount.textContent = String(items.length);
        bgGalleryGrid.innerHTML = '';
        if (items.length === 0) {
            bgGalleryGrid.innerHTML = '<p class="text-muted text-center" style="grid-column: 1/-1;">Keine Bilder im Ordner vorhanden.</p>';
            return;
        }
        for (const item of items) {
            const card = document.createElement('div');
            card.className = 'bg-gallery-card';
            card.setAttribute('data-path', item.path);
            card.title = `Klicken, um '${item.name}' als Hintergrund zu wählen`;
            const thumbWrapper = document.createElement('div');
            thumbWrapper.className = 'bg-gallery-thumb-wrapper';
            const img = document.createElement('img');
            img.src = item.path;
            img.alt = item.name;
            img.loading = 'lazy';
            thumbWrapper.appendChild(img);
            const nameLabel = document.createElement('div');
            nameLabel.className = 'bg-gallery-name';
            nameLabel.textContent = item.name;
            card.appendChild(thumbWrapper);
            card.appendChild(nameLabel);
            card.addEventListener('click', () => {
                this.onSelectBackground(item.path);
                this.close();
            });
            bgGalleryGrid.appendChild(card);
        }
    }
}
//# sourceMappingURL=editor-background-modal.js.map