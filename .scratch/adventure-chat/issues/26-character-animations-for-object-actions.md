# 26 – Figurenposen und Animationen an Objektaktionen binden

**What to build:** Autoren können einer Objektaktion einen Bedienpunkt, eine Blickrichtung und eine Figurenpose oder kurze Animation zuordnen. Eine Pose kann für eine fortdauernde Benutzung gehalten werden. Eine kurze Geste kann einer sichtbaren Objektwirkung vorausgehen. Dieselben Angaben stehen für unterschiedliche geeignete Interaktionen zur Verfügung.

**Blocked by:** [21](21-state-dependent-navigation.md), [22](22-verbs-and-interaction-editor.md).

**Status:** draft

## Acceptance criteria

- [ ] Die Figur erreicht den angegebenen Bedienpunkt, richtet sich aus und zeigt die für diese Aktion konfigurierte Pose oder Animation. Der Autor kann einer anderen Aktion am selben oder einem anderen Objekt eine andere Darstellung zuordnen.
- [ ] Eine gehaltene Pose endet über die vereinbarte Bedienung und die Figur kann anschließend wieder gehen. Eine kurze Geste kann vor einem definierten Objektzustandswechsel abgespielt werden. Andere Besucher im Raum sehen die Handlung und später Eintretende den erreichten Objektzustand.
- [ ] Aktionsvoraussetzungen können vorhandene Objektzustände verwenden. Ein gesperrter oder unerreichbarer Bedienpunkt, wiederholte Eingabe und vorzeitig beendete Benutzung haben definierte Ergebnisse. Zwei Besucher erhalten bei derselben belegbaren Stelle ein konsistentes Ergebnis.
- [ ] Erreichbare Beispiele in der Referenzwelt zeigen beide Fälle. Vorgeschlagen sind eine Sitzpose an einem freigegebenen Platz, etwa bei abgesenktem Schreibtisch, und ein kurzes Handheben, nach dem ein vorbereiteter Tafelinhalt erscheint. Beide verwenden dieselbe konfigurierbare Zuordnung.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Vor Freigabe Ausstieg aus gehaltenen Posen, Verhalten bei Zustandsänderung während der Benutzung, Zeitpunkt der kurzen Aktionswirkung und Darstellung bei fehlender Figurengrafik klären. Inhaltsprüfung und zwei Browser decken Weg, Ausrichtung, Pose, sichtbaren Effekt, Unterbrechung und spätere Raumansicht ab. Eine grobe Geste genügt für die Sichtabnahme. Beispielplatzierung nach den [Vorgaben für die Referenzwelt](../../../PLAN.md#beispiele-in-der-referenzwelt).
