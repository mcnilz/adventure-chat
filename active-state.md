# Aktueller Stand
Revision 16 · 01.11.2026.

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
- [x] Ticket 05 (Raumübergänge und Türen) vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [x] Client-Codebase modularisiert (Single-Responsibility-Module für Netzwerk, Zustand, Simulation, Input, Renderer-Ebenen und UI).
- [x] Clean Code & AI-Agent Guidelines in `docs/agents/coding-standards.md`, `AGENTS.md` und `ARCHITECTURE.md` hinterlegt.
- [x] Versioniertes JSON Schema (`schemas/room.schema.json`) und kanonisches YAML-Autorenformat implementiert.
- [x] Lokaler Browser-Raumeditor (`wwwroot/editor.html`) mit Polygon-, Spawn- und Ausgangs-Tools, Validierung und Vorschau-Modus umgesetzt.
- [x] Usability-Optimierungen im Raumeditor: Auswahl-Inspektor für aktive Elemente, Kanten-Splitten per Klick/Drag zum einfachen Hinzufügen von Bodenpunkten, dynamische reflexionsfreie Koordinatenanzeige, automatische Werkzeugrückstellung auf Auswahl und Tooltips für alle Bedienelemente.
- [x] Live-Aktualisierung der Zielräume und Spawns im Ausgang-Dialog nach Raumneuanlage behoben.
- [x] Generierte Client-Build-Artefakte (`wwwroot/js/`) in `.gitignore` aufgenommen und aus dem Git-Tracking entfernt.
- [x] Ticket 06 (Raumeditor und YAML-Speicherung) vollständig umgesetzt und getestet (Status: `ready-for-human`).
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.
- [ ] MVP abgenommen.

## Aktives Ticket
- **Ticket:** `06-room-editor.md` (Eigenen PNG-Raum im Browsereditor erstellen und speichern)
- **Status:** `ready-for-human` (wartet auf menschliche Abnahme durch den Benutzer)
- **Letzter Stand:** Versioniertes JSON Schema `schemas/room.schema.json` bereitgestellt (`/schemas/room.schema.json`). Backend-Dienste für YAML-Serialisierung (`YamlDotNet`), geometrische und semantische Validierung (`JsonSchema.Net`), Pfad-Traversierungs-Schutz und atomares Speichern unter `/api/editor/...` implementiert. Browser-Editor unter `/editor.html` mit Auswahl-Inspektor, interaktivem Kanten-Splitten, dynamischer Koordinatenanzeige, modalem Ausgangs-Dialog mit Live-Raumaktualisierung, ausführlichen Tooltips und Vorschau-Modus mit Live-Laufanimationen umgesetzt.
- **Ausgeführte Checks:** `npm run typecheck` (0 Fehler), `npm run build` (0 Fehler), `dotnet test` (96/96 Tests erfolgreich: 63 ServerTests + 33 E2ETests).

## Nächste Aktion
1. Menschliche Abnahme von Ticket 06 durch den Benutzer (oder Übernahme in Status `done`).
2. Vorschlag des nächsten Tickets (z. B. Ticket 07).

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- `npm run typecheck` in `src/AdventureChat.Client`: 0 Fehler.
- `npm run build`: 0 Fehler (TypeScript kompiliert und SignalR-Bundle kopiert).
- `dotnet build`: 0 Fehler, 0 Warnungen.
- `dotnet test`: 96 Tests erfolgreich bestanden (63 ServerTests, 33 E2ETests).

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
