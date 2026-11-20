import type { FacingDirection, ObjectActorAnimation, Point } from '../../types.js';
import { InteractionFields } from './interaction-fields.js';

const DIRECTIONS: ReadonlyArray<readonly [FacingDirection, string]> = [
  ['up', 'Oben'], ['down', 'Unten'], ['left', 'Links'], ['right', 'Rechts']
];

export function editActorAnimation(fields: InteractionFields, animation: ObjectActorAnimation | null | undefined,
  points: readonly Point[], update: (next: ObjectActorAnimation | null) => void,
  error: (message: string, field: string) => void): void {
  const group = fields.group('Figurenanimation', 'actor-animation');
  group.select('Figurenanimation', animation ? animation.durationMs == null ? 'pose' : 'gesture' : '', [
    ['', 'Keine'], ['pose', 'Gehaltene Pose'], ['gesture', 'Kurze Geste']
  ], value => {
    if (!value) { if (animation) update(null); return; }
    if (!points.length) {
      error('Lege zuerst einen Bedienpunkt an.', 'Figurenanimation');
      return false;
    }
    const durationMs = value === 'gesture' ? animation?.durationMs ?? 1000 : null;
    if (animation && (animation.durationMs ?? null) === durationMs) return;
    // The pose and gesture require different sprite layouts.
    update({ point: animation?.point ?? 0, direction: animation?.direction ?? 'up', durationMs });
    return;
  });
  if (!animation) return;

  group.select('Bedienpunkt der Animation', String(animation.point), points.map((point, index) =>
    [String(index), `Bedienpunkt ${index + 1} (${point.x}, ${point.y})`] as const),
  value => update({ ...animation, point: Number(value) }));
  group.select('Blickrichtung der Animation', animation.direction, DIRECTIONS,
    value => update({ ...animation, direction: value as FacingDirection }));
  if (animation.durationMs != null) group.number('Gestendauer (ms)', animation.durationMs,
    durationMs => update({ ...animation, durationMs }));

  const sprites = Object.entries(animation.sprites ?? {});
  const assign = (entries: [string, string][]) => update({ ...animation, sprites: Object.fromEntries(entries) });
  sprites.forEach(([id, path], index) => {
    const label = `Figuren-ID ${index + 1}`;
    const replace = (key: string, image: string) =>
      assign(sprites.map((entry, i) => i === index ? [key, image] : entry));
    group.text(label, id, key => {
      if (!/^[a-zA-Z0-9_-]{1,64}$/.test(key) || sprites.some(([other], i) => other === key && i !== index)) {
        error('Verwende eine eindeutige Figuren-ID aus dem Weltkatalog.', label);
        return false;
      }
      replace(key, path);
      return true;
    });
    group.text(`Figuren-PNG ${index + 1}`, path, image => replace(id, image));
    group.button(`Figuren-PNG ${index + 1} entfernen`, () => assign(sprites.filter((_, i) => i !== index)));
  });
  if (sprites.length < 32) group.button('Figuren-PNG hinzufügen', () => {
    let index = 1;
    while (sprites.some(([id]) => id === `figur-${index}`)) index++;
    assign([...sprites, [`figur-${index}`, '']]);
  });
}
