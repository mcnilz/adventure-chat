# Inhalte und Autorenformat
Status: Das bestehende Raumformat und schemas/room.schema.json sind implementiert. Weltpakete aus Ticket 09 sind implementiert; Darstellungserweiterungen 11–13 bleiben Planung.

## Dateien
Jeder Raum besteht aus einer YAML-Datei und referenzierten PNG-Dateien. Figuren benutzen transparente PNG-Sprite-Sheets mit beschreibenden Metadaten. Alle Ressourcenpfade sind relativ zum Inhaltsprojekt; IDs bleiben unabhängig vom Anzeigenamen stabil.

## Illustratives Beispiel
Maßgeblich für gültige Dateien sind das bestehende Schema und die fachliche Prüfung.

```yaml
# yaml-language-server: $schema=../schemas/room.schema.json
version: 1
id: lounge
name: Lounge
background: images/lounge.png
size:
  width: 640
  height: 360
spawns:
  - id: entry
    position: { x: 120, y: 300 }
walkable:
  - id: floor
    points:
      - { x: 40, y: 250 }
      - { x: 600, y: 250 }
      - { x: 600, y: 340 }
      - { x: 40, y: 340 }
exits:
  - id: to-garden
    position: { x: 580, y: 300 }
    radius: 16
    target:
      room: garden
      spawn: door
notes: Ein ruhiger Treffpunkt.
```

## Schema und Fachprüfung
Version, Pflichtfelder, Datentypen, Wertebereiche und unbekannte Felder über ein mitgeliefertes JSON Schema prüfen. Schema lokal zuordnen; keine Internetverbindung für normale Autorenarbeit voraussetzen.
Fachliche Prüfung ergänzt das Schema: eindeutige IDs, existierende Bilder und Zielräume, passende Bildabmessungen, gültige Polygone, Spawn und Ausgang auf begehbarer Fläche. Ein Schema allein liefert keine zuverlässige Completion vorhandener Raum-IDs; diese Auswahl bietet zunächst der grafische Editor.
Geometrische Semantik und Toleranzen werden durch die bestehende Prüfung und Tests abgesichert. Sichtbare Fehlermeldungen mit Raum-ID, Feld und möglichst Zeile.

## YAML-Regeln
Ein einfaches YAML-1.2-Teilprofil: Mappings, Listen und Skalare; keine eigenen Tags, Objektdeserialisierung oder Includes. Doppelte Schlüssel zurückweisen. Aliasse/Anker für das Autorenformat nicht erlauben; Parserressourcen begrenzen.
Der Editor schreibt kanonisch formatiertes YAML. Kommentare und freie Formatierung dürfen beim Speichern verloren gehen; vorher einmal deutlich darauf hinweisen. Fachliche Hinweise gehören ins Feld notes. Unbekannte Felder führen zu Fehlern statt stillschweigendem Verlust.

## Laden und Ressourcen
YAML nur auf .NET-Seite einlesen und prüfen; Browser erhält validierte Laufzeitdaten als JSON. Der JSON-Transport ändert nichts am YAML-Autorenformat.
PNG bleibt Quellformat. Automatische Atlantenoptimierung, proprietäre Bildcontainer und ein Bildeditor sind nicht erforderlich.
Nur selbst erstellte oder passend freigegebene Bilder verwenden; Quelle und Lizenz dokumentieren. Noch keine endgültige Projektlizenz festgelegt.

## Weltformatvertrag (09; Darstellung 11–13 geplant)
Ein externes Weltpaket enthält Manifest, Räume und Bilder; das Manifest nennt Welt-ID, Namen, Start-Raum und Start-Spawn sowie unabhängige Format- und Inhaltsversionen in SemVer. Die Formatversion bestimmt ein unveränderliches lokales Offline-Schema-Bündel und die dokumentierte Interpretation; die explizite Unterstützungsmatrix bestimmt, was ein Server akzeptiert. Leser prüfen Version, Schema und fachliche Beziehungen; Schreiber dürfen Versionen nicht still ändern oder unbekannte Daten verlustbehaftet überschreiben. Das bestehende ganzzahlige `version: 1` bleibt Legacy und wird nicht zu `1.0.0` umgedeutet; `roomFormat: legacy-1` ordnet diese Dateien ausdrücklich dem begrenzten Legacy-Lesepfad zu. Details und SemVer-Regeln: [Kompatibilitätsvertrag](docs/work-packages/world-format-compatibility.md).

Geplant sind optionale Raumangaben für Draufsicht und feste Figurengröße, horizontalen Ausschnitt und Tiefenfaktoren an zwei horizontalen Bezugslinien. Fehlende Angaben erhalten kompatible Defaults; Kamera und Skalierung ändern keine Raumkoordinaten. Konkrete Felder und Wertebereiche sind noch offen. Veröffentlichte Schemas und ältere Fixtures bleiben unverändert.

### Unterstützungsmatrix und Schreiben

| Weltformat | Raumformat | Unterstützung |
| --- | --- | --- |
| `1.0.0` | `legacy-1`, Raum-`version: 1` | Lesen und Schreiben |
| andere | beliebig | ausdrücklich abgelehnt |

Das Manifest verwendet `formatVersion`, `id`, `name`, `contentVersion`, `startRoom`, `startSpawn` und `roomFormat`. `rooms` und `assets/images` sind paketrelative Verzeichnisse. Dateiname und Raum-ID stimmen überein; Hintergründe sind vollständige PNG-Dateien mit passenden Abmessungen; PNG-Dateien sind auf 64 MiB, ihre dekodierten Bilddaten auf 128 MiB und einzelne YAML-Dateien auf 1 MiB begrenzt. Absolute Pfade, `..` und symbolische Links/Junctions werden abgewiesen. Der Editor schreibt ausschließlich `version: 1`, prüft vorhandene Dateien vor Überschreiben und ändert keine Manifestversion. Schema-Bündel und ihre lokalen Referenzen bleiben nach Veröffentlichung unverändert. Die ursprünglichen Legacy-Dateien und das ursprüngliche Schema bleiben als Referenz erhalten. `1.0.0` ist die erstmalige Veröffentlichung des Weltvertrags, keine Umdeutung des ganzzahligen Raumversionsfeldes; daher ist kein früheres Weltformat umzuversionieren. Weitere Formatänderungen folgen den Regeln im Kompatibilitätsvertrag.

## Horizontaler Ausschnitt seit Weltformat 1.2.0
Das optionale Raumobjekt `viewport` enthält ganzzahlige positive `width` und `height` sowie `scrollMode: horizontal`. `height` entspricht `size.height`. Ohne Feld bleibt die vollständige Darstellung erhalten; wirksame Breite ist min(Raumbreite, Ausschnittsbreite). Es verändert keine Geometrie, Laufgeschwindigkeit oder Figurengröße. Schema-Bündel: `schemas/world/1.2.0/`; Schemahinweis entsprechend auf `1.2.0/room.schema.json` richten. Ältere Bündel bleiben unverändert.

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
