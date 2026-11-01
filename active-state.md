# Aktueller Stand
Revision 11 · 01.11.2026.

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
- [x] UI-Layout-Fix: Chatleiste als eigenständiger Bereich unterhalb des Canvas angedockt (kein transparentes Overlay auf Gehbereich).
- [x] SCUMM-Aktionszeile ("Walk to" / "Go to [Raumname]") direkt unterhalb des Spielfelds implementiert.
- [x] Visuelle Erkennbarkeit von Raumübergängen geschärft (beleuchtete Holzschilder, Laternen, pulsierende Türrahmen bei Mouseover, vollflächige Tür-Hitboxen).
- [x] Ticket 05 (Raumübergänge und Türen) vollständig umgesetzt und getestet (Status: `ready-for-human`).
- [x] Client-Codebase modularisiert (Single-Responsibility-Module für Netzwerk, Zustand, Simulation, Input, Renderer-Ebenen und UI).
- [x] Clean Code & AI-Agent Guidelines in `docs/agents/coding-standards.md`, `AGENTS.md` und `ARCHITECTURE.md` hinterlegt.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `05-room-transitions.md` (Zwischen zwei Räumen wechseln und getrennt chatten)
- **Status:** `ready-for-human` (wartet auf menschliche Abnahme durch den Benutzer)
- **Letzter Stand:** Modularisierung des TypeScript-Clients abgeschlossen, Clean-Code- und Agenten-Richtlinien hinterlegt. Zweiter Raum ("Tavernen-Garten"), Ausgangs-Navigation mit automatischem Raumwechsel bei Ankunft, SCUMM-Aktionszeile ("Go to [Raumname]"), beleuchtete Schilder und Laternen an Durchgängen, interaktive Tür-Hitboxen mit Mouseover-Highlighting, SignalR-Gruppenmigration und Chat-Isolation implementiert.
- **Ausgeführte Checks:** `npm run typecheck` (0 Fehler), `npm run build` (0 Fehler), `dotnet test` (61/61 Tests erfolgreich: 39 ServerTests + 22 E2ETests).

## Nächste Aktion
1. Menschliche Abnahme von Ticket 05 durch den Benutzer.
2. Nach Abnahme: Ticket 05 auf `done` setzen.
3. Vorbereitung und Start von Ticket 06 (`06-room-editor.md`).

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `npm run build`: 0 Fehler (SignalR Client-Bundle nach wwwroot kopiert).
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 61 Tests erfolgreich bestanden (39 ServerTests, 22 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
