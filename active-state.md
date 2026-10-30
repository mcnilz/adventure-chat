# Aktueller Stand
Revision 6 · 29.10.2026.

## Vorhanden
- [x] Gespräch in Spezifikation, Architektur, Autorenformat und Tickets überführt.
- [x] Matt Pococks aktuelle Skill-Quellen geprüft und verlinkt.
- [x] Paketumfang und Testgrenzen für Ticket 01 bestätigt.
- [x] Implementierung von Ticket 01 abgeschlossen und menschlich abgenommen (Status: `done`).
- [x] Git LFS für Bild- und Binärdateien konfiguriert.
- [x] SCUMM / Lucasfilm Grafikstil (Maniac Mansion / Zak McKracken Big-Head Proportionen 48x64, Rücken-/Front-/Seitenansicht, Tavernen-Hintergrund 640x360) umgesetzt.
- [x] Paketumfang und Testgrenzen für Ticket 02 bestätigt.
- [x] Ticket 02 vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [x] Ticket 03 (Echtzeit-Multiplayer mit SignalR) vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [x] Wegfindungs-Optimierungen umgesetzt: 14px Ecksicherheitsabstand und dynamische Zwischenweg-Neuberechnung bei Klicks im Lauf.
- [ ] Ticket 04 (Raumchat mit Verlauf und Sprechblasen) umsetzen.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `04-room-chat.md` (Raumchat mit Verlauf und Sprechblasen benutzen)
- **Status:** ready-to-start / draft (wartet auf Bestätigung des vorgeschlagenen Pakets)
- **Letzter Stand:** Tickets 01, 02 und 03 sind vollständig `done` und im lokalen Git committet. SignalR-Hub `/hub/room` und Client-Synchronisation laufen stabil.
- **Ausgeführte Checks:** `npm run typecheck` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (41/41 Tests erfolgreich: 27 ServerTests + 14 E2ETests).

## Nächste Aktion
Neue Session startet direkt mit der Bestätigung und Umsetzung von Ticket 04 (`04-room-chat.md`).
1. Vorgeschlagene Parameter für Ticket 04:
   - Nachrichtenlänge: 1 bis maximal 200 Zeichen.
   - Verlauf: max. 50 Nachrichten pro Raum (im Arbeitsspeicher).
   - Sprechblasendauer: 6 Sekunden Anzeigedauer über der Spielfigur.
   - Rate-Limit: max. 5 Nachrichten pro 3 Sekunden pro Verbindung.
   - XSS-Schutz: Reiner Text, HTML-Tags werden neutralisiert.
2. Nach Bestätigung Umsetzung in `RoomHub.cs`, `RoomSessionManager.cs`, `RoomRenderer.ts`, `AdventureGame.ts` und `index.html`.
3. Automatisierte Absicherung via NUnit (Server) und Playwright E2E (Multi-Browser-Chat).

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `npm run build`: 0 Fehler (SignalR Client-Bundle nach wwwroot kopiert).
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 41 Tests erfolgreich bestanden (27 ServerTests, 14 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
