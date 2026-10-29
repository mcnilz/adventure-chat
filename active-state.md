# Aktueller Stand
Revision 2 · 29.10.2026.

## Vorhanden
- [x] Gespräch in Spezifikation, Architektur, Autorenformat und Tickets überführt.
- [x] Matt Pococks aktuelle Skill-Quellen geprüft und verlinkt.
- [x] Paketumfang und Testgrenzen für Ticket 01 bestätigt.
- [x] Implementierung von Ticket 01 abgeschlossen (Status: `ready-for-human`).
- [ ] Ticket 01 menschlich abgenommen.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `01-local-room.md` (Lokal einen Raum betreten und die eigene Figur bewegen)
- **Status:** `ready-for-human`
- **Letzter Stand:** ASP.NET Core Backend, TypeScript Canvas-2D-Client mit zeitbasierter Bewegung, Zielüberschreibung, Klickfilterung und Aspect-Ratio-Skalierung, PNG-Assets für Hintergrund und Spielfigur sowie Playwright-E2E-Tests implementiert.
- **Ausgeführte Checks:** `tsc --noEmit` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (5/5 Playwright-Tests erfolgreich).

## Nächste Aktion
Menschliche Abnahme von Ticket 01 durchführen. Nach Bestätigung Ticket 01 auf `done` setzen und Ticket 02 (`02-pathfinding.md`) vorschlagen.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt. Exakte Chatlimits, Schemaentwurf und Bibliotheksversionen vor betroffenen Paketen festlegen.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 5 Tests erfolgreich bestanden.

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.

