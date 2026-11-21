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
| `1.1.0`–`1.12.0` | `legacy-1`, Raum-`version: 1` | Lesen und Schreiben für jede ausdrücklich veröffentlichte Minor-Version |
| andere | beliebig | ausdrücklich abgelehnt |

Das Manifest verwendet `formatVersion`, `id`, `name`, `contentVersion`, `startRoom`, `startSpawn` und `roomFormat`. `rooms` und `assets/images` sind paketrelative Verzeichnisse. Dateiname und Raum-ID stimmen überein; Hintergründe sind vollständige PNG-Dateien mit passenden Abmessungen; PNG-Dateien sind auf 64 MiB, ihre dekodierten Bilddaten auf 128 MiB und einzelne YAML-Dateien auf 1 MiB begrenzt. Absolute Pfade, `..` und symbolische Links/Junctions werden abgewiesen. Der Editor schreibt ausschließlich `version: 1`, prüft vorhandene Dateien vor Überschreiben und behält beim normalen Speichern die Manifestversion bei. Schema-Bündel und ihre lokalen Referenzen bleiben nach Veröffentlichung unverändert. Die ursprünglichen Legacy-Dateien und das ursprüngliche Schema bleiben als Referenz erhalten. `1.0.0` ist die erstmalige Veröffentlichung des Weltvertrags, keine Umdeutung des ganzzahligen Raumversionsfeldes; daher ist kein früheres Weltformat umzuversionieren. Weitere Formatänderungen folgen den Regeln im Kompatibilitätsvertrag.

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

Die Verbenleiste löst beim Objektklick direkt eine Aktion aus. Sie erkennt das Verb über den Namen oder die ID der Aktion. Passen im aktuellen Zustand mehrere Aktionen zum selben Verb, gilt die erste in der gespeicherten Reihenfolge. Definiert ein Objekt in irgendeinem Zustand **Benutzen**, so ist dieses Verb nur in den ausdrücklich zugeordneten Zuständen verfügbar. Hat das Objekt keine solche Zuordnung, wählt **Benutzen** die erste verfügbare Aktion des aktuellen Zustands. Damit lassen sich auch vorhandene Aktionen wie „Einschalten“ über **Benutzen** auslösen. Diese Auswahlregel betrifft Spiel und Editorvorschau. Der gespeicherte Datenvertrag und die serverseitige Ausführung der Aktions-IDs behalten ihre bisherigen Regeln.

„Öffnen“ und „Schließen“ verwenden dieselbe Zuordnung über deutsche Namen und IDs oder `open` und `close`. Die Verbauswahl im Editor setzt einen passenden Aktionsnamen. Die für bestehende Inhalte ebenfalls ausgewertete Aktions-ID bleibt editierbar. Für einen eindeutigen Aktionssatz sollten Name und ID zu demselben Verb passen oder eine neutrale ID verwenden.

Ist die Figur für eine gewählte Aktion zu weit vom `usePoint` entfernt, fordert der Client zunächst einen serverbestätigten Weg dorthin an und sendet die Aktion nach dem Ankommen. Der Server prüft die Entfernung weiterhin selbst. Ändert sich der Zustand des gewählten Objekts unterwegs, wird die Aktion mit Rückmeldung verworfen; eine Änderung nur an einem anderen Objekt verhindert sie nicht. Eine neue Ziel- oder Verbwahl bricht den noch ausstehenden Auftrag ab. Nach Abschluss oder Ablehnung der Aktion wird wieder „Gehe zu“ gewählt. Ein unerreichbarer Bedienpunkt führt zu einer Fehlermeldung, nicht zu einer Zustandsänderung.

### Mehrere Bedienpunkte ab 1.7.0

Ein Raumobjekt darf `usePoints` mit einem bis acht unterschiedlichen begehbaren Punkten definieren, beispielsweise `usePoints: [{x: 120, y: 300}, {x: 220, y: 300}]`. Jeder Punkt muss von mindestens einem Raumspawn erreichbar sein. Die Liste gilt auch für rein grafische Objekte. Bei interaktiven Objekten ersetzt sie den einzelnen `interaction.usePoint`; ist sie nicht vorhanden, bleibt dessen bisherige Bedeutung unverändert. Bei rein grafischen alten Objekten ohne Angaben versucht der Client lokale Punkte um das Bild herum; ist keiner erreichbar, erscheint eine lokale Fehlermeldung. Für interaktive Objekte gilt weiter `useRadius`, für lokale Rückmeldungen rein grafischer Objekte 40 Raumpixel. Der Client versucht die Punkte nach Abstand, bis der Server einen Weg bestätigt. Der Server akzeptiert Objektaktionen nur innerhalb des bisherigen Radius um einen der erlaubten Punkte. Ungültige oder leere Listen werden abgewiesen.

1.7.0 ist eine additive Minor-Erweiterung in einem eigenen Schema-Bündel. Alle veröffentlichten 1.0.0–1.6.0-Schemas und ihre Interpretation bleiben erhalten; unter alten Versionen ist `usePoints` unzulässig. Auch bei einer unmöglichen Handlung geht die Figur zunächst zum Objekt. Die Fehlermeldung erscheint ausschließlich als lokale Sprechblase und ist kein Raumchat; danach ist „Gehe zu“ aktiv. Objektzustände in Formaten bis einschließlich 1.7.0 sperren keine Wege.

### Zustandsabhängige Navigation ab 1.8.0

Eigenes Offline-Bündel `schemas/world/1.8.0`, ausdrücklich zusätzlich unterstützt. Minor: ausschließlich optionale Zustandsfelder; ohne sie bleiben Boden, Fußpunkt, Ausgänge und Altwelten unverändert. Frühere Schemas einschließlich Teilschemas bleiben unverändert.

Ein Zustand darf `obstacles` als Liste von Polygonen `{id, points: [{x, y}, ...]}` und `blockedExits` als Liste eigener Ausgangs-IDs angeben. Hindernispunkte sind **absolute Raumkoordinaten**, unabhängig von der Bildposition; beim Stuhl werden Bild und Polygon für beide Endpositionen ausdrücklich definiert. Keine automatisch aus Bild/Alpha gewonnene Kollision und kein kontinuierliches Verschieben. Grundfläche und statische Hindernisse bleiben erhalten; eine offene Tür kann eine feste Boden-Aussparung nicht aufheben. Eine wirksame Ausgangssperre eines beliebigen Objekts genügt zum Sperren.

Pro Zustand höchstens vier einfache, nicht selbstberührende Hindernispolygone mit je 3–16 endlichen Punkten innerhalb des Raumrechtecks und eindeutigen IDs; insgesamt höchstens 256 deklarierte Hindernisvertices pro Raum über **alle** Zustände. Höchstens 32 unterschiedliche eigene Ausgangs-IDs je Zustand. Die bestehenden Limits gelten weiter: 128 Objekte, 32 Zustände und 64 Aktionen je Objekt, 32 direkte Wirkungen je Aktion. Anfangsgeometrie muss alle benannten Spawns zulassen; anfangs freigegebene Ausgänge müssen von einem Spawn erreichbar sein. Gesperrte Ausgänge dürfen zunächst unerreichbar sein. Referenzen werden unabhängig von der aktuellen Freigabe geprüft.

Neue Hindernisse schließen ihren Rand ein: Punkt-/Segmentabstand bis einschließlich `1e-6` Raumpixel gilt als Randkontakt und ist gesperrt, auch bei tangentialen oder kollinearen Wegen. Diese numerische Toleranz erzeugt keinen Figurenradius. Alte Polygon-/Randregeln bleiben unverändert. Bestehende Wegfindung behält 4 Pixel Ecksichtabstand und 14 Pixel Navigationsknotenversatz bei; das ist kein zugesicherter umlaufender Radius.

Vor einer Aktion werden alle direkten Zustandswirkungen gemeinsam geprüft. Figurenpositionen werden zum selben Serverzeitpunkt aus den bestätigten Wegen berechnet. Keine Figur darf danach einen ungültigen Fußpunkt haben. Außerdem muss jede betroffene Figur mindestens einen **vorher erreichbaren** Sicherheitsanker behalten: gültiger benannter Spawn oder freigegebener, erreichbarer Ausgang mit gültigem Zielspawn. Bereits ankerloser Zustand führt zur Ablehnung. Geprüft werden Räume mit geänderter Navigation und Räume, deren direkte Übergangsziele dadurch ungültig werden. Kein Nachweis globaler Welt-/Rätsellösbarkeit. Bei Ablehnung bleiben Objekt- und Bewegungsrevisionen sowie bestehende Laufaufträge unverändert.

Nach Erfolg bleiben weiterhin gültige Wege bestehen. Ungültige Wege werden ab dem aktuellen Fußpunkt zum bisherigen Ziel neu berechnet oder dort sicher gestoppt. Zustand, Geometrie, freigegebene Ausgänge und nötige Wegkorrekturen kommen in derselben revidierten Raumnachricht an. Stoppen meldet die betroffene Figur ausschließlich lokal als Sprechblase; kein Raumchat, keine automatische Wiederaufnahme. Eintritt/Reconnect und Übergänge auf einen aktuell gesperrten vorgesehenen Spawn werden abgelehnt, ohne Ersatzspawn oder Teleport.

Die Editorvorschau prüft Wege, Objektaktionen und Ausgänge mit denselben Serverregeln wie das Spiel. Bei einem interaktiven Objekt verwendet „Automatisch“ die Auswahlregel für „Benutzen“. Über die Verbauswahl lässt sich eine konkrete Aktion testen. Die Vorschau verwendet den Raumdraft beim Start und isolierte Objektzustände, die nach einem Raumwechsel erhalten bleiben. Ein Neustart der Vorschau setzt sie auf die Anfangszustände zurück. Synthetisches Beispiel: `tests/Fixtures/navigation`.

Ältere Weltpakete behalten beim Speichern ihr Format. Für eine Aktualisierung auf 1.8.0, 1.9.0, 1.10.0, 1.11.0 oder 1.12.0 wird das höhere Zielformat ausdrücklich im Editor gewählt. Vor dem Schreiben prüft der Server Manifest, vorhandene Räume und den Entwurf mit dem Zielvertrag. Speichern aktualisiert das Manifest und den bearbeiteten Raum. Übrige Raumdateien bleiben unverändert.

### Sichtbare Objektübergänge ab 1.9.0

Ein Objekt kann unter `interaction.transitions` 1–64 gerichtete Übergänge angeben. `from` und `to` benennen zwei verschiedene Zustände desselben Objekts. Pro Richtung ist ein Eintrag erlaubt. Das PNG und die Zielgröße sind für beide Zustände gleich, die Positionen verschieden. Die Darstellung interpoliert Position und Tiefenlinie linear in der angegebenen Zeit von 100 bis 5000 ganzen Millisekunden.

```yaml
transitions:
  - { from: lowered, to: raised, durationMs: 1800 }
  - { from: raised, to: lowered, durationMs: 1400 }
```

Eine bestätigte eigene Aktion oder direkte Verknüpfung startet den zum bisherigen und neuen Zustand passenden Übergang. Das Zielobjekt kann bei vorhandenen Übergängen mit `actions: []` ausschließlich über Verknüpfungen bedient werden. Der Server übernimmt den Zielzustand samt Geometrie sofort nach der Sicherheitsprüfung. Solange die Darstellung läuft, lehnt er Aktionen mit einer erneuten Zustandsänderung desselben Objekts vollständig ab. Erneutes Setzen desselben Zustands startet keine weitere Bewegung. Beim Eintritt liefert der Snapshot den bereits verstrichenen Anteil einer laufenden Bewegung. Eine Richtung ohne Eintrag springt wie bisher direkt zum neuen Erscheinungsbild.

Format 1.9.0 ist eine additive Minor-Version. Bisher gültige Dateien behalten ohne `transitions` ihre Semantik. Das neue Offline-Bündel liegt unter `schemas/world/1.9.0/`.

### Figurenposen und Gesten ab 1.10.0

Eine Objektaktion kann ein optionales `actorAnimation`-Feld enthalten.

```yaml
actorAnimation:
  point: 0
  direction: up
  durationMs: 1500
  sprites:
    mara: assets/images/workshop-mara-gesture.png
```

`point` ist der nullbasierte Index in `usePoints`. Bei einem einzelnen `interaction.usePoint` ist er 0. Die Figur muss den ausgewählten Punkt bis auf einen Raumpixel erreichen. Der Server prüft seine aktuelle Begehbarkeit und richtet die Figur dort nach `up`, `down`, `left` oder `right` aus. Aktionen ohne `actorAnimation` verwenden weiterhin den gewöhnlichen Bedienradius.

Ohne `durationMs` hält die Figur die Pose ab der erfolgreichen Objektaktion. Der erreichte Zustand `to` muss für die Dauer der Pose bestehen bleiben. Einen Bedienpunkt mit gehaltener Pose vergibt der Server je Raum an die erste berechtigte Figur, die dort ankommt. Ein neuer Gehauftrag, Raumwechsel, Verbindungsende oder Wechsel des vorausgesetzten Zustands beendet die Pose. Dieselbe Eingabe startet eine laufende Benutzung kein zweites Mal.

Mit einer Dauer von 200 bis 5000 ganzen Millisekunden zeigt die Figur eine kurze Geste. Ihre Aktion wirkt erst am Ende nach einer erneuten Prüfung von Zustand, Bedienpunkt und Navigationssicherheit. Bewegung, Raumwechsel, Verbindungsende oder ein Wechsel des vorausgesetzten Ausgangszustands brechen die ausstehende Wirkung ab. Figuren in einem laufenden Raum und beim späteren Eintritt erhalten die bestätigte Pose beziehungsweise die bereits verstrichene Zeit der Geste.

`sprites` ist optional und ordnet bis zu 32 Figuren-IDs aus dem Katalog jeweils ein PNG direkt unter `assets/images/` zu. Eine Pose verwendet ein quadratisches Bild. Eine Geste verwendet drei gleich große quadratische Bilder in einer horizontalen Reihe. Der Renderer zeigt sie in gleichen Zeitabschnitten von links nach rechts und verankert jedes Bild unten mittig am Fußpunkt. Ohne passende Grafik bleibt die gewählte Figur in der konfigurierten Blickrichtung stehen.

Format 1.10.0 ergänzt das optionale Feld in einem eigenen Schema-Bündel unter `schemas/world/1.10.0/`. Bestehende gültige Aktionen behalten ohne das Feld ihre bisherigen Regeln. Der Raumeditor kann die Pose, Geste und Grafiken je Aktion setzen und in der isolierten Vorschau ausprobieren.

### Stationäre NPCs und Gespräche ab 1.11.0

Ein Raum kann unter `npcs` stationäre Figuren mit einer Grafik aus dem Figurenkatalog der Welt definieren. Die Referenzwelt nutzt diese Definition für Nela.

```yaml
npcs:
  - id: nela
    name: Nela
    characterId: bruno
    position: { x: 112, y: 286 }
    talkPoint: { x: 146, y: 294 }
    options:
      - { id: introduction, text: Wer bist du?, reply: Ich bin Nela. Ich passe auf die Lounge auf. }
      - { id: garden, text: Wo ist der Garten?, reply: Durch die Tür rechts erreichst du den Tavernen-Garten. }
```

- `id` ist im Raum eindeutig, `characterId` verweist auf einen exakten Katalogeintrag. `position` setzt den Fußpunkt der Figur. `talkPoint` ist das Laufziel des Besuchers und liegt höchstens 48 Raumpixel entfernt. Beide Punkte müssen auf begehbarem Boden liegen und direkt verbunden sein.
- Jede Figur hat zwei bis acht `options` mit eigener ID, Aussage (`text`) und zugehöriger Antwort (`reply`). IDs werden auf Duplikate unabhängig von der Groß-/Kleinschreibung geprüft. Aussagen müssen sich unterscheiden. Textgrenzen sind 100 Zeichen für den Namen, 120 für eine Aussage und 400 für eine Antwort.
- Der Server prüft den Raum, die Ankunft bis auf zwei Raumpixel am Gesprächspunkt und die freie Verbindung in der aktuellen Raumgeometrie. Besucher können gleichzeitig mit derselben Figur sprechen. Im eigenen Browser ersetzen die Gesprächsoptionen die Verben. Die gewählte Aussage und die Antwort erscheinen nacheinander als Sprechblasen.
- NPCs werden in der Raum-YAML bearbeitet. Der Browsereditor erhält die Einträge beim Speichern und zeigt die Figur in der Raumvorschau.

`npcs` ist optional. Die additive Erweiterung erscheint als Minor-Version mit eigenem Schema-Bündel unter `schemas/world/1.11.0/`.

### Zustandsabhängige Gespräche ab 1.12.0

Ein optionales `when` schaltet eine Frage frei, wenn ein interaktives Objekt im selben Raum den angegebenen Zustand hat. `object` und `state` verwenden die exakten IDs aus der Raumdefinition. Der Server prüft beide Verweise beim Laden und Speichern.

```yaml
options:
  - id: introduction
    text: Wer bist du?
    reply: Ich bin Nela.
    replies:
      - when: { object: bell, state: rung }
        reply: Ich bin Nela und helfe dir gern.
  - id: bell
    text: Hast du die Klingel gehört?
    when: { object: bell, state: rung }
    reply: Ja, ich habe deinen Ruf gehört.
```

`replies` enthält bei Bedarf eine bis acht Antwortvarianten. Die erste passende Variante in der angegebenen Reihenfolge ersetzt `reply`. Wenn keine Variante passt, gilt `reply`. Eine NPC-Definition enthält weiterhin zwei bis acht Fragen. Je nach Zustand können davon auch null sichtbar sein. Die Regeln werden in YAML bearbeitet. Der Raumeditor bewahrt sie beim Speichern und wertet sie in der getrennten Vorschau mit deren Objektzuständen aus.

Das neue Schema-Bündel unter `schemas/world/1.12.0/` ergänzt optionale Felder. Vorhandene Fragen bleiben ohne diese Felder jederzeit verfügbar und behalten ihre Antwort.
