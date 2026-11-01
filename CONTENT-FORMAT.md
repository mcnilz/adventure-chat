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
