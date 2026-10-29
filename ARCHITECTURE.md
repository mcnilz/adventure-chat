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
Der Client sendet ein Ziel und eine fortlaufende Befehlsnummer. Der Server prüft die Sitzung und berechnet einen erlaubten Weg. Ein neuer gültiger Befehl ersetzt den alten Weg. Während des Laufens bleiben Fußpunkte und Wegsegmente im erlaubten Bereich.
Außerhalb liegende oder unerreichbare Ziele werden für den MVP zurückgewiesen und sichtbar markiert; automatische Projektion auf den Rand ist später möglich.
Andere Figuren sind keine Hindernisse. Route und Geschwindigkeit bestimmt der Server. Für die erste Version reicht bestätigte Bewegung mit flüssiger Interpolation; clientseitige Vorhersage nur bei gemessenem Bedarf.
Vor Paket 02 Polygonregeln und Randtoleranzen präzisieren. Kein zweites abweichendes Wegfindungssystem im Browser schreiben.

## Synchronisierung
Beim Betreten liefert der Server einen vollständigen Snapshot mit Raum, Teilnehmern und laufenden Bewegungen. Danach folgen geordnete Änderungen mit Raumrevision bzw. Sequenz und Sitzungsbezug.
Bewegungsberechnung hängt an verstrichener Zeit, nicht an Anzahl empfangener Pakete. Takt und Bandbreite erst messen; keine vorsorgliche Hochfrequenzsimulation.
Nach Wiederverbindung wird erneut ein Snapshot geladen. Vorgeschlagene MVP-Vereinfachung: abgebrochene Sitzung endet, erneuter Eintritt erzeugt genau eine neue Sitzung am gültigen Spawn. Fortsetzen alter Laufwege ist kein MVP-Ziel.
Ziel-, Raum- und Absenderdaten gelten nur nach serverseitiger Prüfung. Nachrichten aus veralteten Raumsitzungen dürfen keinen aktuellen Raum verändern.

## Chat
Nachrichten gelten nur für den aktuellen Raum. Vorschlag: maximal 500 Zeichen; Namen maximal 24 Zeichen; beide nicht leer. Plain Text sicher über Textknoten darstellen, nie als HTML interpretieren.
Server vergibt Nachrichten-ID, Absender und Zeit. Vorschlag: maximal fünf Nachrichten in fünf Sekunden je Sitzung; konkrete Limits vor Paket 04 bestätigen.
Verlauf ist begrenzt, z. B. 200 Nachrichten im Browser. Beim Raumwechsel wird er geleert und der Zielraum klar angezeigt. Keine Zustellung alter Raumhistorie an neue Besucher. Sprechblasen verschwinden vorgeschlagen nach fünf Sekunden, der Verlauf bleibt bis Raumwechsel/Neuladen.

## Darstellung
Original-Pixelart ohne Glättung skalieren, Seitenverhältnis erhalten. Zeichenreihenfolge nach Fußpunkt-Y; echte Vordergrundmasken und perspektivische Skalierung später.
Mindestens eine mitgelieferte Platzhalterfigur mit Stand- und Laufzustand. Bilder pro Richtung können zunächst einfach sein; polierte Charaktergrafik ist kein Abnahmekriterium.
Mausklicks auf Chat und Editorbedienung dürfen keine Bewegung auslösen.

## Editorzugriff
Editor nur in explizitem lokalen Autorenmodus und am Loopback-Interface. Öffentlicher Spielmodus enthält keine schreibbaren Editorendpunkte. Origin/Host prüfen und Schreibzugriffe gegen fremde Webseiten absichern.
Dateizugriffe auf ein festgelegtes Projektverzeichnis beschränken; keine freien absoluten Pfade akzeptieren. PNG-Inhalte und Größenlimits prüfen. Diese Prüfungen gehören in den Editor-Slice, nicht in eine spätere Sicherheitsphase.
Speichern atomar; ungültige Daten überschreiben keine gültigen Dateien. Nicht gespeicherte Änderungen beim Wechsel melden.

## Build und Dependencies
TypeScript strict; erste Option tsc plus Browser-ES-Module, ein zusätzlicher Bundler nur wenn Paketauflösung/Produktionsausgabe ihn rechtfertigen. npm ist Build-Werkzeug, kein Anwendungsserver.
SignalR-Client ist vorgeschlagen; genauen Versionsbaum vor Aufnahme prüfen. YAML-Verarbeitung auf .NET-Seite, keinen Browser-YAML-Parser voraussetzen. YAML- und Schema-Pakete ebenso prüfen.
Keine eigene YAML-Implementierung aus Sicherheitsgründen bauen. Tests benötigen ebenfalls überprüfte Werkzeuge. Lockfiles umfassen Build- und Testabhängigkeiten.

