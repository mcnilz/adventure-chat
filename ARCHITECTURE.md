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

## Geplante Erweiterung: Weltpakete und Darstellung

Ticket 10: Ein Laufzeitkatalog hält pro konfigurierter Welt eigene Raum- und Sitzungsdienste. Der Hub bindet die Verbindung beim Beitritt an deren Welt; Gruppen enthalten Welt- und Raum-ID. Explizites Verlassen bereinigt die Sitzung und Gruppen vor einer neuen Auswahl. Der Spielbrowser startet für einen Weltwechsel seine Seite neu; Reconnect tritt dagegen mit der gemerkten Welt-ID frisch am Startpunkt ein. Im Mehrweltbetrieb gibt es keine lokale HTTP-Ersatzbewegung bei Verbindungsverlust.

Autorenanfragen wählen mit `worldId` einen weltgebundenen Editordienst, der den Raumdienst der jeweiligen Spielsitzungen aktualisiert. Alle bisherigen Zugriffskontrollen gelten davor. Gespeicherte Ressourcenpfade bleiben relativ; HTTP-Raumdaten und Hub-Snapshots liefern weltbezogene Bildadressen. Die Bearbeitungsansicht und Vorschau lösen relative Bildpfade ausschließlich für die ausgewählte Welt auf.
Ticket 09 implementiert externe Weltpakete; Tickets 10–13 erweitern sie gemäß dem [Kompatibilitätsvertrag](docs/work-packages/world-format-compatibility.md). Welt-ID ist die vorgesehene Grenze für Räume, Ressourcen, Sitzungen und Chat. Ticket 09 betreibt eine ausgewählte Welt; Isolation gleichnamiger IDs im Mehrweltbetrieb folgt in Ticket 10. Laufender Zustand bleibt flüchtig. Optionale Draufsicht und feste Figurengröße, horizontale lokale Kamera und Tiefenskalierung behalten Raumkoordinaten und Fußpunkt als Grundlage. Ohne neue Angaben gilt bisheriges Verhalten; Kameraoffset ist lokal und kein Serverzustand. Konkrete Formate und Grenzen werden vor dem jeweiligen Paket bestätigt.

Ticket 11: Weltformat 1.1.0 trägt Perspektive, feste Figurenhöhe und separate Laufgeschwindigkeit. Der Server liefert die Angaben im Raum-Snapshot und bestimmt die Bewegung. Canvas-Renderer werden in Spiel und Editor geteilt; Editor-Vorschauwege nutzen dieselbe serverseitige Wegfindung mit ungespeicherten Raumdaten. Details und Kompatibilitätsmatrix in `docs/work-packages/world-format-compatibility.md`.

Ticket 12: `rendering/camera.ts` definiert den gemeinsamen Ausschnitt und die Bildschirm-/Raumumrechnung. Der lokale Renderer hält die horizontale Kamera, begrenzt sie am Raumrand und setzt sie beim Raumwechsel zurück. Die Raumebenen erhalten dieselbe Canvas-Translation, die Verbindungsanzeige folgt danach in Bildschirmkoordinaten. Eingabe und Tür-Hover lesen den aktuellen lokalen Offset. Der Editor hält eine eigene Bearbeitungsverschiebung; die Vorschau verwendet die Spielkamera. Das optionale Formatfeld `viewport` ist Konfiguration, kein gemeinsamer Kamerazustand.

Ticket 14: Weltpakete validieren den optionalen Figurenkatalog an der Inhaltsgrenze. `/api/worlds/{id}/characters` liefert ausschließlich validierte weltbezogene Bildadressen und Rasterdaten. `JoinRoom` akzeptiert eine Figuren-ID; der Hub löst Default und erlaubte IDs aus seiner Welt auf. Sitzung, Snapshots und Eintrittsereignisse führen dieselbe ID. Der Client löst sie mit dem vor Eintritt geladenen Katalog auf; der gemeinsame CharacterRenderer hält Bilder nach vollständiger Weltadresse und skaliert Quellframes zur Raumhöhe. Name und Figur stehen in der Weltwahl, der Editor verwendet den Default. Laufzeitbesitz und Weltisolation bleiben unverändert.

Ticket 15: RoomObjectValidation prüft optionale grafische Objekte getrennt von Raumgeometrie und Navigation. Das Format 1.4.0 erweitert die explizite Unterstützungsmatrix. RoomService bindet Objektbilder an die Weltadresse; HTTP und Hub liefern dieselben Objektdaten. ObjectRenderer zeichnet PNG-Alpha in Spiel und Editor; RoomRenderer mischt Objekte mit Figuren nach Tiefen-Y und zeichnet Namen/Sprechblasen anschließend. EditorObjects verwaltet Objektformular und vorhandene Uploadschnittstelle; der Canvas erlaubt Verschieben. Bearbeitung und eindeutige Auswahl verwaltet EditorState. Vertragsdetails in CONTENT-FORMAT.md.

## Bearbeitungszustand im Raumeditor (Ticket 17)
EditorState besitzt den bearbeiteten Raumentwurf, die Auswahl und den Ungespeichert-Status. Inspektor und Seitenformular lesen DOM-Eingaben; Objektverwaltung und Canvas reichen Bearbeitungen ebenfalls an das Zustandsmodul weiter. Die TypeScript-Raumansicht für lesende Aufrufer ist tief schreibgeschützt. Kleine Kernfunktionen bündeln Eigenschafts-, Koordinaten-, Objekt- und Entitätsregeln ohne DOM oder Netzwerk. Reine Auswahl und wirkungslose Eingaben erhöhen keine Bearbeitungsrevision; unverändert angezeigte optionale Defaults werden erhalten.

Ein Speichervorgang sendet eine Kopie mit Entwurfsidentität und Bearbeitungsrevision. Erfolg setzt nur genau diese noch aktuelle Revision auf sauber; neuere Bearbeitungen bleiben ungespeichert, Antworten für einen inzwischen anderen Entwurf verändern dessen Status nicht. Während des Speicherns ist weitere Bearbeitung möglich; die Speichern-Schaltfläche bleibt bis zum Abschluss gesperrt. Raumwahl und „Neu“ verlangen vor dem Verwerfen ungespeicherter Änderungen eine Bestätigung. Abbruch erhält Entwurf und Raumauswahl; verspätete Ladeantworten überschreiben keine inzwischen erfolgte Bearbeitung. Die Browserwarnung beim Verlassen verwendet denselben Änderungsstatus. Kein Undo/Redo und keine automatische Speicherung; keine Änderung des Autorenformats.

## Tiefenverlauf (Ticket 13)
RoomDepthScale wird vom versionierten Inhaltslader validiert, über HTTP und SignalR unverändert geliefert und im Editor erhalten. Die reine Funktion actorHeightAt im gemeinsamen Renderbereich berechnet die Zielhöhe anhand des lokalen, interpolierten Raumfußpunkts. Figurenrenderer, Namensschild und Sprechblasen verwenden dieselbe Höhe. Keine Änderung an Simulation, Wegfindung oder Kamera. Das Bildwerkzeug generate-depth-art.js erzeugt lokale Demo-PNGs mit Node-Standardmodulen; es ist keine Serverlaufzeit und benötigt keine neue Abhängigkeit.
