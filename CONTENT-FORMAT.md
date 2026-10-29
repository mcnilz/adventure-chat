# Inhalte und Autorenformat
Status: Formatentwurf, noch kein implementiertes Schema.

## Dateien
Jeder Raum besteht aus einer YAML-Datei und referenzierten PNG-Dateien. Figuren benutzen transparente PNG-Sprite-Sheets mit beschreibenden Metadaten. Alle Ressourcenpfade sind relativ zum Inhaltsprojekt; IDs bleiben unabhängig vom Anzeigenamen stabil.

## Lesbarer Entwurf
Das Beispiel illustriert den Vorschlag und ist keine bereits von der Engine akzeptierte Datei. Das zugehörige ausführbare JSON Schema entsteht in Paket 06.

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
Geometrische Semantik und numerische Toleranzen vor Paket 02 festlegen. Sichtbare Fehlermeldungen mit Raum-ID, Feld und möglichst Zeile.

## YAML-Regeln
Ein einfaches YAML-1.2-Teilprofil: Mappings, Listen und Skalare; keine eigenen Tags, Objektdeserialisierung oder Includes. Doppelte Schlüssel zurückweisen. Aliasse/Anker für das Autorenformat nicht erlauben; Parserressourcen begrenzen.
Der Editor schreibt kanonisch formatiertes YAML. Kommentare und freie Formatierung dürfen beim Speichern verloren gehen; vorher einmal deutlich darauf hinweisen. Fachliche Hinweise gehören ins Feld notes. Unbekannte Felder führen zu Fehlern statt stillschweigendem Verlust.

## Laden und Ressourcen
YAML nur auf .NET-Seite einlesen und prüfen; Browser erhält validierte Laufzeitdaten als JSON. Der JSON-Transport ändert nichts am YAML-Autorenformat.
PNG bleibt Quellformat. Automatische Atlantenoptimierung, proprietäre Bildcontainer und ein Bildeditor sind nicht erforderlich.
Nur selbst erstellte oder passend freigegebene Bilder verwenden; Quelle und Lizenz dokumentieren. Noch keine endgültige Projektlizenz festgelegt.

