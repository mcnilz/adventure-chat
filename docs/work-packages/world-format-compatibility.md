# Architekturziel: stabile, externe und versionierte Weltdefinitionen

Status: fachlich im Gespräch bestätigt; Implementierung folgt in Tickets 09 und 10.

## Vertrag

Eine Welt ist ein eigenständiges Inhaltspaket aus Manifest, Räumen und Bildern. Anwendungssoftware und eigene Inhalte werden unabhängig ausgeliefert. Eine Referenzwelt bleibt Bestandteil des Repositorys. Laufender Zustand (Positionen, Anwesenheit, Bewegung, Chat) wird nicht dauerhaft gespeichert; Neustart bedeutet frischen Eintritt am Start-Spawn.

Eine Formatversion beschreibt das öffentliche Autorenformat einschließlich dokumentierter Interpretation. Eine Inhaltsversion beschreibt die Veröffentlichung einer konkreten Welt. Beide verwenden Major.Minor.Patch. Serverversion, Weltformatversion und Inhaltsversion sind voneinander unabhängig. Die Inhaltsversion allein entscheidet nie über Engine-Kompatibilität. Eine eigenständige, frei kombinierbare Schemaversion wird vermieden: Schemaversion entspricht der zugehörigen Formatversion.

## SemVer und Richtung der Kompatibilität

| Änderung am Formatvertrag | Versionsfolge |
| --- | --- |
| Pflichtfeld hinzufügen, Feld entfernen/umdeuten, bisher gültige Daten ablehnen, zugesichertes Verhalten ändern | Major |
| Optionale Erweiterung mit eindeutigem Standardverhalten, alle bisherigen gültigen Dokumente behalten Bedeutung | Minor |
| Kompatible Korrektur/Klarstellung ohne Änderung der zugesicherten Bedeutung oder Ausschluss gültiger Daten | Patch |

Ein vermeintlicher Bugfix kann einen Major-Wechsel erfordern, wenn er den veröffentlichten Vertrag bricht. Engere Wertebereiche und geänderte Defaults sind ausdrücklich zu bewerten. Neue Funktionen sind nicht automatisch Minor-kompatibel.

Neuere Server müssen bestehende Welten innerhalb einer unterstützten Major-Version weiterhin korrekt laden und ausführen. Die umgekehrte Richtung ist nicht zugesichert: alte Server dürfen neuere Minor-Versionen ablehnen. Eine explizite, getestete Unterstützungsmatrix bestimmt akzeptierte Formatversionen; kein Vergleich mit der Anwendungsversionsnummer und kein pauschales Akzeptieren nach Major-Gleichheit. Bei Einführung einer neuen Major-Version werden bisherige Unterstützungszusagen nicht stillschweigend gestrichen; Supportentscheidung und Übergang müssen ausdrücklich dokumentiert werden.

## Unveränderliche Schemas

Jede veröffentlichte Formatversion erhält ein eingefrorenes Schema-Bündel mit eindeutigem $id und lokalen Referenzen, beispielsweise schemas/world/1.0.0/world.schema.json und room.schema.json. Auch referenzierte Teilschemas bleiben unverändert. Der veröffentlichte Vertrag umfasst Datenstruktur, Defaults und Verhalten. Beispielpfade sind Planung, keine bereits verfügbare Schnittstelle.

Der Server liest zunächst nur die begrenzte Versionsdeklaration, prüft Unterstützung, wählt das passende lokale Bündel und prüft dann sämtliche YAML-Dokumente und fachlichen Beziehungen. Unbekannte Versionen erhalten kein Fallback. YAML wird sicher entsprechend CONTENT-FORMAT.md eingelesen. Externe $ref-Auflösung ist abgeschaltet.

Die Formatversion des Manifests legt die zusammengehörigen Dokumentregeln fest. Room-Versionen und Legacy-Zuordnung müssen dazu ausdrücklich passen. Das vorhandene ganzzahlige version: 1 darf nicht still in SemVer umgedeutet werden. Ein begrenzter, getesteter Legacy-Lesepfad ermöglicht Übernahme alter Raumdateien; automatische Migration bleibt außerhalb des Umfangs.

Versionierte Schema-IDs und dokumentierte lokale Editorzuordnungen ermöglichen Completion auch bei externen Weltordnern. Schema-Kommentare dienen dem Texteditor; der Server vertraut ihnen nicht. Fehlermeldungen unterscheiden nicht unterstützte Version, Schemafehler und fachliche Inkonsistenz.

## Änderungspflicht und Tests

Jede Änderung, auch eine reine Verhaltensänderung ohne YAML-Strukturänderung, beantwortet im Ticket und Review:

1. Welche veröffentlichten Formatverträge und älteren Welten sind betroffen?
2. Bleiben bisher gültige Daten und zugesicherte Semantik erhalten?
3. Welche SemVer-Einstufung folgt daraus und warum? Falls kein Formatbezug besteht: kurz begründen.
4. Welche unveränderten Referenzfixtures weisen Kompatibilität nach?
5. Welche Schemas, Versionsmatrix, Editorfunktionen und Dokumente müssen ergänzt werden?

Ältere Fixtures nicht auf das aktuelle Format umschreiben, um Tests grün zu machen. Parsererfolg allein beweist keine Verhaltenskompatibilität: Startpunkt, Raumwechsel und dokumentierte Defaults über öffentliche Grenzen prüfen. Neue veröffentlichte Schema-Bündel hinzufügen; alte nicht überschreiben. Keine generische Pluginplattform, Migrationsengine oder separate Schema-Registry nötig.


## Unterstützung seit Ticket 11

| Paketformat | Raumschema | Verhalten |
| --- | --- | --- |
| 1.0.0 | unverändertes Bündel 1.0.0, legacy-1 | Adventure, 64 Pixel, 130 Pixel/s; neue Felder abgewiesen |
| 1.1.0 | neues Bündel 1.1.0, legacy-1 | optionale Perspektive, feste Höhe und Geschwindigkeit |
| 1.2.0 | neues Bündel 1.2.0, legacy-1 | zusätzlich optionaler horizontaler Ausschnitt; fehlend = ganzer Raum |
| andere | keine | ausdrücklich abgewiesen |

Minor-Einstufung: ausschließlich optionale Felder mit Defaults, die die bisherige Semantik erhalten. Kein bestehendes Schema, keine ältere Referenzwelt und kein Teilschema wird verändert. `worlds/reference` und `content/rooms` bleiben die älteren Fixtures; `worlds/map` demonstriert 1.1.0. Tests prüfen Schema-Hashes, unveränderte Dateien sowie Laden und Editor-Speichern.

Die Figurengröße ist kein Kollisionsradius. Geprüft wird der Fußpunkt im erlaubten Polygon; Sichtlinien meiden Hindernisecken mit 4 Pixeln, Navigationsknoten liegen typischerweise 14 Pixel von Ecken entfernt. Das ist kein zugesicherter umlaufender Figurenradius. Eine große Figur kann optisch über eine Hecke ragen, während ihr Fußpunkt gültig bleibt. Die Referenzkarte prüft den 15 Pixel breiten unteren Durchgang und ein eingeschlossenes, unerreichbares Ziel. Kein Algorithmuswechsel.

Ticket 12: Format 1.2.0 ist Minor, weil ausschließlich das optionale Feld `viewport` hinzukommt. Fehlende Angaben behalten die Darstellung und Raumkoordinaten älterer Versionen. Der Server unterstützt ausdrücklich 1.0.0, 1.1.0 und 1.2.0; die alten Bündel einschließlich Teilschemas und Referenzwelten bleiben unverändert. `worlds/scroll` ergänzt beide Perspektiven. Kameraverschiebung ist ausschließlich Browserzustand; kein zusätzlicher Hubparameter oder Sitzungszustand.

## Figurenkatalog seit Weltformat 1.3.0

Unterstützt werden ausdrücklich 1.0.0, 1.1.0, 1.2.0 und 1.3.0; andere Versionen werden abgewiesen. Neues unveränderliches Bündel: `schemas/world/1.3.0/`. Minor: optionale Manifestfelder, unveränderte Raumregeln und unveränderte Defaults älterer Welten. Frühere Schemas einschließlich Teilschemas bleiben unverändert.

```yaml
formatVersion: 1.3.0
# übrige Manifestfelder wie bisher
defaultCharacterId: hero
characters:
  - id: hero
    name: Meine Figur
    image: assets/images/hero.png
    frameWidth: 64
    frameHeight: 96
```

`characters` und `defaultCharacterId` stehen gemeinsam oder fehlen gemeinsam. Ein Katalog hat 1–32 Figuren mit eindeutigen, groß-/kleinschreibungssensitiven IDs (ASCII-Buchstaben, Ziffern, Unterstrich, Bindestrich; 1–64 Zeichen), Anzeigenamen (1–100 Zeichen), relativem PNG-Pfad unter `assets/images/` und ganzzahliger Framebreite/-höhe von 1–256. Default-ID muss enthalten sein. Ein vollständiges PNG muss exakt vier Frames breit und drei Frames hoch sein. Reihen: rechts, hinten, vorne; links spiegelt rechts. Stand verwendet Spalte 0, Laufen zyklisch 0–3. Keine zusätzlichen Animationsmetadaten. Bestehende PNG-, Größen- und Dateisystemschutzregeln gelten.

Katalogfiguren werden auch in Draufsichträumen verwendet. Die Raum-Figurengröße bestimmt die Zielhöhe, das Quellseitenverhältnis bleibt erhalten; Fußpunkt bleibt unten mittig. Ohne Katalog bleiben bisherige Adventurefigur und prozedurale Draufsichtfigur erhalten. Editor-Vorschau verwendet die Defaultfigur mit demselben Renderer. Grafische Katalogverwaltung ist nicht enthalten.

Teilnehmer wählen Name und Figur vor Eintritt; gleiche Figuren dürfen mehrfach vorkommen. Server akzeptiert nur weltbezogene IDs, fehlende ID verwendet den Weltdefault. Auswahl bleibt über Raumwechsel und frischen Reconnect erhalten; bei Weltwechsel wird neu gewählt. Keine Persistenz über Seiten-/Browserneustart. Bildadressen und Browsercache sind weltgebunden.

Kompatibilitätsnachweis: `tests/Fixtures/reference-v1` ist die unveränderte Referenzwelt aus Commit b3d0b83 (vor den Inhaltsänderungen für Ticket 16). Zusätzlich bleiben `content/rooms`, `worlds/map`, `worlds/scroll` und alle älteren Schema-Bündel unverändert. Neue synthetische Testwelten unter `tests/Fixtures/characters-a` und `characters-b` weisen Katalograster und Weltisolation nach.
