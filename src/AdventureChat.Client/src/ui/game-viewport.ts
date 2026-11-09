// Scale the complete HTML surface; canvas coordinates remain in room pixels.
export class GameViewport {
  private expanded = false;
  private readonly observer: ResizeObserver;

  public constructor(
    private readonly app: HTMLElement,
    private readonly container: HTMLElement,
    private readonly button: HTMLButtonElement
  ) {
    this.button.addEventListener('click', this.toggle);
    this.observer = new ResizeObserver(() => this.resize());
    this.observer.observe(app);
    this.observer.observe(container);
    this.resize();
  }

  public dispose(): void {
    this.observer.disconnect();
    this.button.removeEventListener('click', this.toggle);
  }

  private readonly toggle = (): void => {
    this.expanded = !this.expanded;
    this.button.textContent = this.expanded ? 'Normalgröße' : 'Auf Fenstergröße';
    this.button.setAttribute('aria-pressed', String(this.expanded));
    this.resize();
  };

  private resize(): void {
    if (this.container.hidden) return;
    const padding = getComputedStyle(this.app);
    const width = Math.max(1, this.app.clientWidth - parseFloat(padding.paddingLeft) - parseFloat(padding.paddingRight));
    const height = Math.max(1, this.app.clientHeight - parseFloat(padding.paddingTop) - parseFloat(padding.paddingBottom));
    // Computed dimensions are untransformed and retain fractional CSS pixels.
    const surface = getComputedStyle(this.container);
    const scale = Math.min(width / parseFloat(surface.width), height / parseFloat(surface.height), this.expanded ? Infinity : 1);
    this.container.style.setProperty('--game-scale', String(scale));
  }
}
