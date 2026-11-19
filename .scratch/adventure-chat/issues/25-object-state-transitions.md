# 25 – Sichtbare Übergänge zwischen Objektzuständen konfigurieren

**What to build:** Autoren können einer Objektaktion einen sichtbaren Übergang zwischen zwei definierten Objektzuständen zuordnen. Verschiedene Bedienelemente können ausdrücklich auf dieselbe Zielinstanz wirken. Darstellung und erreichter Zustand sind für Besucher konsistent.

**Blocked by:** [21](21-state-dependent-navigation.md), [22](22-verbs-and-interaction-editor.md).

**Status:** draft

## Acceptance criteria

- [ ] Ein Autor kann für eine zulässige Zustandsänderung einen einfachen sichtbaren Übergang konfigurieren und die Gegenrichtung separat festlegen. Dieselbe Konfiguration lässt sich auf ein anderes geeignetes Objekt übertragen.
- [ ] Zwei getrennte Bedienelemente können die Zustandswerte desselben Zielobjekts über vorhandene direkte Verknüpfungen setzen. Zwei Browser stimmen nach der Aktion, bei konkurrierender Bedienung und bei späterem Eintritt im gültigen Zielzustand überein.
- [ ] Die Darstellung einer laufenden oder abgelehnten Änderung hat ein definiertes Ergebnis. Für Änderungen der Begehbarkeit gelten die vorhandenen Navigations- und Sicherheitsregeln.
- [ ] Ein erreichbares Beispiel in der Referenzwelt zeigt Hin- und Rückrichtung. Vorgeschlagen ist eine Tischplatte, die über zwei Tasten sichtbar hoch- und herunterfährt. Ein zweites geeignetes Objekt in einer Arbeitskopie belegt die Wiederverwendung.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Ein kurzer Übergang zwischen festen Endstellungen ist der Einstieg. Vor Freigabe Darstellungsform, Zeitpunkt der wirksamen Zustandsänderung und Verhalten bei Gegenaktionen während des Übergangs festlegen. Über Inhaltsvalidierung und zwei Browser Tasten, Gegenaktion, gleichzeitige Bedienung und Eintritt während und nach der Darstellung prüfen. Die Lesbarkeit der Bewegung bleibt Sichtabnahme. Beispielplatzierung nach den [Vorgaben für die Referenzwelt](../../../PLAN.md#beispiele-in-der-referenzwelt).
