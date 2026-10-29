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
- [x] Ticket 03 (Echtzeit-Multiplayer mit SignalR) vollständig umgesetzt und getestet (Status: `ready-for-human`).
- [ ] Ticket 04 (Raum-Chat) umsetzen.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `03-multiplayer.md` (Zwei Browser sehen einander laufen)
- **Status:** `ready-for-human`
- **Letzter Stand:** SignalR-Hub `/hub/room` und `RoomSessionManager` in ASP.NET Core implementiert, TypeScript-Client angebunden mit Multi-User-Rendering, Y-Tiefensortierung, Namensschildern und Echtzeit-Wegpunktsynchronisation.
- **Ausgeführte Checks:** `tsc --noEmit` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (39/39 Tests erfolgreich: 25 ServerTests + 14 E2ETests).

## Nächste Aktion
Menschliche Abnahme von Ticket 03 einholen. Nach Bestätigung:
1. Status von `03-multiplayer.md` auf `done` setzen.
2. Ticket 04 (`04-room-chat.md` - Sprechblasen und Raum-Chat) vorschlagen und vorbereiten.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt. Exakte Chatlimits, Schemaentwurf und Bibliotheksversionen vor betroffenen Paketen festlegen.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 39 Tests erfolgreich bestanden (25 ServerTests, 14 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
