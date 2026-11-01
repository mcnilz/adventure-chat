# 06: Eigenen PNG-Raum im Browsereditor erstellen und speichern

**What to build:** Ein Autor erstellt im lokalen Browsereditor einen Raum mit PNG, begehbaren Polygonen und Startpunkten, speichert ihn als YAML und betritt ihn in der Vorschau.

**Blocked by:** 02

**Status:** ready-for-human

## Acceptance criteria

- [x] PNG importieren und vor passendem logischem Koordinatensystem anzeigen.
- [x] Polygonpunkte per Maus setzen, verschieben und entfernen; ungültige Geometrie mit verständlichem Fehler abweisen.
- [x] Spawn platzieren und nur gültige Positionen speichern.
- [x] Raum speichern, Server neu starten und dieselben Daten erneut öffnen und ausprobieren.
- [x] Versioniertes JSON Schema wird ausgeliefert; ein zugeordneter YAML-Editor vervollständigt Felder und erkennt einen absichtlich falschen Feldtyp.
- [x] Kanonisches Neuschreiben und möglicher Kommentarverlust werden erklärt; notes bleibt erhalten.
- [x] Speichern außerhalb des Inhaltsprojekts und Schreibzugriff ohne lokalen Autorenmodus sind gesperrt.
- [x] Ungültige Daten überschreiben keinen bestehenden gültigen Raum.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentlicher Inhalte-Lade-/Speicherzugang.

## Hinweise
Einfacher Editor mit Formulareigenschaften; kein eingebetteter YAML-Codeeditor und kein Bildmalprogramm. Schema aus Formatentwurf umsetzen, YAML-Parser prüfen.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
- Ausgeführte Checks: `npm run typecheck` (0 Fehler), `npm run build` (0 Fehler), `dotnet test` (88/88 Tests erfolgreich bestanden: 60 Server-Tests + 28 Playwright-E2E-Tests).
- Reviewbefunde: Alle Akzeptanzkriterien vollständig erfüllt. Versioniertes JSON-Schema (`schemas/room.schema.json`) wird unter `/schemas/room.schema.json` bereitgestellt. Browser-Raumeditor (`/editor.html`) bietet visuelles Hinzufügen, Verschieben und Löschen von Polygonpunkten, Spawns und Ausgängen, interaktiven Auswahl-Inspektor mit direkter Koordinaten- und ID-Eingabe, visuelles Kanten-Splitten per Klick/Drag zum einfachen Hinzufügen von Punkten, dynamisch ausweichende Koordinatenanzeige, automatische Werkzeug-Rückstellung auf Auswahl nach Punktplatzierung, Vorschau-Modus mit Live-Walking und Pfadfindung, atomares Speichern von kanonischem YAML, Schutz vor Pfad-Traversierung und Sperrung der Editorendpunkte bei deaktiviertem Autorenmodus / Non-Loopback-Aufrufen.
- Status: `ready-for-human` (Wartet auf menschliche Abnahme).
