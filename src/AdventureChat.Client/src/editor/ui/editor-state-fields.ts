import type { ObjectAppearance, Point } from '../../types.js';
import type { RoomView } from '../core/editor-types.js';
import { InteractionFields } from './interaction-fields.js';

export function editObjectState(fields: InteractionFields, appearance: ObjectAppearance, room: RoomView,
  update: (value: ObjectAppearance) => void): void {
  fields.text('Zustandsname', appearance.name, name => update({ ...appearance, name }));
  fields.text('Zustands-PNG', appearance.image, image => update({ ...appearance, image }));
  fields.number('Zustand X', appearance.position.x, x => update({ ...appearance, position: { ...appearance.position, x } }));
  fields.number('Zustand Y', appearance.position.y, y => update({ ...appearance, position: { ...appearance.position, y } }));
  fields.number('Zustandsbreite', appearance.size.width, width => update({ ...appearance, size: { ...appearance.size, width } }));
  fields.number('Zustandshöhe', appearance.size.height, height => update({ ...appearance, size: { ...appearance.size, height } }));
  fields.number('Zustand Tiefenlinie Y', appearance.depthY, depthY => update({ ...appearance, depthY }));

  const navigation = fields.group('Navigation in diesem Zustand', 'state-navigation');
  const blockedExits = appearance.blockedExits ?? [];
  for (const exit of room.exits ?? []) {
    const matches = (id: string) => id.toLowerCase() === exit.id.toLowerCase();
    navigation.check(`Ausgang sperren: ${exit.label ?? exit.id}`, blockedExits.some(matches), blocked => {
      const ids = blocked ? [...blockedExits, exit.id] : blockedExits.filter(id => !matches(id));
      update({ ...appearance, blockedExits: ids.length ? ids : null });
    });
  }
  const obstacles = appearance.obstacles ?? [];
  const updateObstacle = (index: number, points: readonly Point[] | null) => {
    const next = [...obstacles];
    if (points) next[index] = { ...next[index]!, points };
    else next.splice(index, 1);
    update({ ...appearance, obstacles: next.length ? next : null });
  };
  obstacles.forEach((obstacle, index) => {
    const polygon = navigation.group(`Hindernis ${obstacle.id}`, `obstacle/${obstacle.id}`);
    obstacle.points.forEach((point, vertex) => {
      const editPoint = (next: Point) => updateObstacle(index, obstacle.points.map((p, i) => i === vertex ? next : p));
      polygon.number(`Punkt ${vertex + 1} X`, point.x, x => editPoint({ ...point, x }));
      polygon.number(`Punkt ${vertex + 1} Y`, point.y, y => editPoint({ ...point, y }));
      if (obstacle.points.length > 3) polygon.button(`Punkt ${vertex + 1} löschen`,
        () => updateObstacle(index, obstacle.points.filter((_, i) => i !== vertex)));
    });
    if (obstacle.points.length < 16) polygon.button('Punkt einfügen', () => {
      const [first, second, ...rest] = obstacle.points;
      if (first && second) updateObstacle(index, [first, { x: (first.x + second.x) / 2, y: (first.y + second.y) / 2 }, second, ...rest]);
    });
    polygon.button('Hindernis löschen', () => updateObstacle(index, null));
  });
  if (obstacles.length < 4) navigation.button('Hindernis hinzufügen', () => {
    const id = unusedId('hindernis', obstacles.map(obstacle => obstacle.id));
    const x = Math.min(appearance.position.x, room.size.width - 24);
    const y = Math.min(appearance.position.y, room.size.height - 16);
    update({ ...appearance, obstacles: [...obstacles, { id, points: [
      { x, y }, { x: x + 24, y }, { x: x + 24, y: y + 16 }, { x, y: y + 16 }
    ] }] });
  });
}

export function unusedId(prefix: string, ids: readonly string[]): string {
  let index = 1;
  while (ids.includes(`${prefix}-${index}`)) index++;
  return `${prefix}-${index}`;
}
