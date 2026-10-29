# Aktueller Stand
Revision 5 · 29.10.2026.

## Vorhanden
- [x] Gespräch in Spezifikation, Architektur, Autorenformat und Tickets überführt.
- [x] Matt Pococks aktuelle Skill-Quellen geprüft und verlinkt.
- [x] Paketumfang und Testgrenzen für Ticket 01 bestätigt.
- [x] Implementierung von Ticket 01 abgeschlossen und menschlich abgenommen (Status: `done`).
- [x] Git LFS für Bild- und Binärdateien konfiguriert.
- [x] SCUMM / Lucasfilm Grafikstil (Maniac Mansion / Zak McKracken Big-Head Proportionen 48x64, Rücken-/Front-/Seitenansicht, Tavernen-Hintergrund 640x360) umgesetzt.
- [x] Paketumfang und Testgrenzen für Ticket 02 bestätigt.
- [x] Ticket 02 vollständig umgesetzt und getestet (Status: `ready-for-human`).
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `02-pathfinding.md` (Ein Hindernis mit servergeprüfter Bewegung umgehen)
- **Status:** `ready-for-human`
- **Letzter Stand:** Servervalidierte Wegfindung mit Visibility Graph + A* in C# implementiert, ASP.NET Core Endpunkte bereitgestellt, TypeScript-Client integriert und mit 15 Server-Unit-Tests sowie 8 Playwright-E2E-Tests vollständig abgesichert.
- **Ausgeführte Checks:** `tsc --noEmit` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (23/23 Tests erfolgreich: 15 ServerTests + 8 E2ETests).

## Nächste Aktion
Menschliche Abnahme von Ticket 02 einholen. Nach Bestätigung:
1. Status von `02-pathfinding.md` auf `done` setzen.
2. Ticket 03 (`03-multiplayer.md` - SignalR Echtzeit-Multiplayer) vorschlagen und vorbereiten.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt. Exakte Chatlimits, Schemaentwurf und Bibliotheksversionen vor betroffenen Paketen festlegen.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 23 Tests erfolgreich bestanden (15 ServerTests, 8 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
