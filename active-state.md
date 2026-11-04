# Aktueller Stand
Revision 25 · 02.11.2026.

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
- [x] Ticket 08 (Multiplayer-Stabilisierung und Autorenbetrieb) menschlich abgenommen (Status: `done`; Benutzer: „passt“, 01.11.2026).
- [x] MVP-Gesamtumfang gemäß SPEC.md vollständig implementiert und getestet.
- [ ] Skills installiert oder im Zielprojekt geprüft.
- [ ] Git-Repository und endgültiger Projektname festgelegt.

## Aktives Ticket
- **Ticket 10: `in-progress`.** Umfang und Testgrenzen bestätigt. Weltliste, weltbezogener Start und PNG-Auslieferung vorhanden. `JoinRoom` akzeptiert optional `worldId`; unbekannte IDs und wiederholter Beitritt werden abgewiesen. Pro Welt eigener Raum-/Sitzungszustand und eigene SignalR-Gruppen. Öffentlicher Hubtest mit zwei Welten und gleichen Raum-IDs weist Snapshot-, Chatverlaufs- und Ereignisisolation sowie Empfang innerhalb derselben Welt nach. Ohne `worldId` nutzt der bestehende Client vorläufig weiterhin die erste konfigurierte Welt. Weltwahl im Browser, Verlassen/Wechsel, Reconnect-Weltauswahl, weltbezogener HTTP-Pfad-Fallback und Editor fehlen noch. Weltbezogene Raumabfragen und Bildadressen in HTTP-Antworten und Hub-Snapshots sind umgesetzt. Kein `ready-for-human`. Ticket 09 ist `done`; seine dokumentierten Reviewpunkte bleiben offen.
- Tickets 01–08 bleiben `done`; Ticket 08 wurde am 01.11.2026 ausdrücklich abgenommen.

## Nächste Aktion
Ticket 10 an den bestätigten öffentlichen Grenzen weiter umsetzen; anschließend vollständige Checks, Review und Abnahmeanleitung. Tickets 11–13 bleiben draft; die übrigen Abhängigkeiten stehen in PLAN.md.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- Ticket 10, Zwischenstand 04.11.: HTTP-Raum-/Bildisolation zuerst rot, dann gr�n; Hubtest pr�ft weltbezogene Bildadressen. Review durch zwei Subagenten (Standards/Spec); DTO-Vertrag erg�nzt und Weltendpunkte ausgelagert. Vollsuite: 168 bestanden, keine Warnungen; nach Strukturkorrektur 36 Weltpakettests sowie Typcheck/Build erfolgreich. Keine manuelle Mehrweltabnahme. Vorheriger Nachweis 02.11.: TypeScript-Typcheck und Client-Build bestanden; vollständiger .NET-/Browserlauf mit 168 erfolgreichen Tests und ohne Warnungen. Neuer Mehrwelt-Hubtest zuerst rot, dann grün; Diff-Whitespaceprüfung ohne Befund. Keine manuelle Mehrweltabnahme; Browserauswahl und weitere offene Teile stehen oben.
- Ticket 09: Am 02.11. TypeScript-Typcheck, vollständiger .NET-/Browserlauf (163 bestanden), externe Browserprüfung und 31 gezielte Weltpakettests erfolgreich. Vorher Locked Restore offline und Build erfolgreich. Symbolischer Datei-Link-Test mangels Windows-Berechtigung übersprungen; Junction-Test bestanden. Details im Ticket.
- Ticket 08: Client-Typcheck und Build, Locked Restore, .NET-Build, 87 Server- und 43 Browsertests erfolgreich. Ein vorheriger Gesamtlauf hatte einen Browserstartfehler; die getrennten abschließenden Suiten bestanden.

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
