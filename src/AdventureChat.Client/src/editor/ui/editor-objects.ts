import { EditorApi } from '../core/editor-api.js';
import type { EditorState } from '../core/editor-state.js';
import type { RoomObjectDto } from '../core/editor-types.js';

export class EditorObjects {
  private displayedObject: RoomObjectDto | undefined;
  private readonly root = document.createElement('section');
  private readonly list = document.createElement('select');
  private readonly fields = new Map<string, HTMLInputElement>();
  private readonly form = document.createElement('div');

  public constructor(private readonly state: EditorState, private readonly changed: () => void,
    private readonly error: (message: string) => void) {
    this.root.className = 'sidebar-section object-panel';
    const heading = document.createElement('h3'); heading.textContent = 'Raumobjekte';
    const hint = document.createElement('p');
    hint.textContent = 'PNG verdeckt Figuren hinter der Tiefenlinie. Kollision separat als Hindernis zeichnen. Alt + Klick wählt darunterliegende Geometrie. Benötigt Weltformat 1.4.0.';
    this.list.id = 'object-list'; this.list.setAttribute('aria-label', 'Raumobjekt auswählen');
    this.list.addEventListener('change', () => {
      state.setSelection({type: 'object', objectId: this.list.value}); changed();
    });
    const add = document.createElement('button'); add.className = 'btn-secondary'; add.id = 'object-add'; add.textContent = 'Objekt anlegen';
    add.onclick = () => {
      if (!state.addObject()) { error('Maximal 128 Objekte pro Raum.'); return; }
      changed();
    };
    this.root.append(heading, hint, this.list, add, this.form);
    for (const [key, label] of [['id','Objekt-ID'],['image','PNG-Pfad'],['x','X'],['y','Y'],['width','Breite'],['height','Höhe'],['depthY','Tiefenlinie Y']]) {
      const input = document.createElement('input'); input.id = 'object-' + key;
      input.type = key === 'id' || key === 'image' ? 'text' : 'number'; input.step = '1';
      const row = document.createElement('label'); row.htmlFor = input.id; row.textContent = label!; row.append(input);
      this.fields.set(key!,input); this.form.append(row);
      input.addEventListener('change', () => this.apply());
    }
    const images = document.createElement('select'); images.id = 'object-images'; images.setAttribute('aria-label','Vorhandenes PNG zuordnen');
    const refresh = async () => {
      try {
        const data = await EditorApi.fetchBackgrounds(); images.replaceChildren(new Option('PNG auswählen…',''));
        for (const image of data.backgrounds) images.append(new Option(image.name,image.path));
      } catch (e) { error(String(e)); }
    };
    images.onfocus = () => void refresh();
    images.onchange = () => { if(images.value) { this.fields.get('image')!.value=images.value; this.apply(); } };
    const upload = document.createElement('input'); upload.type='file'; upload.accept='image/png'; upload.id='object-upload';
    upload.setAttribute('aria-label','Objekt-PNG hochladen');
    upload.onchange = async () => {
      const file=upload.files?.[0]; if(!file) return;
      try {
        const result=await EditorApi.uploadBackground(file);
        if(!result.success || !result.path) throw new Error(result.error ?? 'Upload fehlgeschlagen');
        this.fields.get('image')!.value=result.path; this.apply(); await refresh();
      } catch(e) { error(String(e)); }
      upload.value='';
    };
    const remove = document.createElement('button'); remove.className='btn-danger'; remove.id='object-delete'; remove.textContent='Objekt löschen';
    remove.onclick=()=>{state.deleteSelected();changed();};
    this.form.append(images,upload,remove);
    document.querySelector('.editor-sidebar')!.prepend(this.root);
  }

  private selected(): RoomObjectDto | undefined {
    const sel=this.state.getSelection();
    return sel.type==='object' ? this.state.getRoom().objects?.find(o=>o.id===sel.objectId) : undefined;
  }

  public update(): void {
    const object=this.selected();
    this.list.replaceChildren(new Option('Objekt auswählen…',''));
    for(const item of this.state.getRoom().objects ?? []) this.list.append(new Option(item.id,item.id));
    this.list.value=object?.id ?? ''; this.form.hidden=!object;
    if(!object) return;
    const values={id:object.id,image:object.image,x:object.position.x,y:object.position.y,width:object.size.width,height:object.size.height,depthY:object.depthY};
    for(const [key,value] of Object.entries(values)) {
      const input = this.fields.get(key)!;
      if (document.activeElement !== input || this.displayedObject !== object) input.value=String(value);
    }
    this.displayedObject = object;
  }

  private apply(): void {
    const object=this.selected(); if(!object) return;
    const value=(key:string)=>this.fields.get(key)!.value;
    const edited = {
      id: value('id'), image: value('image'),
      position: { x: Number(value('x')), y: Number(value('y')) },
      size: { width: Number(value('width')), height: Number(value('height')) },
      depthY: Number(value('depthY'))
    };
    if (!this.state.updateObject(object.id, edited)) {
      this.error('Eindeutige ID und ganze Raumkoordinaten erforderlich; Größe muss in den Raum passen.');
      return;
    }
    this.changed();
  }
}
