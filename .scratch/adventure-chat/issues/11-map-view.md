# 11: Begehbare Kartenansicht mit fester Figurengröße

**What to build:** Ein Autor erstellt einen Raum als begehbare Karte mit Draufsicht-Figuren. Teilnehmer können darin gemeinsam laufen und chatten sowie über Kartenorte normale Räume betreten. Perspektive und Figurengröße werden vom Autor pro Raum festgelegt; es gibt keinen zur Laufzeit veränderbaren Zoom.

**Blocked by:** 09 (versioniertes Autorenformat); für die Gesamtabnahme im Mehrweltbetrieb zusätzlich 10

**Status:** draft

## Acceptance criteria

- [ ] Räume behalten das gemeinsame Grundmodell aus Hintergrund, begehbaren Flächen, Startpunkten und Ausgängen. Eine optionale Darstellungsangabe unterscheidet bisherige Adventure-Ansicht und Draufsicht. Ein eigener Raumtyp wird nur bei nachgewiesenen abweichenden fachlichen Regeln eingeführt und begründet.
- [ ] Der Autor legt die Figurengröße pro Raum als festen Maßstab fest. Die genaue Einheit, der Bezugspunkt und zulässige Werte werden vor Implementierung festgelegt. Keine Zoomsteuerung, keine dynamische Kamera und keine Größenänderung durch Teilnehmer.
- [ ] Mindestens eine Referenzfigur besitzt Draufsichtgrafiken mit erkennbaren Bewegungsrichtungen und Stand-/Laufzuständen. Fehlende Grafiken werden im Autorenmodus verständlich gemeldet; kein unbemerkter Wechsel zur falschen Perspektive. Die Behandlung weiterer Figuren ohne Draufsichtgrafiken wird vor Implementierung festgelegt.
- [ ] Spiel und Editorvorschau zeigen übereinstimmende Perspektive, Größe und Figurenposition. Skalierung verändert keine Weltkoordinaten und umgeht keine serverseitige Bewegungsprüfung. Namen, Sprechblasen und anklickbare Ausgänge bleiben benutzbar.
- [ ] Die Laufgeschwindigkeit kann als separate Autoreneinstellung pro Raum festgelegt werden; diese Ergänzung ist ein Vorschlag, vor Implementierung bestätigen. Ohne neue Angabe gilt die bisherige Geschwindigkeit. Der Server bestimmt die Bewegung; alle Clients interpolieren denselben Verlauf.
- [ ] Kartenorte nutzen bestehende Ausgänge mit Zielraum und Ziel-Spawn. Ein Kartenraum führt in mindestens zwei normale Räume, von denen eine Rückkehr auf die Karte möglich ist. Perspektive, Maßstab und gegebenenfalls Geschwindigkeit wechseln korrekt mit dem Raum.
- [ ] Eine Referenzkarte enthält ein Labyrinth mit Verzweigungen, Sackgasse, Hindernissen und schmalen, für die konfigurierte Geometrie zulässigen Durchgängen. Zunächst vorhandene Wegfindung prüfen; Änderungen nur bei belegter Lücke, kein vorsorglicher Algorithmuswechsel.
- [ ] Wege schneiden keine Wände oder gesperrten Flächen. Unerreichbare Ziele werden nach den bestehenden Regeln zurückgewiesen. Bewegungsziel während des Laufens ändern funktioniert auch im Labyrinth.
- [ ] Die optische Figurengröße wird nicht still zum Kollisionsradius. Vor Umsetzung den vorhandenen Fußpunkt-/Abstandsvertrag prüfen und dokumentieren, wie Durchgangsbreite, Hindernisabstand und sichtbare Figur zusammenpassen. Keine neue Figurenkollision erforderlich.
- [ ] Zwei Browser sehen konsistente Bewegung, Anwesenheit, Raumwechsel und Chat auf der Karte. Vorhandene Welt- und Raumisolation bleibt erhalten.
- [ ] Bestehende Raumdateien behalten ohne neue Felder ihre bisherige Darstellung, Größe und Bewegung. Optionale Felder mit kompatiblen Defaults erhalten eine begründete Minor-Einstufung; veröffentlichte Schemas bleiben unverändert. Neue Formatversion und Unterstützungsmatrix ergänzen, alte Referenzwelten unverändert testen.
- [ ] Raumeditor bietet Ansicht und feste Größe als Eigenschaften sowie Vorschau. Speichern verwendet eine Formatversion, die diese Angaben tatsächlich unterstützt.
- [ ] Dokumentation und Glossar unterscheiden Draufsicht, Figurengröße, Bildschirmskalierung und Laufgeschwindigkeit. Tatsächliche Checks, Review und manuelle Abnahmeschritte sind dokumentiert; done erst nach menschlicher Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Inhaltsvalidierung, serverseitige Bewegungs-/Raumwechselgrenze, vorhandene Renderer-/Browsergrenzen. Nicht die internen Schritte eines Wegfindungsalgorithmus nachbauen.

Gezielte Fälle: bestehender Raum ohne neue Angaben; Draufsicht mit zwei festen Größen; ungültiger Maßstab; fehlende Draufsichtgrafik; Editor speichern und neu laden; Kartenraum zu normalem Raum und zurück; Labyrinthroute um Hindernisse; schmaler zulässiger Durchgang; unerreichbares Ziel; Richtungswechsel während Bewegung; zwei Browser mit übereinstimmender Darstellung und Bewegung. Alte Raumfixtures bleiben unverändert. Visuelle Prüfung ergänzt Verhaltensprüfungen bei Fußpunkt, Sprite-Skalierung, Namen und Sprechblasen.

## Vorgehen

1. Bestehendes Raum-, Sprite- und Bewegungsmodell lesen. Konkrete Darstellungsfelder, Maßstab, Grafikzuordnung und Testgrenzen vorschlagen. Die noch vorgeschlagene Geschwindigkeitsoption bestätigen lassen.
2. Draufsichtfigur und Referenzkarte durchgehend darstellen, vorhandene Raumübergänge nutzen.
3. Labyrinthfälle an bestehender Wegfindung prüfen und nur notwendige Korrekturen umsetzen.
4. Editor, Formatversion, Kompatibilitätsfixtures, Dokumentation und Review abschließen.

## Nicht im Umfang

Inventar, Rätsel, Skripting, dynamischer Zoom, Kamerascrolling, automatisch erzeugte Labyrinthe und eine neue Weltzustandspersistenz.

## Manuelle Abnahme

Karte betreten, Draufsicht und feste Größe prüfen, durch das Labyrinth laufen, währenddessen ein neues Ziel anklicken, zwei Kartenorte betreten und zurückkehren. Mit zweitem Browser Bewegung und Chat prüfen. Danach eine unveränderte ältere Welt öffnen und deren bisheriges Verhalten bestätigen.
