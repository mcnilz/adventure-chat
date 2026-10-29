# 06: Eigenen PNG-Raum im Browsereditor erstellen und speichern

**What to build:** Ein Autor erstellt im lokalen Browsereditor einen Raum mit PNG, begehbaren Polygonen und Startpunkten, speichert ihn als YAML und betritt ihn in der Vorschau.

**Blocked by:** 02

**Status:** draft

## Acceptance criteria

- [ ] PNG importieren und vor passendem logischem Koordinatensystem anzeigen.
- [ ] Polygonpunkte per Maus setzen, verschieben und entfernen; ungültige Geometrie mit verständlichem Fehler abweisen.
- [ ] Spawn platzieren und nur gültige Positionen speichern.
- [ ] Raum speichern, Server neu starten und dieselben Daten erneut öffnen und ausprobieren.
- [ ] Versioniertes JSON Schema wird ausgeliefert; ein zugeordneter YAML-Editor vervollständigt Felder und erkennt einen absichtlich falschen Feldtyp.
- [ ] Kanonisches Neuschreiben und möglicher Kommentarverlust werden erklärt; notes bleibt erhalten.
- [ ] Speichern außerhalb des Inhaltsprojekts und Schreibzugriff ohne lokalen Autorenmodus sind gesperrt.
- [ ] Ungültige Daten überschreiben keinen bestehenden gültigen Raum.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentlicher Inhalte-Lade-/Speicherzugang.

## Hinweise
Einfacher Editor mit Formulareigenschaften; kein eingebetteter YAML-Codeeditor und kein Bildmalprogramm. Schema aus Formatentwurf umsetzen, YAML-Parser prüfen.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

