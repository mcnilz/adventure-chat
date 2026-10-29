# Aktueller Stand
Revision 4 · 29.10.2026.

## Vorhanden
- [x] Gespräch in Spezifikation, Architektur, Autorenformat und Tickets überführt.
- [x] Matt Pococks aktuelle Skill-Quellen geprüft und verlinkt.
- [x] Paketumfang und Testgrenzen für Ticket 01 bestätigt.
- [x] Implementierung von Ticket 01 abgeschlossen und menschlich abgenommen (Status: `done`).
- [x] Git LFS für Bild- und Binärdateien konfiguriert.
- [x] SCUMM / Lucasfilm Grafikstil (Maniac Mansion / Zak McKracken Big-Head Proportionen 48x64, Rücken-/Front-/Seitenansicht, Tavernen-Hintergrund 640x360) umgesetzt.
- [ ] Paketumfang und Testgrenzen für Ticket 02 bestätigt / gestartet.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `02-pathfinding.md` (Ein Hindernis mit servergeprüfter Bewegung umgehen)
- **Status:** `ready-to-start`
- **Letzter Stand:** Ticket 01 vollständig umgesetzt, abgenommen und im lokalen Git eingecheckt. Alle 6 Playwright-E2E-Tests laufen fehlerfrei durch.
- **Ausgeführte Checks:** `tsc --noEmit` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (6/6 Playwright-Tests erfolgreich).

## Nächste Aktion
Ticket 02 (`02-pathfinding.md`) im neuen Kontext starten:
1. Begehbare Raumgeometrie mit Hindernis definieren (z. B. Tavernentisch / Theke im begehbaren Bereich).
2. Wegfindung implementieren (empfohlen: Visibility Graph mit euklidischem A* um Polygonecken).
3. Servervalidierung / Pfadprüfung in ASP.NET Core einbinden (Vorbereitung für SignalR in Ticket 03).
4. Automatisierte Tests erweitern (Unit-Tests für Pfadfindung + Playwright-E2E-Test für Hindernisumrundung und Klickablehnung).

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt. Exakte Chatlimits, Schemaentwurf und Bibliotheksversionen vor betroffenen Paketen festlegen.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 6 Tests erfolgreich bestanden.

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.

