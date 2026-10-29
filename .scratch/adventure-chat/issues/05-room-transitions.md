# 05: Zwischen zwei Räumen wechseln und getrennt chatten

**What to build:** Zwei Beispielräume sind über erreichbare Ausgänge verbunden. Besucher wechseln durch Anklicken des Ausgangs in den Zielraum; Figuren und Chat folgen dem aktuellen Raum.

**Blocked by:** 04

**Status:** draft

## Acceptance criteria

- [ ] Figur läuft zum Ausgang und erscheint am benannten Spawn des Zielraums.
- [ ] Alte Raumteilnehmer sehen den Austritt; neue Raumteilnehmer sehen den Eintritt.
- [ ] Nachrichten erreichen ausschließlich die aktuellen Teilnehmer des jeweiligen Raums.
- [ ] Beim Wechsel wird der alte Verlauf geleert und der neue Raum kenntlich gemacht.
- [ ] Manipulierte Raumwechsel und nicht existierende Zielspawns werden zurückgewiesen.
- [ ] Verspätete Bewegungs-/Chatereignisse aus dem alten Raum beschädigen den neuen Zustand nicht.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentliche Raum-/Chatkommandos.

## Hinweise
Handgeschriebene Beispieldefinitionen reichen hier. Kein allgemeines Interaktionssystem erforderlich.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

