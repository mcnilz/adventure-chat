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
- [ ] Ticket 03 (Echtzeit-Multiplayer mit SignalR) umsetzen.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `03-multiplayer.md` (Zwei Browser sehen einander laufen)
- **Status:** `proposed`
- **Letzter Stand:** Ticket 02 (Wegfindung & Hindernisumrundung) erfolgreich abgenommen. Vorbereitung für Ticket 03 (SignalR-Hub, Mehrspieler-Synchronisation im Raum, Verbindungsaufbau und Disconnect-Handling).
- **Ausgeführte Checks:** `tsc --noEmit` (0 Fehler), `dotnet build` (0 Warnungen/Fehler), `dotnet test` (24/24 Tests erfolgreich: 15 ServerTests + 9 E2ETests).

## Nächste Aktion
Bestätigung des Umfangs für Ticket 03 einholen, anschließend SignalR-Hub und Client-Synchronisation implementieren.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt. Exakte Chatlimits, Schemaentwurf und Bibliotheksversionen vor betroffenen Paketen festlegen.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 24 Tests erfolgreich bestanden (15 ServerTests, 9 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
