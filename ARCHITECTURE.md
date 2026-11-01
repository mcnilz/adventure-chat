# Architektur und Laufzeitverhalten

## Überblick
Ein ASP.NET-Core-Prozess stellt statische Browserdateien, Inhaltedaten und die Echtzeitverbindung bereit. Ein nur lokal aktivierter Editor verwendet gesonderte Endpunkte. Keine Build-Werkzeuge im ausgelieferten Server erforderlich.
Der Browser rendert mit Canvas 2D; Chat und Formulare sind normales HTML/CSS. Der Editor verwendet denselben Raumrenderer und dieselbe Koordinatenumrechnung.
Die Module unten sind Verantwortlichkeiten, keine Anweisung für je ein Projekt oder Interface.

## Verantwortlichkeiten
| Bereich | Aufgabe |
| --- | --- |
| Inhaltelader | YAML lesen, Schema und fachliche Referenzen prüfen, Laufzeitdaten liefern |
| Raumzustand | Teilnehmer, Ziele, Wege und bestätigte Bewegungen verwalten |
| Echtzeitadapter | Sitzungen und Nachrichten transportieren, Verbindungen bereinigen |
| Browserdarstellung | Bilder, Animationsframes, Namen, Sprechblasen und Interpolation |
| Raumeditor | Geometrie und Eigenschaften bearbeiten, prüfen, speichern und testen |

## Bewegung
Vorgeschlagene MVP-Regel: logische Pixelkoordinaten im Hintergrundbild, Ursprung links oben, Fußpunkt als Figurenposition. Fenstergröße verändert keine Weltdaten.
Ein Raum besitzt einfache, nicht selbstschneidende begehbare Polygone. Hindernisse werden zunächst durch nicht begehbare Bereiche ausgespart; separate Kollisionsobjekte entfallen.
Der Client sendet ein Ziel. Der Server berechnet die aktuelle Position aus Weg und Zeit und ermittelt den neuen Weg mit 130 logischen Pixeln pro Sekunde. Eine vom Client mitgesendete Startposition und Fortschrittsmeldungen verändern die Serverposition nicht. Ein neuer gültiger Befehl ersetzt den alten Weg.
Außerhalb liegende oder unerreichbare Ziele werden für den MVP zurückgewiesen und sichtbar markiert; automatische Projektion auf den Rand ist später möglich.
Andere Figuren sind keine Hindernisse. Route und Geschwindigkeit bestimmt der Server. Für die erste Version reicht bestätigte Bewegung mit flüssiger Interpolation; clientseitige Vorhersage nur bei gemessenem Bedarf.
Vor Paket 02 Polygonregeln und Randtoleranzen präzisieren. Kein zweites abweichendes Wegfindungssystem im Browser schreiben.

## Synchronisierung
Beim Betreten liefert der Server einen vollständigen Snapshot mit Raum, Teilnehmern und laufenden Bewegungen. Danach folgen geordnete Änderungen mit Raumrevision bzw. Sequenz und Sitzungsbezug.
Raumwechsel verwenden die Ausgangs-ID; der Server löst das Ziel auf und akzeptiert den Wechsel innerhalb des Ausgangsradius zuzüglich fünf Pixeln Toleranz. Bewegungsereignisse tragen Raum-ID und eine laufende Bewegungsrevision.
Bewegungsberechnung hängt an verstrichener Zeit, nicht an Anzahl empfangener Pakete. Takt und Bandbreite erst messen; keine vorsorgliche Hochfrequenzsimulation.
Nach Wiederverbindung wird erneut ein Snapshot geladen. Vorgeschlagene MVP-Vereinfachung: abgebrochene Sitzung endet, erneuter Eintritt erzeugt genau eine neue Sitzung am gültigen Spawn. Fortsetzen alter Laufwege ist kein MVP-Ziel.
Ziel-, Raum- und Absenderdaten gelten nur nach serverseitiger Prüfung. Nachrichten aus veralteten Raumsitzungen dürfen keinen aktuellen Raum verändern.

## Chat
Nachrichten gelten nur für den aktuellen Raum. Sie sind auf 200 Zeichen und Namen auf 24 Zeichen begrenzt. Der Server vergibt Nachrichten-ID, Absender und Zeit und begrenzt den Versand auf fünf Nachrichten in drei Sekunden. Der Server hält die letzten 50 Nachrichten je Raum vor.

## Darstellung
Original-Pixelart ohne Glättung skalieren, Seitenverhältnis erhalten. Zeichenreihenfolge nach Fußpunkt-Y; echte Vordergrundmasken und perspektivische Skalierung später.
Mindestens eine mitgelieferte Platzhalterfigur mit Stand- und Laufzustand. Bilder pro Richtung können zunächst einfach sein; polierte Charaktergrafik ist kein Abnahmekriterium.
Mausklicks auf Chat und Editorbedienung dürfen keine Bewegung auslösen.

## Editorzugriff
Editor nur in explizitem lokalen Autorenmodus und am Loopback-Interface. Öffentlicher Spielmodus enthält keine schreibbaren Editorendpunkte. Origin/Host prüfen und Schreibzugriffe gegen fremde Webseiten absichern.
Der Schalter `--authoring` aktiviert den Editor; ohne ihn antworten die Autoren-APIs mit 403 und die Editorseite mit 404. Host und Origin werden geprüft, Schreibzugriffe benötigen ein sitzungsgebundenes Antiforgery-Token. Uploads akzeptieren ausschließlich PNG mit geprüfter Signatur bis 10 MiB.
Dateizugriffe auf ein festgelegtes Projektverzeichnis beschränken; keine freien absoluten Pfade akzeptieren. PNG-Inhalte und Größenlimits prüfen. Diese Prüfungen gehören in den Editor-Slice, nicht in eine spätere Sicherheitsphase.
Speichern atomar; ungültige Daten überschreiben keine gültigen Dateien. Nicht gespeicherte Änderungen beim Wechsel melden.

## Build und Dependencies
TypeScript strict; erste Option tsc plus Browser-ES-Module, ein zusätzlicher Bundler nur wenn Paketauflösung/Produktionsausgabe ihn rechtfertigen. npm ist Build-Werkzeug, kein Anwendungsserver.
SignalR-Client ist vorgeschlagen; genauen Versionsbaum vor Aufnahme prüfen. YAML-Verarbeitung auf .NET-Seite, keinen Browser-YAML-Parser voraussetzen. YAML- und Schema-Pakete ebenso prüfen.
Keine eigene YAML-Implementierung aus Sicherheitsgründen bauen. Tests benötigen ebenfalls überprüfte Werkzeuge. Lockfiles umfassen Build- und Testabhängigkeiten.

## Client-Modulstruktur (TypeScript)
Der Browser-Client ist nach dem Single-Responsibility-Prinzip in saubere, entkoppelte Module gegliedert (siehe auch `docs/agents/coding-standards.md`):
- `types.ts` & `constants.ts`: Strikte Domänentypen, DTO-Verträge und Konfigurationskonstanten.
- `math.ts`: Reine Geometrie- und Vektorfunktionen (Polygon-Einschlusstests, Distanzen).
- `simulation/movement.ts`: Seiteneffektfreie Bewegungsberechnung, Wegpunktfortschritt und Schritt-Animationen.
- `state/room-state.ts`: Entitätenverwaltung (`RoomState`, lokale und entfernte Spieler, Chatverlauf).
- `network/room-client.ts`: SignalR-Verbindungslebenszyklus, Server-Invokes und HTTP-Fallback.
- `input/input-handler.ts`: Koordinatentransformation (`screenToLogical`), Tür-Hit-Testing und Mausevents.
- `rendering/`:
  - `renderer.ts`: Master `RoomRenderer` als Kompositions-Schicht.
  - `character-renderer.ts`: Richtungsabhängiges Figuren- und Namensschild-Rendering.
  - `speech-bubble-renderer.ts`: Mehrzeilige Sprechblasen und Textumbruch.
  - `overlay-renderer.ts`: Debug-Geometrien, Klick-Ripples, Tür-Highlights und Verbindungsstatus.
- `ui/chat-ui.ts`: XSS-sicheres Chat-Panel, Aktionszeile und DOM-Ereignisentkopplung.
- `game.ts`: `AdventureGame`-Fassade zur Orchestrierung aller Teilsysteme unter Wahrung stabiler E2E-Hooks.

