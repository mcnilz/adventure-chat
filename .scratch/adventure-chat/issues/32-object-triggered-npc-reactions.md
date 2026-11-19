# 32 – Eine Objektaktion mit einer gezielten NPC-Reaktion verknüpfen

**What to build:** Autoren können einer ausgewählten erfolgreichen Objektaktion eine definierte Reaktion eines benannten NPC zuordnen. Als erster Reaktionstyp ist vorgesehen, dass der NPC ein angegebenes erreichbares Ziel ansteuert. Das Paket ist eine optionale Erweiterung.

**Blocked by:** [20](20-shared-object-actions.md), [21](21-state-dependent-navigation.md), [29](29-npc-movement.md).

**Status:** draft

## Acceptance criteria

- [ ] Die Zuordnung benennt die auslösende Aktion, die reagierende Figur und das Ziel. Ein Autor kann dieselbe Art der Zuordnung für verschiedene geeignete Objekte und NPCs verwenden. Fehlende oder unverträgliche Verweise werden bei der Inhaltsprüfung erkannt.
- [ ] Nur die ausdrücklich zugeordnete und erfolgreich abgeschlossene Aktion löst die Reaktion aus. Der NPC verwendet vorhandene Weg- und Übergangsregeln. Bei Wiederholung, gleichzeitiger Bedienung oder gesperrtem Ziel gilt das vereinbarte Verhalten.
- [ ] Ein erreichbares Beispiel in der Referenzwelt zeigt Aktion und beobachtbare Reaktion. Eine Klingel, nach deren Betätigung ein NPC zum Eingang kommt, ist ein mögliches Beispiel. In einer Arbeitskopie lässt sich dieselbe Regel mit einem andersartigen Auslöser nachweisen.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Eine gezielte NPC-Reaktion erweitert die in [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md) beschriebenen direkten Objektwirkungen. Vor Paketfreigabe die unterstützte Reaktion, ihren Vorrang gegenüber laufenden NPC-Wegen, Mehrspielerregeln und die Formatwirkung festlegen. Inhaltsgrenze und zwei Browser prüfen ausgelöste und abgelehnte Aktionen, Unterbrechungen, blockierte Ziele und Isolation der Vorschau. Der Zustandswechsel allein bleibt ohne automatische Folgeaktionen.
