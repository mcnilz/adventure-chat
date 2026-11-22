import { EditorApi } from '../core/editor-api.js';

const UPGRADE_FORMATS = ['1.8.0', '1.9.0', '1.10.0', '1.11.0', '1.12.0', '1.13.0', '1.14.0', '1.15.0', '1.16.0'];

export class EditorFormat {
  private readonly selector = document.querySelector<HTMLSelectElement>('#world-format')!;
  private readonly hint = document.querySelector('#world-format-change')!;
  private version: string | null = null;

  public constructor() {
    this.selector.onchange = () => this.showChange();
  }

  public async init(): Promise<void> {
    this.showVersion((await EditorApi.status()).formatVersion ?? null);
  }

  public showVersion(version: string | null): void {
    this.version = version;
    this.selector.replaceChildren(new Option(version ?? 'Legacy', version ?? ''));
    for (const target of UPGRADE_FORMATS) {
      if (version && target.localeCompare(version, 'en', { numeric: true }) > 0) this.selector.add(new Option(target, target));
    }
    this.selector.disabled = this.selector.options.length < 2;
    this.showChange();
  }

  public upgradeFrom(): string | undefined {
    return this.version && this.version !== this.selector.value ? this.version : undefined;
  }

  public targetVersion(): string | undefined {
    return this.upgradeFrom() ? this.selector.value : undefined;
  }

  public setSaving(saving: boolean): void {
    this.selector.disabled = saving || this.selector.options.length < 2;
  }

  public resetSelection(): void {
    this.selector.value = this.version ?? '';
    this.showChange();
  }

  private showChange(): void {
    const from = this.upgradeFrom();
    this.hint.textContent = from
      ? `Beim Speichern wird in world.yaml das Weltformat von ${from} auf ${this.selector.value} geändert.`
      : '';
  }
}
