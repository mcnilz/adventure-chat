# Qualität und Testgrenzen
Neue Testgrenzen vor dem jeweiligen Paket bestätigen lassen. Die für Ticket 08 bestätigten Grenzen stehen im Ticket.

| Öffentliche Grenze | Beobachtungen | Was sie nicht absichert |
| --- | --- | --- |
| Browserbedienung | Klickbewegung, Figuren, Chat, Übergänge, Editor speichern/öffnen | alle numerischen Sonderfälle und manipulierte Nachrichten |
| Öffentliche Raumkommandos über echten Server/SignalR | ungültige Ziele, Identitätsbindung, Reihenfolge, Raumisolierung, Reconnect | tatsächliches Pixellayout |
| Öffentlicher Inhalte-Lade-/Speicherzugang | fehlerhafte YAML-Daten, kaputte Referenzen, Dateischutz | subjektive Bedienqualität |

Keine zusätzliche öffentliche Test-API nur zum Auslesen privater Felder anlegen. Falls Wegfindung separat schwer zu prüfen ist, eine fachliche Modulgrenze für Routenberechnung begründet vorschlagen und bestätigen lassen.

## Vorgehen
Ein beobachtbares Verhalten wählen, zunächst fehlschlagenden Test nachweisen, minimal implementieren, nächstes Verhalten. Keine Sammlung hypothetischer Tests vor der Implementierung. Strukturverbesserungen im Review durchführen und danach betroffene Checks erneut ausführen.
Typechecking und betroffene Tests laufend; vollständige einschlägige Suite am Paketende. Keine unnötige Wiederholung ohne neue Änderung oder Fehler.
Automatisierung für fachliche Regeln und Mehrbenutzerzustand; manuelle Prüfung für Bewegungsgefühl und Editorbedienung. Neue Tests müssen vor der Implementierung des betreffenden Verhaltens scheitern können.

## Werkzeuge – Vorschläge
.NET: NUnit, ohne FluentAssertions. Browser: Playwright für zwei isolierte Kontexte und Editorablauf. Keine zusätzliche TS-Unit-Testplattform ohne konkreten Bedarf. Versions-/Lizenzprüfung dieser Testwerkzeuge gehört vor ihre Aufnahme.

## Kritische Szenarien
- Nicht rechteckiger Weg mit Hindernis, unerreichbares Ziel, Zieländerung während Bewegung.
- Identische logische Zielposition bei unterschiedlichen Browsergrößen.
- Zwei Teilnehmer, Raumwechsel während Chatversand, Verlassen/Verbindungsabbruch.
- Manipulierter Absender, fremde Raum-ID, unmöglicher Übergang und Nachrichtenflut.
- Reconnect-Snapshot ohne Doppelgänger; alter Zustand überschreibt keinen neuen.
- Ungültiges Polygon, fehlendes Bild, ungültiger Zielspawn, doppelte YAML-Schlüssel.
- Atomare Speicherung, erneutes Öffnen nach Serverneustart, Pfad außerhalb des Projektordners abweisen.
- Chattext mit HTML-Zeichen bleibt lesbarer Text und führt nichts aus.

## MVP-Demo
Zwei Räume im Editor neu erstellen und nach Neustart laden. Zwei Browser betreten dieselbe Welt, laufen, chatten und wechseln Räume. Eine Verbindung trennen und erneut verbinden. Anschließend Raumdatei im YAML-Editor mit zugeordnetem Schema bearbeiten und Fehleranzeige prüfen.
Aktuelle tatsächliche Testläufe und Einschränkungen stehen im aktiven Ticket und in `active-state.md`.

## Grenzen für 09–13
Für 09 bestätigt: öffentliche Lade- und Dateisystemgrenze mit temporären Welten, bestehende Editor-/HTTP-Grenze und Browserablauf. Für 10 sind seit 02.11.2026 die öffentlichen Sitzungs-/Hub- und Inhaltsgrenzen sowie Browserabläufe bestätigt: identische lokale IDs und Bildpfade, getrennte Gruppen und Chatverläufe, Weltwahl und Wechsel, Reconnect, Editor-Speichern und Bild-/Vorschauzuordnung. Unveränderte ältere Formatfixtures prüfen zugesicherte Semantik und veröffentlichte Schemas. Visuelle Abnahme ergänzt die Automatisierung. Grenzen für 11–13 bleiben vor Umsetzung zu bestätigen; konkrete Fälle und tatsächliche Nachweise stehen im jeweiligen Ticket.

Für Ticket 11 am 04.11.2026 bestätigt: öffentliche Inhalts-Lade-/Speichergrenze, serverseitige Bewegungs-/Raumwechselgrenze und bestehende Renderer-/Browsergrenzen. Konkrete Fälle und Grenzen im Ticket 11; keine privaten Schritte der Wegfindung testen. Richtungen, Stand-/Schrittzustand, feste Größen und Fußpunkt werden zusätzlich über die öffentliche Canvas-Darstellung geprüft.

Für Ticket 12 am 04.11.2026 bestätigt: öffentliche Kamera-/Koordinatenberechnung, bestehende Renderer-/Browser-/Editorgrenzen sowie öffentliche Weltlade-/Speichergrenze und Serverbewegung als Regression. Sollwerte prüfen Ruhebereich 35–65 %, beide Ränder, Spawnzentrierung, kurzen Raum und CSS-Skalierung. Browser prüfen getrennte Ausschnitte, Chat, Türen, Wechsel und frischen Eintritt. Editor prüft nach Verschiebung gespeicherte Raumkoordinaten. Automatisierung ersetzt keine manuelle Bewertung von Bewegungsgefühl und Editorbedienung. Konkrete Nachweise im Ticket 12.

Für Ticket 14 am 04.11.2026 bestätigt: öffentliche Weltlade-/Inhaltsgrenze, Sitzungs-/Hubgrenze und Browser-/Canvasdarstellung einschließlich gemeinsamer Editor-Vorschau. Fälle: gültige und ungültige Kataloge/Pfade/PNGs/Raster, Weltisolation bei gleichen IDs und Pfaden, zwei Browser mit Name und Figur, Raum-/Weltwechsel und echter Reconnect, beide Raster in allen Richtungen und Stand/Lauf, fehlendes Bild ohne Absturz, unveränderte ältere Fixtures. Keine privaten Rendererfelder. Sichtprüfung der tatsächlichen Figuren bleibt menschliche Abnahme; Integration der Referenzgrafiken folgt erst in Ticket 16.

Für Ticket 15 am 04.11.2026 bestätigt: öffentliche Inhalts-Lade-/Speichergrenze, Editorbedienung und öffentliche Canvas-/Browserdarstellung. Objektvalidierung, PNG/Pfade, Transparenz, davor/dahinter/Gleichstand, mehrere Figuren, Figurengrößen, Kamera und Editor-Roundtrip; Navigation und unveränderte ältere Fixtures als Regression. Maskenkanten und Bediengefühl bleiben menschliche Abnahme. Konkrete Nachweise im Ticket 15.
