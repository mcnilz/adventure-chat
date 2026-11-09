# Adventure Chat
Arbeitsname, endgültiger Name offen.

## Zweck
Ein lokaler Multiplayer-Chat mit begehbaren Räumen und einem Browser-Raumeditor. Die aktuelle Projektübergabe steht in [active-state.md](active-state.md).

## Einstieg
1. [AGENTS.md](AGENTS.md): Arbeitsregeln und Lesereihenfolge.
2. [SPEC.md](SPEC.md): Umfang, User Stories und MVP-Abnahme.
3. [ARCHITECTURE.md](ARCHITECTURE.md): Technik und Zuständigkeiten.
4. [CONTENT-FORMAT.md](CONTENT-FORMAT.md): YAML und Bildressourcen.
5. [TESTING.md](TESTING.md): vorgeschlagene Testgrenzen.
6. [PLAN.md](PLAN.md): Abhängigkeiten und prüfbare Ergebnisse.
7. [SKILLS.md](SKILLS.md): Matt-Pocock-Workflow und Startprompts.
8. [active-state.md](active-state.md): aktueller Stand.
9. [GLOSSARY.md](GLOSSARY.md): gemeinsame Begriffe.

## Verwendung
Die lokalen Tickets unter [.scratch/adventure-chat/issues](.scratch/adventure-chat/issues/) dokumentieren Pakete und Abnahmen.

## Lokaler Start und Entwicklung (PowerShell 7)

### Voraussetzungen
- .NET 10 SDK
- Node.js (>= 20) und npm

### 1. Client kompilieren
```powershell
cd src\AdventureChat.Client
npm ci --ignore-scripts
npm run build
npm run typecheck
cd ..\..
```

### 2. Projekt bauen und automatisierte Tests ausführen
```powershell
# Abhängigkeiten anhand der Lockfiles wiederherstellen und bauen
dotnet restore --locked-mode
dotnet build --no-restore

# Einmalig den zur Playwright-Version passenden Chromium installieren
pwsh tests\AdventureChat.E2ETests\bin\Debug\net10.0\playwright.ps1 install chromium

# Browser-Verhaltenstests (Playwright / NUnit) ausführen
dotnet test --no-build --no-restore
```

### 3. Server starten

#### A. Regulärer Spielmodus (Multiplayer-Chat)
Im regulären Spielmodus sind die Editor- und Autoren-APIs deaktiviert. Spieler können sich mit eigenem Namen in beliebige erstellte Räume einwählen, herumlaufen, chatten und Ausgänge benutzen.
```powershell
dotnet run --project src\AdventureChat.Server\AdventureChat.Server.csproj
```
Browser öffnen: `http://localhost:5000` (oder angezeigte Portadresse).
Die gesamte Spieloberfläche einschließlich Chat passt sich bei kleinen Fenstern proportional an Breite und Höhe an. „Auf Fenstergröße“ oben rechts erlaubt zusätzlich das Vergrößern auf die verfügbare Browserfläche; „Normalgröße“ begrenzt wieder auf die bisherige Breite. Beide Modi passen sich bei Fenster- und Raumwechseln automatisch an; die Auswahl gilt bis zum Neuladen der Seite.
Ohne Weltparameter startet automatisch die aktuelle Referenzwelt mit Lounge, Garten, Mara/Bruno und Demo-Runde. Das gilt auch beim Start der gebauten Anwendung. Im Projekt verwendet der Autorenmodus `worlds/reference`; außerhalb des Projekts die mitgelieferte Weltkopie. Explizite `World:Root`-/`World:Roots`-Angaben haben Vorrang. Ein ausdrücklich gesetztes `Content:Root` verwendet weiterhin den bisherigen Legacy-Inhaltsmodus.
Mit URL-Parametern direkt einsteigen: `http://localhost:5000?name=Guybrush&room=lounge`

#### B. Autorenmodus (Integrierter Raum- & Welteditor)
Im Autorenmodus ist der Editor unter `/editor.html` aktiv. Räume können visuell modelliert, begehbare Flächen und Hindernisse gezeichnet, Spawns und Ausgänge platziert sowie Hintergrundbilder verwaltet und hochgeladen werden.
```powershell
dotnet run --project src\AdventureChat.Server\AdventureChat.Server.csproj -- --authoring
```
Editor im Browser aufrufen: `http://localhost:5000/editor.html`

### 4. Kurzanleitung: Räume verbinden und Welt nutzen
1. **Ersten Raum erstellen:** Im Editor `➕ Neu` anklicken, ID vergeben (z. B. `hub`), Bodenfläche aufziehen, Startpunkt (`📍 Spawn setzen`) mit ID `entry` platzieren.
2. **Zweiten Raum erstellen:** `➕ Neu` anklicken, ID vergeben (z. B. `dungeon`), Bodenfläche aufziehen, Startpunkt mit ID `entry` und einen Spawn mit ID `from-hub` platzieren.
3. **Ausgang verknüpfen:** Im ersten Raum `hub` das Werkzeug `🚪 Ausgang setzen` wählen, Zielraum `dungeon` und Ziel-Spawn `from-hub` im Dialog auswählen und auf der begehbaren Fläche platzieren.
4. **Welt validieren:** Button `🌐 Welt prüfen` anklicken, um alle raumübergreifenden Referenzen und Erreichbarkeiten automatisch zu verifizieren.
5. **Gemeinsam betreten:** Server im Spielmodus starten und mit mehreren Browserfenstern unter `http://localhost:5000?room=hub` gemeinsam den Chat-Raum betreten und durch den Ausgang wechseln.

## Festgelegt und noch offen

Planungsannahmen: private Tests statt öffentlichem Dienst, ungefähr zehn gleichzeitige Besucher, Desktopbrowser mit Maus, Punktfigur für Wegfindung ohne Spielerkollision, flüchtige Sitzungen und Nachrichten. Diese Annahmen werden vor dem jeweiligen betroffenen Paket bestätigt. GitHub-Veröffentlichung ist eine spätere Option.


## Externes Weltpaket (Ticket 09)

Die Referenzwelt kann unabhängig vom Anwendungsverzeichnis kopiert werden:

```powershell
Copy-Item -Recurse worlds\reference C:\AdventureWorld
# Als Spielserver; für den lokalen Editor zusätzlich --authoring angeben
dotnet run --project src\AdventureChat.Server -- --World:Root C:\AdventureWorld --authoring
```

`world.yaml` bestimmt Welt-ID, Namen, Format- und Inhaltsversion sowie Start-Raum und Start-Spawn. `rooms/*.yaml` enthält das ausdrücklich zugeordnete Legacy-Raumformat `version: 1`; Bilder liegen unter `assets/images`. Der Server prüft alle Dateien beim Start und bricht bei Fehlern ab. Im Spiel erfolgt der Eintritt stets am festgelegten Startpunkt, auch nach Neustart oder Reconnect. Der Editor ist nur im lokalen Autorenmodus verfügbar und speichert im Weltordner. Laufender Spielzustand bleibt flüchtig.

Alternativ lassen sich mehrere Pfade als `--World:Roots:0 <Pfad> --World:Roots:1 <Pfad>` konfigurieren. Alle werden geprüft, doppelte Welt-IDs abgewiesen. `World:Root` steht, falls zusätzlich angegeben, an erster Stelle. Ohne Weltkonfiguration startet die Referenzwelt; mit ausdrücklich gesetztem `Content:Root` bleibt der bisherige lokale Betrieb mit URL-Raumwahl erhalten.

### Neue Taverne: Mara, Bruno und Tischverdeckung

Lokal aus dem Projekt starten:

```powershell
dotnet run --project src/AdventureChat.Server -- --World:Root D:\adventure-chat\worlds\reference --authoring
```

In der Weltwahl „Referenzwelt“ wählen, Namen eingeben und Mara oder Bruno auswählen. Die rechte Tür der Lounge führt in den Garten, dessen linke Tür zurückführt. Mit zwei Browserfenstern unterschiedliche Figuren auswählen, laufen und eine Chatnachricht senden; Namen und Sprechblasen vergleichen.

Zum Prüfen der Verdeckung hinter den großen Tisch laufen: Lounge ungefähr (330,258), Garten (330,278). Die Tischplatte verdeckt die unteren Körperteile; bei (330,330) steht die Figur davor. Beide Seiten erreicht man über die freien Wege links und rechts. Mit D wird die Geometrie sichtbar. Im Editor dieselbe Welt wählen, beide Räume speichern/neuladen und dieselben Ziele im Vorschaumodus anklicken. Die Raumobjekte lassen sich über ihre Auswahlliste bearbeiten.

Die Räume bleiben 640 × 360 mit `actorHeight: 128`. Die neuen Figuren verwenden native 128 × 128-Frames mit 125 sichtbaren Körperpixeln und drei Pixeln oberem Rand; der Spielrenderer zeichnet sie ohne Größenänderung. Die gelieferten Sheets unter `design-guide/assets/characters/native128` werden bytegleich in die Spielgrafiken kopiert. Ältere Figurenoriginale und 1280 × 720-Raumoriginale bleiben erhalten. Herkunft, Bearbeitungen und Inhaltsversion 2.0.2 stehen in [ART-PROVENANCE.md](worlds/reference/ART-PROVENANCE.md), die aktualisierte Gestaltung in [Design Guide](worlds/reference/design-guide/index.html). Profil-Gegenphasen bleiben subtil; Bewegungsgefühl, anatomische Feinheiten und Maskenkanten bleiben Teil der menschlichen Sichtabnahme.

Bei konfigurierten Weltpaketen erscheint im Spiel und im lokalen Editor zuerst die Weltwahl, auch bei nur einer Welt. Im Spiel führt „Zur Weltwahl“ aus der Sitzung zurück; anschließend lässt sich eine andere Welt betreten. Nach einem Verbindungsabbruch erfolgt der frische Eintritt in die zuletzt gewählte Welt am Start-Spawn. Ist diese Welt nicht mehr verfügbar, erscheint die Auswahl erneut. Bei Verbindungsverlust werden in diesem Modus keine lokalen Ersatzbewegungen berechnet.

Im Editor merkt die Seitenadresse die gewählte Welt als `?worldId=...`. Laden, Speichern, Validierung, Bilder und Vorschau beziehen sich auf diese Welt. „Zur Weltwahl“ wechselt die Welt über einen Seitenwechsel; bei ungespeicherten Änderungen warnt der Browser. Weltpakete werden weiterhin ausschließlich über Serverkonfiguration eingebunden; die Browseradresse enthält keine Verzeichnispfade.

Die öffentlichen Raumdaten liegen unter `/api/worlds/{worldId}/rooms/{roomId}`, Bilder unter `/world-assets/{worldId}/images/...`. Die bisherigen `/api/rooms/...`-Routen bleiben Zugänge zur ersten Welt beziehungsweise zum Betrieb ohne Weltpaket; der Mehrweltclient verwendet für Bewegung ausschließlich die an seine Sitzung gebundenen Hubbefehle. Autorenabfragen über `/api/editor/...` tragen `worldId` als Abfrageparameter; unbekannte Welt-IDs liefern 404. Die Autoren-Zugriffsbeschränkungen gelten unverändert.

Für YAML-Editoren das mit der Anwendung ausgelieferte `schemas/world/1.0.0/room.schema.json` lokal den Dateien `rooms/*.yaml` und `world.schema.json` dem Manifest zuordnen. Der Browsereditor schreibt beim Speichern eine lokale relative Schema-Zuordnung; Schema-Kommentare sind kein Laufzeitvertrauen. Bei übernommenen Dateien mit älteren Schema-Kommentaren kann die Zuordnung im Texteditor angepasst werden, ohne Raumdaten zu ändern. Die unveränderten Dateien aus `content/rooms` lassen sich durch Kopieren nach `rooms/` im Paket mit `roomFormat: legacy-1` verwenden, sofern die zugehörigen Bilder ebenfalls im Paket liegen.

Beispiel für VS Code mit installierter YAML-Erweiterung: Den externen Weltordner als Arbeitsordner öffnen und in dessen lokalen Einstellungen die beiden absoluten Schema-Dateipfade auf die eigene Installation anpassen:

```json
{
  "yaml.schemas": {
    "file:///D:/adventure-chat/src/AdventureChat.Server/bin/Debug/net10.0/schemas/world/1.0.0/world.schema.json": "world.yaml",
    "file:///D:/adventure-chat/src/AdventureChat.Server/bin/Debug/net10.0/schemas/world/1.0.0/room.schema.json": "rooms/*.yaml"
  }
}
```

Die Pfade sind Beispiele und müssen auf das lokal vorhandene Schema-Bündel zeigen. Die Referenzdateien enthalten bewusst keine vorrangigen Schema-Kommentare. Unveränderte alte Raumdateien können einen früheren Kommentar enthalten; für eine eindeutige Zuordnung in einem Texteditor gegebenenfalls dessen Schema-Pfad im Kommentar korrigieren. Das ändert weder Raumdaten noch die beim Server maßgebliche Legacy-Zuordnung.


## Kartenwelt (Format 1.1.0)

```powershell
dotnet run --project src/AdventureChat.Server -- --World:Root D:\adventure-chat\worlds\map --authoring
```

In der Weltwahl „Kartenwelt“ betreten. Die beiden braunen Kartenorte führen links zur Taverne und rechts zum Garten; dort führt „Karte“ zurück. Die U-Hecke links der Mitte bildet eine begehbare Sackgasse. Die Hecken sind Hindernisse, die geschlossene Hecke rechts oben enthält ein unerreichbares Ziel. Der untere Durchgang an den langen Hecken ist nur 15 Raumpixel breit und bleibt für den Fußpunktvertrag zulässig.

Format 1.1.0 ergänzt optionale Raumfelder `perspective: adventure | top-down`, `actorHeight: 16..128` (logische Raumpixel, Standard 64) und `walkSpeed: 1..1000` (logische Raumpixel/s, Standard 130). Ohne Angaben bleibt bisheriges Verhalten erhalten. Der Fußpunkt ist der untere mittlere Anker des Figurenrahmens. Bildschirmskalierung passt nur die Anzeige an; Autorenmaßstab ändert weder Koordinaten noch Kollisionen. Kein Teilnehmerzoom und keine Kameraänderung.

Der Editor bietet die drei Eigenschaften und speichert sie nur in einem Weltpaket mit `formatVersion: 1.1.0` oder `1.2.0`. Ältere Pakete verweigern diese Felder verständlich. Für ein Upgrade das Manifest bewusst auf 1.1.0 setzen und den Server neu starten; der Editor ändert das Manifest nicht automatisch. Das neue Offline-Schema liegt unter `schemas/world/1.1.0/`; veröffentlichte ältere Schemas bleiben unverändert. `roomFormat: legacy-1` und `version: 1` bleiben das Grundmodell; das Manifest bestimmt das unterstützte Schema-Bündel.

Die einzige auswählbare Figur ist weiterhin die mitgelieferte Referenzfigur. Ihre Draufsicht ist direkt in Canvas gezeichnet, mit vier Richtungen und wechselnden Schritten. Sie benötigt keine externe Draufsichtdatei, die fehlen könnte. Weitere Figuren oder Grafikzuordnungen gibt es noch nicht. Unbekannte Perspektiven werden abgewiesen statt auf Adventure zurückzufallen.

Die Vorschau berechnet Wege für die aktuellen, auch ungespeicherten Raumdaten auf dem Server. Während der kurzen Anfrage bleibt die Figur stehen; die Vorschau ist keine Mehrteilnehmersitzung. Spiel und Vorschau verwenden denselben Renderer und dieselbe Raumgeschwindigkeit.

## Scrollwelt (Format 1.2.0)
```powershell
dotnet run --project src/AdventureChat.Server -- --World:Root D:\adventure-chat\worlds\scroll --authoring
```
Die Galerie und die Karte haben je 1280×360 Raumpixel, Hindernisse sowie Türen an beiden Enden. Goldene Türflächen wechseln zur anderen Darstellung.

Optionales Raumfeld (Format 1.2.0):
```yaml
viewport:
  width: 640
  height: 360
  scrollMode: horizontal
```
Breite und Höhe sind positive ganze Raumpixel; Höhe muss der Raumhöhe entsprechen. Ohne Feld bleibt der ganze Raum sichtbar. Kürzere Räume scrollen nicht. Die Kamera folgt nur der eigenen Figur außerhalb des mittleren Ruhebereichs von 35–65 %, ohne Nachschwingen; bei Eintritt wird der Spawn innerhalb der Grenzen zentriert. Es gibt keinen Zoom oder vertikales Scrollen.

Im Editor legt „Ausschnittsbreite“ das optionale Feld fest; leer entfernt es. „Ausschnitt verschieben“ bewegt ausschließlich die lokale Bearbeitungsansicht. In der Laufvorschau folgt die Kamera der Figur. Alle Geometriedaten und Eingabeziele bleiben Raumkoordinaten. Für bestehende Pakete Manifest bewusst auf 1.2.0 setzen und Server neu starten; alte Pakete lehnen das neue Feld ab.

Für einen neuen breiten Raum ein passendes PNG importieren und die bearbeitbaren Gesamtmaße auf dessen Pixelabmessungen setzen. Begehbare Flächen, Spawns und Türen mit dem verschiebbaren Ausschnitt platzieren; die Ausschnittsbreite bestimmt nur die Sicht, nicht die Raumgröße.

## Scrollen und Tiefe ausprobieren (Ticket 13)

Alle drei mitgelieferten Welten reference, map und scroll enthalten dieselbe erreichbare Demo-Runde:
- reference: Lounge → rechte Tür zum Garten → Ausgang „Demo: Karte und Tiefe“ bei (540,330).
- map: Karte → Garten → Demo-Ausgang bei (520,330).
- scroll: Galerie → Demo-Ausgang bei (600,325).

1. **Kartenweg (tour-map):** 1280 × 360, Ausschnitt 640 × 360, feste Figurengröße. Von (100,325) nach (1180,325) und zurück laufen. Bei verschobenem Ausschnitt klicken; am rechten Schild geht es zum Hügelpfad, am linken zurück zur Ausgangswelt.
2. **Hügelpfad (tour-depth):** 640 × 360 ohne Scrollen. Von unten nach (320,140) und zurück laufen: oben halbe Größe, unten volle Größe. Weg und Bäume verengen/verkleinern sich nach hinten. Rechtes Schild führt zum Panorama-Hügelweg.
3. **Panorama-Hügelweg (tour-combined):** 1280 × 360 mit Scrollen und Tiefe. (320,140) → (600,325) → (960,140) → (1180,325) laufen. Vorne/hinten und links/rechts vergleichen; zweiter Browser kann am anderen Ende bleiben. Rechts zurück zum Kartenweg, links zum Hügelpfad.

Im Editor dieselben Räume wählen, Tiefenwerte ändern, speichern/neuladen und Vorschau starten. Figurenfußpunkt, Namen, Chatblasen und ruhigen Größenverlauf beobachten. Menschliche Sichtabnahme bleibt separat.

Die neuen Demo-Hintergründe sind originale geometrische Pixelzeichnungen aus scripts/generate-depth-art.js, ohne externe Bildquellen. Erneut erzeugen: `node scripts/generate-depth-art.js`. Kein zusätzlicher npm-Baustein; keine neue Lizenz für das Gesamtprojekt behauptet. Gelieferte Referenzgrafiken und ihre Herkunft bleiben unverändert.


Mara und Bruno erhalten in der Karten-Demo eigenständige schematische Draufsicht-Sheets, erzeugt durch dasselbe lokale Bildwerkzeug. Die gelieferten Adventure-Sheets bleiben unverändert.
