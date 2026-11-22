# 32 – Eine Objektaktion mit einer gezielten NPC-Reaktion verknüpfen

**What to build:** Autoren können einer ausgewählten erfolgreichen Objektaktion eine definierte Reaktion eines benannten NPC zuordnen. Als erster Reaktionstyp ist vorgesehen, dass der NPC ein angegebenes erreichbares Ziel ansteuert. Das Paket ist eine optionale Erweiterung.

**Blocked by:** [20](20-shared-object-actions.md), [21](21-state-dependent-navigation.md), [29](29-npc-movement.md).

**Status:** ready-for-human

## Acceptance criteria

- [x] Die Zuordnung benennt die auslösende Aktion, die reagierende Figur und das Ziel. Ein Autor kann dieselbe Art der Zuordnung für verschiedene geeignete Objekte und NPCs verwenden. Fehlende oder unverträgliche Verweise werden bei der Inhaltsprüfung erkannt.
- [x] Nur die ausdrücklich zugeordnete und erfolgreich abgeschlossene Aktion löst die Reaktion aus. Der NPC verwendet vorhandene Weg- und Übergangsregeln. Bei Wiederholung, gleichzeitiger Bedienung oder gesperrtem Ziel gilt das vereinbarte Verhalten.
- [x] Ein erreichbares Beispiel in der Referenzwelt zeigt Aktion und beobachtbare Reaktion. Eine Klingel, nach deren Betätigung ein NPC zum Eingang kommt, ist ein mögliches Beispiel. In einer Arbeitskopie lässt sich dieselbe Regel mit einem andersartigen Auslöser nachweisen.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Eine gezielte NPC-Reaktion erweitert die in [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md) beschriebenen direkten Objektwirkungen. Vor Paketfreigabe die unterstützte Reaktion, ihren Vorrang gegenüber laufenden NPC-Wegen, Mehrspielerregeln und die Formatwirkung festlegen. Inhaltsgrenze und zwei Browser prüfen ausgelöste und abgelehnte Aktionen, Unterbrechungen, blockierte Ziele und Isolation der Vorschau. Der Zustandswechsel allein bleibt ohne automatische Folgeaktionen.

## Freigabe am 22.11.2026

Der Benutzer bestätigt mit „ja“ den Lauf zu einem Ziel im selben Raum, den Vorrang von Gesprächen und Führungen und die Fortsetzung einer unterbrochenen Patrouille. Gleichzeitig ausgelöste Rufe zum selben Ziel führen höchstens zu einem Lauf. Ein gesperrter oder wegen einer laufenden Beschäftigung abgewiesener Ruf verfällt. Die erfolgreiche Objektaktion bleibt wirksam. Nela läuft beim Klingeln zum Eingang der Lounge und bleibt dort ansprechbar. Bestätigt sind Laden und Speichern einschließlich unveränderter älterer Referenzwelten, zwei Browser, eine isolierte Editorvorschau und ein andersartiger Auslöser in einer Arbeitskopie.

Format 1.15.0 ist eine additive Minor-Erweiterung. Die optionale Angabe `npcReaction` gehört zur konkreten Objektaktion. Der Server prüft die lokale NPC-ID und die Erreichbarkeit in der statischen Raumgeometrie. Ausgeführte Läufe berücksichtigen die jeweils wirksamen Objektzustände. Weitere Rufe während eines begonnenen Rufs verfallen. Die Referenzwelt verwendet Inhaltsversion 5.7.0.

## Manuelle Abnahme

1. Die [lokale Referenzwelt](http://127.0.0.1:63891/) öffnen und mit zwei Spielern in die Lounge eintreten. Im ersten Fenster **Drücken** wählen und auf die Klingel am Tresen klicken. In beiden Fenstern läuft Nela nach vorn zum Eingangsbereich und bleibt dort stehen.
2. **Spreche mit** wählen, Nela am Ziel anklicken und **Hast du die Klingel gehört?** auswählen. Nela antwortet und kann wie zuvor die Führung anbieten.
3. Im [Raumeditor](http://127.0.0.1:63891/editor.html?worldId=reference) **Lounge** und **Klingel** wählen. In den Aktionsfeldern ist Nela mit dem Ziel (160, 330) eingetragen. In der Raumvorschau beginnt Nela an ihrer Ausgangsposition und reagiert auf den eigenen Klingelruf.

Die getrennte Programmkopie liegt unter `.scratch/adventure-chat/evidence/32/review-app`, ihr Weltpaket in `worlds/reference` innerhalb dieser Kopie. Browser-Eintritt, Editorfelder und die isolierte Ankunft sind geprüft. Innerhalb der Vorschau setzt ein Raumwechsel die NPC-Positionen auf die Ausgangspositionen des betretenen Raums zurück.

## Prüfbelege

- TypeScript-Typcheck, Clientbuild und vollständige Suite bestanden. 400 Tests insgesamt, davon 264 Server- und 136 Browsertests. Der Browserlauf dauerte 15 Minuten und 44 Sekunden. [Testprotokoll](../evidence/32/final-suite.log)
- Gezielt geprüft wurden Laden und Speichern einschließlich ausdrücklichem Formatwechsel, eine abgelehnte veraltete Aktion, Wiederholung während des Laufs, unterbrochene Patrouille, zwei gleichzeitig klingelnde Besucher, Vorrang von Gespräch und Führung sowie die isolierte Vorschau. Eine Arbeitskopie ruft die Wache durch Betätigung eines Stuhls. Ihre Reaktion berücksichtigt eine zwischenzeitlich geschlossene Tür. Für eine Sperre während eines Gesprächs ist geprüft, dass der alte Ruf auch nach Wiederöffnen verfällt.
- Standards- und Spezifikationsreview mit getrennter Nachprüfung abgeschlossen. Dabei wurden die anfängliche Spawn-Erreichbarkeit und der sofortige Abbruch eines unerreichbar gewordenen Rufs abgesichert. Beide Nachprüfungen ergaben keine offenen Befunde.
- Das neue Schema-Bündel ergänzt 1.15.0. Die veröffentlichten älteren Schemas und Referenzfixtures sind im Dateivergleich unverändert. Die Vollsuite prüft das Laden und Verhalten älterer Welten. Keine neuen Abhängigkeiten.
