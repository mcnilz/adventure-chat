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
Für Ticket 18 am 09.11.2026 mit „ja genau“ bestätigt: bestehende öffentliche Browser-/Canvasgrenze für Gesamtoberfläche bei 1280×480, 640×360, 800×900 und 1920×1080. Sichtbare Außenkanten, proportionaler Maßstab, Standardbegrenzung, Umschalten und Resize in beiden Modi, reale Klickbewegung, Chat und Raumwechsel; bestehende Scrolltests als Regression. Lesbarkeit und Pixelartwirkung bleiben menschliche Sichtabnahme. Keine zusätzliche Test-API und keine Änderung der Formatgrenzen.

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

Für Ticket 13 am 09.11.2026 mit „ja ok“ bestätigt: öffentliche Skalierungs-/Canvasdarstellung, Inhaltsvalidierung und Snapshot, Editor-Speicherung und Browserabläufe. Endpunkte, Zwischenwert einschließlich Bruchteilen, Begrenzung, Defaults und ungültige Werte; ältere unveränderte Fixtures, unterschiedliche Kameraoffsets/Fenstergrößen, zwei Teilnehmer und erreichbare Demo-Szenarien. Automatisierung ersetzt keine Sichtabnahme der räumlichen Gestaltung und des Bewegungsgefühls.

## Grenzen für 20–23

Ticket 21 am 12.11.2026 mit „ja“ für die im Paketvorschlag genannten Entscheidungen und öffentlichen Grenzen bestätigt: Inhalte-Laden/Speichern, echte Bewegungs-/Aktions-/Übergangskommandos und Browser/Canvas samt isolierter Editorvorschau. Fußpunkt einschließlich Sperrrand; mindestens ein zuvor erreichbarer lokaler Sicherheitsanker; blockierten vorgesehenen Spawn ablehnen; gültigen Umweg oder sicheren Stopp ohne automatische Fortsetzung. Konkrete Fälle und tatsächliche Belege im Ticket 21. Keine neue öffentliche Geometrie-Testgrenze und kein Nachweis globaler Weltlösbarkeit.

Ticket 20 wurde am 12.11.2026 einschließlich der unten genannten Grenzen bestätigt. Die Grenzen der weiteren Pakete bleiben vor ihrer Umsetzung zu bestätigen.

Ticket 22 wurde am 19.11.2026 für Browser/Canvas und Editor sowie die öffentlichen Inhalts- und SignalR-Grenzen bestätigt. Die Browserabläufe prüfen Verben mit zwei Spielern, Speichern und Neuladen von Zuständen, Aktionen und Verknüpfungen, einen ausdrücklich gewählten Formatwechsel mit Fehlerfällen sowie isolierte Vorschauaktionen und Raumwechsel. Bediengefühl und Satzverständlichkeit bleiben menschliche Abnahme.

Ticket 23 wurde am 20.11.2026 für öffentliche Inhalts- und Navigationsprüfungen und echte Browserabläufe in einer isolierten Referenzweltkopie bestätigt. Dazu gehören Gegenaktionen mit zwei Teilnehmern, Wegsperren, Laufstopp, sichere Ablehnung, späterer Eintritt, Raumrückkehr, Wiederverbindung, Weltneustart und Speichern, Neuladen und Aktionen im Editor. Alte Referenzfixtures, Tour und Weltisolation laufen als Regression mit. Grafik und Bediengefühl bleiben menschliche Abnahme.

Für Ticket 26 sind Inhaltsvalidierung, Speichern und Neuladen im Editor, die öffentlichen Raumkommandos und echte Browserabläufe mit zwei Teilnehmern bestätigt. Geprüft werden der gewählte Bedienpunkt, Blickrichtung, Pose und Geste im Canvas, Platzkonkurrenz, Abbruch und sichtbare Objektwirkung beim späteren Eintritt. Die Lesbarkeit der Geste und das Bediengefühl bleiben menschliche Abnahme.

Für Ticket 27 wurden am 21.11.2026 die öffentlichen Inhalts- und Browsergrenzen bestätigt. Fälle sind die Validierung und das Speichern von NPCs mit Katalogverweisen, die Regressionsprüfung mit älteren Referenzfixtures sowie browserseitig zwei unabhängige Gespräche, Reichweite, Abbruch, Raumrückkehr und Trennung vom Raumchat. Die visuelle Einbindung der Figur und die Bedienung bleiben menschliche Abnahme.

Für Ticket 28 wurden am 21.11.2026 Laden und Speichern mit Objekt- und Zustandsverweisen, unveränderte ältere Referenzfixtures, eine veraltete Auswahl nach der Aktion eines zweiten Besuchers sowie die getrennte Editorvorschau bestätigt. Die Browserprüfung deckt auch die Reihenfolge passender Antworten und den Ausstieg bei leerer Auswahl ab.

| Paket | Vorgeschlagene öffentliche Grenzen / Schwerpunkt | Ausdrückliche Grenze |
| --- | --- | --- |
| [20](.scratch/adventure-chat/issues/20-shared-object-actions.md) | Inhalts-Laden/Speichern, echte SignalR-Kommandos, Browser/Canvas: reversible Zustände, atomare direkte Wirkungen, Konkurrenz, Snapshots, Weltisolation, Neustart | Keine privaten Sperren/Zustandscontainer testen; keine Script-Ausführung oder Persistenz zusichern. |
| [21](.scratch/adventure-chat/issues/21-state-dependent-navigation.md) | Inhaltsgrenze und öffentliche Bewegungs-/Aktions-/Übergangskommandos: dynamische Sperren, Fußpunkt, lokale Sicherheitsanker, laufende Wege und Mehrfachänderungen | Sicherheitsankerdefinition und Toleranzen vor Umsetzung bestätigen. Kein Beweis globaler Welt-/Rätsellösbarkeit oder kontinuierlicher Möbelphysik. |
| [22](.scratch/adventure-chat/issues/22-verbs-and-interaction-editor.md) | Browser/Editor, öffentliche Speicher-/Hubgrenzen: Satzbildung, echte Verbwahl, Roundtrip, Vorschauisolation, Referenzen, explizite Formatwahl, Skalierung | Keine neue Test-API; Bediengefühl und Satzverständlichkeit bleiben menschlich. |
| [23](.scratch/adventure-chat/issues/23-interactive-reference-examples.md) | Echte Referenzabläufe in isolierter Weltkopie: zwei Spieler, späterer Eintritt, Tür/Stuhl/Schalter/Tor, Navigation, Editor, Reconnect/Neustart | Grafik-/Szenenwirkung bleibt menschliche Abnahme; keine bestehenden Abnahmewelten oder eingefrorenen Fixtures ändern. |
| [25](.scratch/adventure-chat/issues/25-object-state-transitions.md) | Am 20.11.2026 bestätigt. Inhalte laden und speichern, Editor und Vorschau, Browser/Canvas mit zwei Teilnehmern und späterem Eintritt. Beide Richtungen, konkurrierende Bedienung, sichere Ablehnung und ältere unveränderte Referenzfixtures. | Lesbarkeit der Bewegung bleibt menschliche Sichtabnahme. |

Bei jedem Formatpaket unveränderte ältere Schemas/Teilschemas und Referenzfixtures samt zugesicherter alter Semantik prüfen. Neue Daten nur in neuen Testwelten beziehungsweise temporären Kopien. Typcheck/Build und vollständige einschlägige Suite gehören zur späteren Umsetzung, wurden für diesen reinen Planungsauftrag nicht ausgeführt. Detaillierte Fälle und offene Entscheidungen stehen in den Tickets.
