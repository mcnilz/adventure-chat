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

