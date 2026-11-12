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

## Raumobjekte seit Weltformat 1.4.0 (Ticket 15)

Unterstützung: ausdrücklich 1.0.0, 1.1.0, 1.2.0, 1.3.0 und 1.4.0. Neues Offline-Bündel unter schemas/world/1.4.0. Minor-Erweiterung: objects ist optional; ohne Liste bleiben Darstellung, Bewegung und Defaults älterer Welten erhalten. Alte Schemas einschließlich Teilschemas und Referenzfixtures bleiben unverändert. Alte Server dürfen 1.4.0 ablehnen.

Raum-YAML:

```yaml
objects:
  - id: table
    image: assets/images/table.png
    position: { x: 200, y: 200 }
    size: { width: 120, height: 120 }
    depthY: 280
```

Maximal 128 Objekte; IDs sind eindeutig und groß-/kleinschreibungssensitiv, 1–64 ASCII-Buchstaben, Ziffern, Unterstrich oder Bindestrich. Position ist links oben. Position, Zielgröße und depthY sind ganzzahlige logische Raumpixel. Positive Zielgröße und gesamtes Rechteck müssen in den Raum passen; depthY liegt zwischen 0 und Raumhöhe einschließlich. Quellgröße darf von Zielgröße abweichen. PNG unter assets/images mit bestehenden Pfad-, Link-, Datei- und Dekodierlimits; Upload maximal 10 MiB, Inhaltsprüfung vor Raumübernahme.

Objekte und Figuren werden gemeinsam nach Raum-Y gezeichnet. Figuren mit Fußpunkt unter depthY erscheinen vor dem Objekt, ebenso bei Gleichstand. Objekte gleicher Tiefe werden nach ordinaler ID aufsteigend gezeichnet. PNG-Alpha bleibt erhalten, Namen und Sprechblasen folgen darüber. Kamera und Bildschirmskalierung verändern diese Ordnung nicht. Grafische Objekte erzeugen keine Kollisionsflächen.

Editor: Bereich „Raumobjekte“, Auswahlliste für überlappende Motive, Objekt anlegen, PNG-Pfad oder vorhandenes PNG zuordnen/hochladen, Werte bearbeiten, per Maus verschieben und löschen. Umriss und Tiefenlinie erscheinen in der Bearbeitung. Die Vorschau verwendet denselben Objektzeichner wie das Spiel. Ein bestehendes Weltmanifest wird nicht automatisch hochgestuft. Kleine synthetische Testwelt: tests/Fixtures/foreground; Referenzweltgestaltung folgt in Ticket 16.

## Tiefenverlauf seit Weltformat 1.5.0
Server akzeptiert ausdrücklich 1.0.0, 1.1.0, 1.2.0, 1.3.0, 1.4.0 und 1.5.0. Letzteres ergänzt optionale depthScale-Daten mit Faktor 1 als kompatiblem Default. Minor-Erweiterung, keine Änderung der Bedeutung älterer Formate. Details und Grenzen stehen in CONTENT-FORMAT.md. Alle früheren Schema-Bündel und tests/Fixtures bleiben unverändert. Mitgelieferte Demo-Welten sind weiterentwickelte Inhalte: reference 2.1.0, map und scroll 1.1.0 (zusätzliche Räume und Verbindungen). Sie sind keine eingefrorenen Kompatibilitätsfixtures.


### Optionale Draufsicht-Sheets (1.5.0)
Am 09.11.2026 ausdrücklich bestätigt: Ein Figurenkatalogeintrag darf zusätzlich topDownImage nennen. Es gelten dieselben PNG-/Pfadgrenzen und dasselbe Quellraster wie image (vier Spalten, Reihen rechts/hinten/vorne, links gespiegelt). In top-down-Räumen wird dieses Sheet verwendet; fehlt es, bleibt die bisherige Katalogfigur erhalten. In Adventure-Räumen gilt immer image. Auswahl und Figuren-ID bleiben über Übergänge erhalten. Dies ist ebenfalls eine optionale Minor-Erweiterung; frühere Formate erlauben das Feld nicht.

## Interaktive Objekte und weitere Planung (20–23)

Ticket 20 erweitert die ausdrückliche Unterstützung um 1.6.0. Die neuen Felder sind optional und nur im neuen Schema zulässig. Ältere Bündel und Referenzfixtures bleiben unverändert; neue Server behalten deren bisherige Darstellung und Navigation. Das ist eine additive Minor-Erweiterung. Maßgeblich sind [Formatvertrag](../../CONTENT-FORMAT.md), [ADR 0002](../adr/0002-declarative-object-interactions.md) und Tickets 20–23.

| Geplantes Paket | Vorgesehene Formatversion | SemVer-Begründung / Nachweis vor Veröffentlichung |
| --- | --- | --- |
| 20: Zustände, Aktionen, direkte Verknüpfungen | 1.6.0 | Minor nur mit optionalen Angaben; fehlend = weiterhin grafische Objekte ohne Aktionen. Alte Darstellung/Navigation unverändert. Neues eigenes Schema-Bündel und explizite Matrix. |
| 21: Hindernisse und Ausgangssperren | 1.7.0 nach veröffentlichtem 1.6.0 | Weitere optionale Minor-Erweiterung; keine Änderung alter Fußpunkt-/Bodenregeln ohne neue Angaben. Eigenes Bündel; veröffentlichtes 1.6.0 nicht nachträglich erweitern. |
| 22: Verben und Editor | keine weitere geplant | Bereits veröffentlichte Verträge bedienen/erhalten. Neues Feld oder neue Interpretation würde erneut eine Versionsentscheidung benötigen. Kein stilles Hochstufen alter Welten. |
| 23: Referenzbeispiele | Format aus 21, geplant 1.7.0 | Inhaltsversion unabhängig begründen: aktuell reference 3.0.0; additive Szene bei Erhalt aller Zusagen Minor, geänderte bestehende Navigations-/Spawnsemantik Major. |

Versionsnummern bei Paketfreigabe gegen den tatsächlichen Stand prüfen. Eine spätere andere Reihenfolge darf keinen veröffentlichten Vertrag verändern. Bis Implementierung und Prüfung akzeptiert kein Server diese geplanten Versionen allein wegen dieses Dokuments.

Nachweispflicht je Formatpaket: veröffentlichte Schemas einschließlich aller Teilschemas unverändert vergleichen; vorhandene ältere Referenzfixtures unverändert laden und ihre zugesicherte Darstellung, Navigation und Defaults ausführen. Insbesondere tests/Fixtures/reference-v1 sowie die vorhandenen versionsbezogenen Fixtures verwenden, keine erwarteten Altwerte an das neue Verhalten anpassen. Neue Fälle in neuen synthetischen Welten ergänzen; ältere Weltstände in temporären Kopien durch öffentliche Lade-/Speichergrenzen prüfen. Neue Felder unter alter Version, nicht unterstützte Versionen und inkonsistente Zustandsreferenzen gezielt abweisen. Erweiterung darf kein bestehendes gültiges älteres Dokument neu ablehnen; andernfalls Major-Frage vor Umsetzung klären.

Dieser Planungsdiff selbst verändert weder Schemas/Teilschemas noch Fixtures, Welten oder deren Interpretation. Die spätere Referenzintegration erfolgt ausschließlich in Ticket 23 nach Abnahme von 19.
