# Aktueller Stand
Revision 19 · 01.11.2026.

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
- [x] Ticket 06 (Raumeditor und YAML-Speicherung) vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [x] Ticket 07 (Selbst erstellte Räume verbinden und gemeinsam nutzen) vollständig umgesetzt, getestet und menschlich abgenommen (Status: `done`).
- [x] MVP-Gesamtumfang gemäß SPEC.md vollständig implementiert und getestet.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.

## Aktives Ticket
- **Ticket 08:** `in-progress`; Basis `ef43f41`. Bewegung, Ausgangsprüfung, Ereignisbezug, Reconnect, Autorenmodus und YAML-Startprüfung bearbeitet. Einzelne Regressionen einschließlich echter Wiederverbindung bestanden. Locked Restore, Clientbuild und Server-Suite (87 Tests) bestanden; Browser-Gesamtsuite und Review nach letzten Änderungen laufen noch. Details im Ticket `.scratch/adventure-chat/issues/08-mvp-stabilization.md`.
- **Vorherige Tickets:** 01 bis 07 sind menschlich abgenommen (`done`).
- **Historischer Stand vor Ticket 08:** 106 Tests wurden für Tickets 01–07 gemeldet. Diese Zahl ist kein Nachweis für den aktuellen Arbeitsstand.

## Nächste Aktion
Ticket 08: laufende Browser-Gesamtsuite abschließen, vollständigen Review gegen `ef43f41` durchführen und dessen Befunde bearbeiten; erst danach menschliche Abnahme vorbereiten.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- Ticket 08: Client-Typcheck und Build, Locked Restore sowie .NET-Build erfolgreich; Server-Suite zuletzt 87 Tests erfolgreich. Browser-Gesamtlauf nach Änderungen und Review sind noch offen.

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
