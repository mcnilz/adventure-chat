# 05: Zwischen zwei Räumen wechseln und getrennt chatten

**What to build:** Zwei Beispielräume ("Lounge" und "Tavernen-Garten") sind über erreichbare Ausgänge verbunden. Besucher wechseln durch Anklicken des Ausgangs in den Zielraum; Figuren und Chat folgen dem aktuellen Raum.

**Blocked by:** 04

**Status:** done

## Acceptance criteria

- [x] Figur läuft zum Ausgang und wechselt nach Ankunft automatisch zum benannten Spawn des Zielraums.
- [x] Alte Raumteilnehmer sehen den Austritt; neue Raumteilnehmer sehen den Eintritt.
- [x] Nachrichten erreichen ausschließlich die aktuellen Teilnehmer des jeweiligen Raums (kein Empfang fremder Raumnachrichten bei Abwesenheit).
- [x] Beim Wechsel bleibt der persönliche Chatverlauf erhalten und der Raumwechsel wird mit einer Systemnachricht kenntlich gemacht.
- [x] Manipulierte Raumwechsel und nicht existierende Zielspawns werden serverseitig zurückgewiesen.
- [x] Verspätete Bewegungs-/Chatereignisse aus dem alten Raum beschädigen den neuen Zustand nicht.

- [x] Visuelle Overlays und Tür-Hoverboxen im regulären Spielbetrieb deaktiviert (nur im Debug-Modus aktiv, Steuerung über SCUMM-Aktionszeile).

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentliche Raum-/Chatkommandos.

## Hinweise
- Bestätigte Parameter:
  - Navigation: Laufen zum Ausgang und automatischer Wechsel bei Ankunft.
  - Chatverlauf: Persönlicher Verlauf bleibt erhalten; nur Nachrichten empfangen, bei denen der Spieler im Raum war; Systemnachricht beim Raumwechsel.
  - Zweiter Raum: "Tavernen-Garten" (640x360, Pixel-Art).
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
- Ausgeführte Checks: `npm run typecheck` (0 Fehler), `npm run build` (0 Fehler), `dotnet test` (61/61 Tests bestanden: 39 Server-Tests, 22 Playwright-E2E-Tests).
- Reviewbefunde: Alle Akzeptanzkriterien erfüllt, Raumwechsel über SignalR-Gruppentrennung und SessionManager-Validierung abgesichert, Chatnachrichten und Figurenpositionen bleiben strikt raumgebunden, SCUMM-Aktionszeile liefert klares Feedback.
- Status: `ready-for-human` (Wartet auf menschliche Abnahme).

