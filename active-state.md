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
- [x] Ticket 02 vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [x] Ticket 03 (Echtzeit-Multiplayer mit SignalR) vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [ ] Ticket 04 (Raum-Chat) umsetzen.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `04-room-chat.md` (Raumchat mit Verlauf und Sprechblasen benutzen)
- **Status:** Vorbereitung / Freigabe
- **Letzter Stand:** Ticket 03 wurde erfolgreich abgenommen. Wegfindung wurde um Ecksicherheitsabstände und dynamische Zwischenzielberechnung optimiert.
- **Ausgeführte Checks:** `tsc --noEmit` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (41/41 Tests erfolgreich: 27 ServerTests + 14 E2ETests).

## Nächste Aktion
Ticket 04 (`04-room-chat.md` - Raumchat mit Verlauf und Sprechblasen) vorschlagen, Detailparameter (Limits, Dauer) bestätigen und nach Freigabe implementieren.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt. Exakte Chatlimits (Länge 1-200 Zeichen, Verlauf 50 Einträge, Sprechblase 5-8 Sekunden) in Ticket 04 bestätigen.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 41 Tests erfolgreich bestanden (27 ServerTests, 14 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
