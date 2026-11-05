# Aktueller Stand
Revision 41 · 05.11.2026.

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
- **Ticket 16: ready-for-human.** Native128-Lieferung am 05.11.2026 eingebunden: gelieferte Mara-/Bruno-Sheets bytegleich, Raster 128 x 128 bei Raumhoehe 128, sichtbarer Koerper 125 Pixel; keine Vergroesserung mehr im Renderer. Inhalt 2.0.2, Format 1.4.0 unveraendert. Typcheck/Clientbuild und Vollsuite 234 Tests (0 Warnungen) bestanden; drei gezielte Browserfaelle und pixelgenauer Vergleich aller 40 Richtungs-/Phasenansichten gruen. Beide separaten Subagent-Reviewachsen gegen 382a83f ohne offene Befunde. Lieferquellen und Originale erhalten; Python-Lieferwerkzeug bleibt lokal/unversioniert und wird nicht benoetigt. Profil-Gegenphasen, Standpose und Bewegungsgefuehl bleiben menschliche Sichtabnahme. Aktuelle Screenshots und Details im Ticket 16; lokaler Commit Integrate native128 reference characters.
- **Ticket 15: done.** Menschlich am 04.11.2026 nach Start der Testwelt foreground abgenommen („funktioniert“). Implementierungscommit e485525. Am 04.11.2026 mit „ok“ für Umfang, Datenvertrag und Testgrenzen bestätigt und umgesetzt: Vordergrundobjekte mit PNG-Alpha/Tiefenlinie, gemeinsamer Spiel-/Editorrenderer, Objektverwaltung/Upload/Alt-Klick. Format 1.4.0 additive Minor-Erweiterung; alte Schemas/Fixtures und Benutzerbilder unverändert. Typcheck/Build, Vollsuite 230 Tests (0 Warnungen), 14 gezielte Inhaltsfälle und drei Objekt-Browsertests einschließlich Reviewnachträgen erfolgreich. Beide Reviewachsen nach Korrekturen ohne offene Funktions-/Standardsbefunde. Abnahme und Details im Ticket 15.
- **Ticket 14: `done`.** Menschlich am 04.11.2026 abgenommen („funktioniert“). Name/Figur vor Eintritt, Katalog 1.3.0, gemeinsamer Renderer und Editor umgesetzt. Typcheck/Build, 213 Tests (0 Warnungen), beide Reviewachsen ohne offene Befunde. Implementierungscommit `8761c42`. Details im Ticket 14. Ticket 15 ist done; Ticket 16 ist ready-for-human.
- **Reihenfolge:** Ticket 16 ist ready-for-human; nach dessen Abnahme Ticket 13 (draft) vorschlagen. Ticket 13 ist kein Blocker für 16.
- **Benutzerdateien:** gelieferte Lounge-/Garten- und Figurenoriginale unverändert erhalten; Design-Guide-Integrationshinweise aktualisiert. Spielkopien und Tischmasken gesondert in Ticket 16 erzeugt. Alte Referenzwelt aus b3d0b83 unter `tests/Fixtures/reference-v1` bytegetreu erhalten. Benutzer-Arbeitskopie `.scratch/foreground-demo` unangetastet und nicht mitcommittiert.
- **Ticket 12: `done`.** Benutzer hat am 04.11.2026 ausdrücklich klargestellt: „ticket 12 (scrolling) hatte ich schon abgenommen.“ Abnahme einschließlich dokumentiertem Randsprechblasen-Nachtrag nachgetragen. Implementierungscommits `6dd96c7` und `a046edb`. Horizontale Kamera, Format 1.2.0, Editor und Scrollwelt implementiert; abschließende Nachtrags-Vollsuite 192 Tests, 0 Warnungen; Typcheck/Build, beide Reviewachsen und Diffprüfung bestanden. Details im Ticket 12.

- **Ticket 11: `done`.** Bestätigt am 04.11.2026 („ja“), implementiert und geprüft. Kartenwelt `worlds/map`, Draufsichtfigur, feste Höhe 16–128 Raumpixel (Standard 64), separate Geschwindigkeit (Standard 130), Editor und gemeinsame Vorschau, Format 1.1.0 mit unveränderten alten Schemas/Referenzwelten. Vollsuite 183 Tests grün; beide Reviewachsen nach Sackgassenkorrektur ohne offene Befunde. Menschlich am 04.11.2026 abgenommen („funktioniert super“). Details und konkrete Abnahmeschritte: `.scratch/adventure-chat/issues/11-map-view.md`. Implementierungscommit: `a3497dc`.
- Ticket 10 ist `done`, ausdrücklich am 04.11.2026 abgenommen („abegnommen“), Implementierungscommit `28e3b7f`. Tickets 01–09 bleiben `done`; dokumentierte Reviewpunkte von Ticket 09 bleiben offen.

## Nächste Aktion
Menschliche Sichtabnahme von Ticket 16 anhand der konkreten Schritte im Ticket. Erst ausdrückliche Abnahme macht es done; Feedback setzt es zurück auf in-progress. Danach Ticket 13 vorschlagen und vor Umsetzung bestätigen lassen. Benutzer-Arbeitskopie .scratch/foreground-demo erhalten.

## Offene Annahmen
Privater Testbetrieb, ca. zehn Personen, Desktop-Mausbedienung; keine kollidierenden Figuren; Reconnect als frischer Eintritt.

## Checks
- Ticket 14, 04.11.: 14 gezielte Inhaltsfälle und sieben neue Browsertests grün; abschließende Vollsuite 213 bestanden, 0 Warnungen. Typcheck/Build und beide Subagent-Reviews bestanden; Screenshot- und Diffprüfung erfolgreich. 1.3.0 als additive Minor-Erweiterung, keine neuen Abhängigkeiten. Alle fünf Dateien der Legacy-Fixture unabhängig bytegetreu gegen b3d0b83 geprüft. Tatsächliche Referenzfiguren erst in Ticket 16.
- Ticket 12, 04.11.: Rot/Grün-Nachweise für neuen Weltlader und öffentliche Kamera-/Koordinatenberechnung; drei Scroll-Browsertests für zwei Ausschnitte, Chat, Türen, echten Reconnect und Editor inklusive breiter Neuanlage/Speichern/Neuladen grün. Weltvalidierungs-/Scrolltests gezielt erfolgreich; Typcheck und Build bestanden. Abschließende Vollsuite: 191 bestanden, 0 Warnungen. Standards- und Spec-Befunde korrigiert und nachgeprüft, keine offen. Keine neuen Abhängigkeiten. Ältere Schemas/Teilschemas und Referenzfixtures unverändert. Details im Ticket 12.
- Ticket 11, 04.11.: Typcheck und Clientbuild bestanden. Drei dokumentierte Rot/Grün-Nachweise für neues Format, alte Editorgrenze und Sackgassenwand. Vier gezielte Kartentests bestanden, einschließlich Renderer bei 32/48 Pixeln in vier Richtungen, Stand/Schritt/Fußpunkt, zwei Browser und Editor-Vorschauweg. Abschließende Vollsuite nach Reviewkorrektur: 183 bestanden, keine Warnungen. Standards-Review ohne Befund; Spec-Review fand fehlende begehbare Sackgasse, korrigiert und nachgeprüft. Screenshotprüfung Spiel/Editor bestanden. Keine neuen direkten Abhängigkeiten; Minor-Format und Unterstützungsmatrix dokumentiert. EPERM beim eingeschränkten Browserstart durch Testlauf mit Ausführungsrechten aufgelöst. Details im Ticket.

- Ticket 10, 04.11.: Weltwahl-/Wechseltest, Editor-Speichertest und initialer Verbindungsfehler jeweils zuerst rot, dann grün. Fünf gezielte Mehrwelt-Browsertests bestanden, einschließlich Tür/Reconnect/Disconnect mit fremder Welt, Bildzuordnung in Bearbeitung und Vorschau und Bereinigung alter Gruppen. Typcheck/Build bestanden. Abschließende Vollsuite: 172 bestanden, keine Warnungen; Diffprüfung sauber. Beide Subagent-Reviews korrigiert und nachgeprüft. Menschlich am 04.11.2026 ausdrücklich abgenommen. Frühere Umgebungsfehler (Browserstartrechte, Dateisperre bei überlappenden Tests) sind im Ticket dokumentiert.
- Ticket 09: Am 02.11. TypeScript-Typcheck, vollständiger .NET-/Browserlauf (163 bestanden), externe Browserprüfung und 31 gezielte Weltpakettests erfolgreich. Vorher Locked Restore offline und Build erfolgreich. Symbolischer Datei-Link-Test mangels Windows-Berechtigung übersprungen; Junction-Test bestanden. Details im Ticket.
- Ticket 08: Client-Typcheck und Build, Locked Restore, .NET-Build, 87 Server- und 43 Browsertests erfolgreich. Ein vorheriger Gesamtlauf hatte einen Browserstartfehler; die getrennten abschließenden Suiten bestanden.

## Übergabeformat für spätere Änderungen
Aktives Ticket, letzter nachgewiesener Stand, ausgeführte Checks und Ergebnisse, konkrete Blocker, nächster Schritt sowie Referenz auf Benutzerabnahme hier knapp festhalten. Erledigte Detailhistorie gehört ins jeweilige Ticket.
