# Aktueller Stand
Revision 8 · 30.10.2026.

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
- [x] Ticket 04 (Raumchat mit Verlauf und Sprechblasen) vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [ ] Ticket 05 (Raumübergänge und Türen) umsetzen.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `05-room-transitions.md` (Zwischen zwei Räumen wechseln und getrennt chatten)
- **Status:** `in-preparation` (wartet auf Bestätigung von Umfang und Testgrenzen vor Implementierungsstart)
- **Geplanter Umfang:** Zweiter Beispielraum (z. B. "Garden" / "Tavern Backyard"), Ausgänge/Türen mit interaktiven Klick-Bereichen, Wegfindung zum Ausgang und automatischer Raumwechsel nach Erreichen, SignalR-Raumwechsel/Umgruppierung, Entleerung des alten Chatverlaufs und Laden des neuen Raum-Snapshots, saubere Ereignisisolation zwischen Räumen.
- **Letzter Stand:** Ticket 04 menschlich abgenommen und abgeschlossen.

## Nächste Aktion
1. Bestätigung von Ergebnis, Detailentscheidungen und Testgrenzen für Ticket 05 durch den Benutzer.
2. Nach Bestätigung: Ticket 05 (`05-room-transitions.md`) auf `in-progress` setzen und umsetzen.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `npm run build`: 0 Fehler (SignalR Client-Bundle nach wwwroot kopiert).
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 53 Tests erfolgreich bestanden (34 ServerTests, 19 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
