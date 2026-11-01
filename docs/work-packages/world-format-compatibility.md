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
