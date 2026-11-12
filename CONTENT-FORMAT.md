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
| `1.1.0`–`1.7.0` | `legacy-1`, Raum-`version: 1` | Lesen und Schreiben für jede ausdrücklich veröffentlichte Minor-Version |
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

## Tiefenskalierung seit Weltformat 1.5.0

Explizit unterstützt: 1.0.0 bis 1.5.0 in den veröffentlichten Minor-Versionen; alle anderen Versionen werden abgewiesen. Neues Offline-Bündel: schemas/world/1.5.0. Optionale Erweiterung mit Faktor 1 als Default; ältere Schemas und Fixtures bleiben unverändert.

```yaml
depthScale:
  back: { y: 140, factor: 0.5 }
  front: { y: 320, factor: 1 }
```

Beide Y-Werte sind endliche Raumkoordinaten zwischen 0 und Raumhöhe, back.y < front.y. Beide Faktoren sind endlich und liegen einschließlich zwischen 0.25 und 2. Zwischen den Linien wird linear nach dem ungerundeten Y-Fußpunkt interpoliert; außerhalb gilt der jeweilige Randfaktor. Endhöhe = actorHeight × Faktor, ohne erneute Begrenzung auf 16–128. Der Fußpunkt bleibt unten mittig. Namen und Sprechblasen folgen der skalierten Höhe, ihre Schrift bleibt unverändert. Kameraoffset, CSS-Größe und X-Position beeinflussen den Verlauf nicht.

Der Editor bietet die vier Zahlen und einen Aktivschalter; die gemeinsame Vorschau zeigt den Verlauf beim Laufen. Ungültige Daten werden beim Validieren/Speichern zurückgewiesen. Ohne Feld bleibt die bisherige feste Größe erhalten. Geschwindigkeit, Navigation, Ausgänge, Objekt-Tiefenlinien und Kollision bleiben unverändert. Ein vorhandenes Manifest wird beim Speichern nicht automatisch hochgestuft.


### Optionale Draufsicht-Sheets (1.5.0)
Am 09.11.2026 ausdrücklich bestätigt: Ein Figurenkatalogeintrag darf zusätzlich topDownImage nennen. Es gelten dieselben PNG-/Pfadgrenzen und dasselbe Quellraster wie image (vier Spalten, Reihen rechts/hinten/vorne, links gespiegelt). In top-down-Räumen wird dieses Sheet verwendet; fehlt es, bleibt die bisherige Katalogfigur erhalten. In Adventure-Räumen gilt immer image. Auswahl und Figuren-ID bleiben über Übergänge erhalten. Dies ist ebenfalls eine optionale Minor-Erweiterung; frühere Formate erlauben das Feld nicht.

## Interaktionen ab Weltformat 1.6.0 (Ticket 20)

Der Server akzeptiert ausdrücklich 1.0.0, 1.1.0, 1.2.0, 1.3.0, 1.4.0, 1.5.0, 1.6.0 und 1.7.0; andere Versionen werden abgewiesen. Das Offline-Bündel für Interaktionen liegt unter `schemas/world/1.6.0`. Die älteren Bündel bleiben unverändert. Die optionale Erweiterung erhält die Bedeutung und Defaults alter Welten; deshalb ist sie eine Minor-Version. Ein grafisches Objekt ohne `interaction` hat keine serverseitigen Zustandsaktionen.

Ein `objects`-Eintrag kann `interaction` mit `name`, `initialState`, `usePoint`, `useRadius`, `states` und `actions` enthalten. Zustände haben `id`, `name`, `image`, `position`, `size` und `depthY`. Aktionen haben `id`, `name`, `from` und `to`; optionale `effects` nennen explizit `room`, `object` und `state` innerhalb derselben Welt. Ein vollständiges Beispiel mit Stuhl und Schalter/Tor steht in [der Testwelt](tests/Fixtures/interactions/rooms/hall.yaml). Objektzustände und -aktionen sind auf 32 beziehungsweise 64 pro Objekt begrenzt, direkte Wirkungen auf 32 pro Aktion. IDs bestehen aus 1–64 ASCII-Buchstaben, Ziffern, Unterstrichen oder Bindestrichen; Namen haben höchstens 100 Zeichen. Der Bedienpunkt muss begehbar und von mindestens einem Spawn erreichbar sein; der Radius liegt über 0 und höchstens bei 200 Raumpixeln. Zustandsbilder und Darstellungen unterliegen denselben Pfad-, PNG- und Raumgrenzen wie grafische Objekte.

Der Server prüft Anfangs- und Zielzustände, eindeutige Zustands-IDs und eindeutige Kombinationen aus Aktions-ID und Ausgangszustand sowie alle direkten Zielreferenzen. Mehrfache Zuweisung desselben Zielobjekts, auch des Ausgangsobjekts, ist ungültig. Aktionen lösen keine Folgeaktionen aus. Die Ausführung ist servergeordnet und atomar; die Revision des aktuellen Raums schützt vor veralteten oder wiederholten Befehlen. Die tatsächliche Nähe zum Bedienpunkt wird am Server geprüft. Zustände bleiben beim Leeren eines Raums, Raumwechsel und Reconnect erhalten; Weltneustart und erfolgreiches Speichern von Autorendaten setzen sie auf Anfangszustände zurück. Bei einem Editor-Speichern werden Teilnehmer über den Reset informiert und müssen erneut beitreten.

Die vorgezogene Verbenbedienung nutzt die bestehenden 1.6.0-Felder ohne Formatänderung: Für „Ziehen“ und „Drücken“ erkennt sie die entsprechenden deutschen Namen oder IDs beziehungsweise `pull` und `push`. „Benutzen“ bietet alle im aktuellen Zustand erlaubten Aktionen; eine einzelne wird direkt ausgeführt, bei mehreren erscheinen nach dem Objektklick beschriftete Aktionsknöpfe. So bleiben auch bestehende Welten mit frei gewählten Aktionsnamen bedienbar. Autoren können die direkte Zuordnung zu Ziehen und Drücken über die vorhandenen Aktions-IDs oder Namen herstellen; eine explizite Verbzuordnung im Autorenwerkzeug gehört zur späteren Planung. Die Zuordnung ändert weder gespeicherte Daten noch die serverseitige Bedeutung älterer Formate.

Ist die Figur für eine gewählte Aktion zu weit vom `usePoint` entfernt, fordert der Client zunächst einen serverbestätigten Weg dorthin an und sendet die Aktion nach dem Ankommen. Der Server prüft die Entfernung weiterhin selbst. Ändert sich der Zustand des gewählten Objekts unterwegs, wird die Aktion mit Rückmeldung verworfen; eine Änderung nur an einem anderen Objekt verhindert sie nicht. Eine neue Ziel- oder Verbwahl bricht den noch ausstehenden Auftrag ab. Nach Abschluss oder Ablehnung der Aktion wird wieder „Gehe zu“ gewählt. Ein unerreichbarer Bedienpunkt führt zu einer Fehlermeldung, nicht zu einer Zustandsänderung.

### Mehrere Bedienpunkte ab 1.7.0

Ein Raumobjekt darf `usePoints` mit einem bis acht unterschiedlichen begehbaren Punkten definieren, beispielsweise `usePoints: [{x: 120, y: 300}, {x: 220, y: 300}]`. Jeder Punkt muss von mindestens einem Raumspawn erreichbar sein. Die Liste gilt auch für rein grafische Objekte. Bei interaktiven Objekten ersetzt sie den einzelnen `interaction.usePoint`; ist sie nicht vorhanden, bleibt dessen bisherige Bedeutung unverändert. Bei rein grafischen alten Objekten ohne Angaben versucht der Client lokale Punkte um das Bild herum; ist keiner erreichbar, erscheint eine lokale Fehlermeldung. Für interaktive Objekte gilt weiter `useRadius`, für lokale Rückmeldungen rein grafischer Objekte 40 Raumpixel. Der Client versucht die Punkte nach Abstand, bis der Server einen Weg bestätigt. Der Server akzeptiert Objektaktionen nur innerhalb des bisherigen Radius um einen der erlaubten Punkte. Ungültige oder leere Listen werden abgewiesen.

1.7.0 ist eine additive Minor-Erweiterung in einem eigenen Schema-Bündel. Alle veröffentlichten 1.0.0–1.6.0-Schemas und ihre Interpretation bleiben erhalten; unter alten Versionen ist `usePoints` unzulässig. Auch bei einer unmöglichen Handlung geht die Figur zunächst zum Objekt. Die Fehlermeldung erscheint ausschließlich als lokale Sprechblase und ist kein Raumchat; danach ist „Gehe zu“ aktiv. Ticket 21 plant seine optionalen zustandsabhängigen Hindernisse und Ausgangssperren nun separat für 1.8.0. Objektzustände sperren bis dahin keine Wege.
