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
- **Status:** `in-progress` (Parameter und Testgrenzen bestätigt, Implementierung gestartet)
- **Geplanter Umfang:** Zweiter Beispielraum ("Tavernen-Garten"), Ausgänge/Türen mit Navigation & automatischem Raumwechsel bei Ankunft, SignalR-Raumgruppenwechsel, persönlicher Chatverlauf mit Raumwechsel-Systemnachrichten, strikte Trennung von Chat & Bewegung zwischen Räumen.
- **Letzter Stand:** Parameter mit Benutzer abgestimmt (Laufen & Wechseln, persönlicher Verlauf mit Systemmeldungen, Tavernen-Garten als 2. Raum).

## Nächste Aktion
1. Implementierung von Ticket 05 (Modelle, Server-Hub/Manager, Renderer, Client-Navigation, Tests).
2. Ausführung von Build, Typecheck und automatisierten Tests (NUnit & Playwright).
3. Bereitstellung der Ergebnisse zur menschlichen Abnahme (`ready-for-human`).

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `npm run build`: 0 Fehler (SignalR Client-Bundle nach wwwroot kopiert).
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 53 Tests erfolgreich bestanden (34 ServerTests, 19 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
