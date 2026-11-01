# 05: Zwischen zwei Räumen wechseln und getrennt chatten

**What to build:** Zwei Beispielräume ("Lounge" und "Tavernen-Garten") sind über erreichbare Ausgänge verbunden. Besucher wechseln durch Anklicken des Ausgangs in den Zielraum; Figuren und Chat folgen dem aktuellen Raum.

**Blocked by:** 04

**Status:** in-progress

## Acceptance criteria

- [ ] Figur läuft zum Ausgang und wechselt nach Ankunft automatisch zum benannten Spawn des Zielraums.
- [ ] Alte Raumteilnehmer sehen den Austritt; neue Raumteilnehmer sehen den Eintritt.
- [ ] Nachrichten erreichen ausschließlich die aktuellen Teilnehmer des jeweiligen Raums (kein Empfang fremder Raumnachrichten bei Abwesenheit).
- [ ] Beim Wechsel bleibt der persönliche Chatverlauf erhalten und der Raumwechsel wird mit einer Systemnachricht kenntlich gemacht.
- [ ] Manipulierte Raumwechsel und nicht existierende Zielspawns werden serverseitig zurückgewiesen.
- [ ] Verspätete Bewegungs-/Chatereignisse aus dem alten Raum beschädigen den neuen Zustand nicht.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentliche Raum-/Chatkommandos.

## Hinweise
- Bestätigte Parameter:
  - Navigation: Laufen zum Ausgang und automatischer Wechsel bei Ankunft.
  - Chatverlauf: Persönlicher Verlauf bleibt erhalten; nur Nachrichten empfangen, bei denen der Spieler im Raum war; Systemnachricht beim Raumwechsel.
  - Zweiter Raum: "Tavernen-Garten" (640x360, Pixel-Art).
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

