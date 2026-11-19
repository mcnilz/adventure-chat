export class InteractionFields {
  public constructor(private readonly parent: HTMLElement) {}

  public text(label: string, value: string, update: (value: string) => void | boolean): HTMLInputElement {
    const input = document.createElement('input');
    input.type = 'text';
    input.value = value;
    input.onchange = event => {
      const field = event.currentTarget as HTMLInputElement;
      if (update(field.value.trim()) === false) field.value = value;
    };
    this.label(label, input);
    return input;
  }

  public number(label: string, value: number, update: (value: number) => void): void {
    const input = document.createElement('input');
    input.type = 'number';
    input.step = 'any';
    input.value = String(value);
    input.required = true;
    input.onchange = event => {
      const field = event.currentTarget as HTMLInputElement;
      if (field.reportValidity()) update(field.valueAsNumber);
    };
    this.label(label, input);
  }

  public select(label: string, value: string, options: Iterable<readonly [string, string]>,
    update: (value: string) => void | boolean): HTMLSelectElement {
    const select = document.createElement('select');
    for (const [id, name] of options) select.append(new Option(name, id));
    select.value = value;
    select.onchange = event => {
      const field = event.currentTarget as HTMLSelectElement;
      if (update(field.value) === false) field.value = value;
    };
    this.label(label, select);
    return select;
  }

  public check(label: string, value: boolean, update: (value: boolean) => void): void {
    const input = document.createElement('input');
    input.type = 'checkbox';
    input.checked = value;
    input.onchange = event => update((event.currentTarget as HTMLInputElement).checked);
    this.label(label, input);
  }

  public button(label: string, action: () => void): void {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn-secondary';
    button.textContent = label;
    button.onclick = action;
    this.parent.append(button);
  }

  public group(title: string, validationKey?: string): InteractionFields {
    const fieldset = document.createElement('fieldset');
    const legend = document.createElement('legend');
    legend.textContent = title;
    if (validationKey) fieldset.dataset.validationKey = validationKey;
    fieldset.append(legend);
    this.parent.append(fieldset);
    return new InteractionFields(fieldset);
  }

  private label(text: string, input: HTMLElement): void {
    const label = document.createElement('label');
    label.textContent = text;
    input.setAttribute('aria-label', text);
    label.append(input);
    this.parent.append(label);
  }
}

// Keep controls mounted while committing a field on blur so the next click
// or keyboard focus still reaches its original target.
export function updateInteractionForm(current: HTMLElement, next: HTMLElement): void {
  const selector = 'fieldset, legend, input, select, button';
  const currentFields = Array.from(current.querySelectorAll<HTMLElement>(selector));
  const nextFields = Array.from(next.querySelectorAll<HTMLElement>(selector));
  if (currentFields.length !== nextFields.length ||
    currentFields.some((field, index) => field.tagName !== nextFields[index]!.tagName ||
      field.getAttribute('aria-label') !== nextFields[index]!.getAttribute('aria-label'))) {
    current.replaceChildren(...next.childNodes);
    return;
  }
  currentFields.forEach((field, index) => {
    const replacement = nextFields[index]!;
    if (field instanceof HTMLInputElement && replacement instanceof HTMLInputElement) {
      field.onchange = replacement.onchange;
      field.value = replacement.value;
      field.checked = replacement.checked;
      field.setCustomValidity('');
    } else if (field instanceof HTMLSelectElement && replacement instanceof HTMLSelectElement) {
      const value = replacement.value;
      const options = Array.from(replacement.options);
      if (options.length !== field.options.length ||
        options.some((option, i) => option.value !== field.options[i]!.value || option.text !== field.options[i]!.text))
        field.replaceChildren(...options);
      field.value = value;
      field.onchange = replacement.onchange;
    } else if (field instanceof HTMLFieldSetElement) {
      if (replacement.dataset.validationKey) field.dataset.validationKey = replacement.dataset.validationKey;
      else delete field.dataset.validationKey;
    } else {
      field.onclick = replacement.onclick;
      field.textContent = replacement.textContent;
    }
  });
}
