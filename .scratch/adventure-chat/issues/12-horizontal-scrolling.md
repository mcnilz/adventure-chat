# 12: Horizontal scrollende Räume

**What to build:** Ein Autor erstellt einen Raum, der breiter ist als der sichtbare Bildausschnitt. Beim Laufen folgt die Kamera der eigenen Figur horizontal. Dies funktioniert sowohl mit bisheriger Adventure-Darstellung als auch mit der Kartenansicht, ohne Laufzeit-Zoom.

**Blocked by:** 09; für die Abnahme beider Darstellungen zusätzlich 11, für die Mehrwelt-Regressionsprüfung 10

**Status:** ready-for-human

## Acceptance criteria

- [ ] Gesamter Raum und sichtbarer Ausschnitt sind getrennte Größen. Der Autor legt eine logische Ausschnittsgröße und horizontales Scrollverhalten fest. Konkrete Felder und Wertebereiche vor Implementierung festlegen. Raumhöhe entspricht im ersten Schritt der logischen Ausschnittshöhe; vertikales Scrollen ist nicht Bestandteil dieses Pakets.
- [ ] Die Kamera folgt ausschließlich der eigenen Figur horizontal. Ein mittlerer Ruhebereich verhindert Scrollen bei jeder kleinen Bewegung. Dessen konkrete Grenzen und gegebenenfalls zeitabhängige Glättung vor Implementierung abstimmen; kein dauerhaftes Nachschwingen im Stand.
- [ ] Der Ausschnitt bleibt innerhalb der Raumgrenzen. Am linken und rechten Rand erscheinen keine Flächen außerhalb des Hintergrunds. Die Figur bleibt beim erlaubten Laufen sichtbar.
- [ ] Beim Eintritt wird die Kamera vor dem ersten sichtbaren Bild passend zum Spawn gesetzt. Raumwechsel und Reconnect übernehmen keinen Kameraoffset aus einem vorherigen Raum.
- [ ] Spielerpositionen, Polygone, Wege, Startpunkte und Ausgänge bleiben in Koordinaten des gesamten Raums. Die Kamera verändert nur die lokale Darstellung, niemals den serverseitigen Zustand.
- [ ] Mausklicks und Hover-Erkennung berücksichtigen Bildschirmposition, CSS-Skalierung, eventuelle Bildränder und Kameraoffset. Ein sichtbarer Punkt ergibt vor, während und nach Scrollen das richtige Raumziel. Chat- und Editorbedienung lösen weiterhin keine Bewegung aus.
- [ ] Figuren, Hintergrund, Namen, Sprechblasen, Ausgänge und Geometrie-Overlays verwenden dieselbe Kameratransformation. Chatpanel und übrige Bildschirmbedienung bleiben ortsfest.
- [ ] Jeder Teilnehmer hat seinen eigenen Kameraoffset. Andere Figuren dürfen außerhalb des Ausschnitts liegen, bleiben aber Teil derselben Raumsitzung. Raumchat bleibt raumbezogen; Sichtbarkeit im Ausschnitt wird nicht zur neuen Kommunikationsgrenze.
- [ ] Editor bietet Vorschau und Verschieben des Ausschnitts, damit Autoren beide Raumenden bearbeiten und Kameraführung ausprobieren können. Editorbewegung der Vorschau ändert keine Raumkoordinaten. Bestehende Zeichen- und Auswahlwerkzeuge funktionieren bei verschobenem Ausschnitt.
- [ ] Bestehende Räume ohne neue Angaben behalten die bisherige vollständige Darstellung. Räume bis zur Ausschnittsbreite scrollen nicht; kleinerer Inhalt wird gemäß bisherigem Skalierungs-/Randverhalten dargestellt.
- [ ] Feste Figurengröße aus Ticket 11 bleibt unabhängig vom Kameraoffset. Keine Zoomsteuerung, kein Parallaxeffekt, kein vertikales Scrollen und keine neue Wegfindung nur wegen der Kamera.
- [ ] Autorenformat und versionierte Schemas erhalten passende optionale Felder mit kompatiblen Defaults; SemVer-Einstufung und Unterstützungsmatrix werden begründet aktualisiert. Veröffentlichte Schemas und ältere Referenzfixtures bleiben unverändert.
- [ ] Breiter Referenzraum hat Hindernisse und Ausgänge an beiden Enden. Zwei Browser prüfen Bewegung, unterschiedliche Ausschnitte, Raumwechsel und Chat. Kartenansicht wird ebenfalls mit Scrollen abgenommen.
- [ ] Dokumentation, PLAN.md, TESTING.md und active-state.md nennen das Paket, öffentliche Testgrenzen, tatsächliche Checks und manuelle Abnahmeschritte. Umsetzung erst nach Bestätigung, Status done erst nach menschlicher Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Koordinatenumrechnung und Kameraberechnung, bestehende Renderer-/Browser- und Editorgrenzen; vorhandene Server-Bewegungstests als Regression. Keine Tests für private Kamerafelder.

Gezielte Fälle: linke/rechte Kameragrenze; Ruhebereich; Spawn an beiden Raumenden; kurzer und breiter Raum; Klick auf dasselbe Raumziel mit verschiedenen Offsets; CSS-Skalierung und Fenstergrößenänderung; Ausgang-Hover und Klick bei verschobenem Bild; Zielwechsel während des Laufens; Namens-/Sprechblasenposition; Editor-Geometrie nach Verschieben korrekt speichern; zwei Browser weit auseinander im selben Raum; Raumwechsel und Reconnect ohne alten Offset; unveränderte alte Raumdatei. Prüfen, dass Kameraoffsets nicht in serverseitige Bewegungsbefehle oder gemeinsame Zustände gelangen.

## Vorgehen

1. Aktuelle Render-, Eingabe- und Editortransformationen prüfen. Einheitlichen Raum-/Bildschirmvertrag, Ausschnittskonfiguration und Testgrenzen konkretisieren und bestätigen lassen.
2. Breiten Referenzraum mit begrenzter lokaler Kamera darstellen; korrekte Eingaben und Overlays absichern.
3. Ruhige Kameraführung, Eintritt und Raumwechsel abschließen; unabhängige Ausschnitte mit zwei Browsern prüfen.
4. Editorvorschau, Formatkompatibilität, Dokumentation und Review ergänzen.

## Manuelle Abnahme

Von links nach rechts und zurück laufen, unterwegs Ziele und Türen anklicken und beide Ausgänge benutzen. Zweiten Browser am anderen Raumende stehen lassen: Beide Ausschnitte folgen ausschließlich der jeweiligen eigenen Figur; Chat bleibt gemeinsam. Im Editor nach Verschieben des Ausschnitts einen Punkt bearbeiten, speichern und dessen unveränderte Raumkoordinaten prüfen. Gleichen Ablauf mit Kartenansicht und einer älteren nicht scrollenden Welt prüfen.

## Bestätigtes Paket · 04.11.2026
Benutzerbestätigung „ja“ für Umfang, Details und Testgrenzen. Optionales `viewport: { width, height, scrollMode: horizontal }`; positive ganzzahlige Breite, Höhe gleich Raumhöhe; kürzere Räume zeigen weiterhin ihren vollständigen Inhalt. Ruhebereich 35–65 %, direktes Folgen ohne Nachschwingen; Spawn zentriert innerhalb der Raumgrenzen. Format 1.2.0 als Minor-Erweiterung, alte Schemas und Fixtures unverändert.

Bestätigte Grenzen: öffentliche Kamera-/Koordinatenfunktionen, bestehende Renderer-/Browser-/Editorgrenzen, Weltladen und Formatkompatibilität sowie bestehende Serverbewegung als Regression. Keine privaten Kamerafelder als Testgrenze.

## Implementierung
Gemeinsame lokale Kamera mit Raumkoordinaten für Rendering und Eingabe; alle Raumebenen verschoben, Verbindungsanzeige und Chat ortsfest. Canvas-Seitenverhältnis folgt dem Ausschnitt. Editor: optionale Ausschnittsbreite, lokaler Schieberegler und Laufvorschau mit Spielrenderer. Neue Referenzwelt `worlds/scroll` mit Galerie und Karte (1280×360, Ausschnitt 640×360), Hindernissen und Ausgängen an beiden Enden. Keine neuen direkten Abhängigkeiten.

## Bisherige Nachweise
- Rot/Grün: 1.2.0-Weltladen zunächst abgewiesen, nach Schema-/Servererweiterung grün.
- Rot/Grün: öffentlicher Kameratest scheiterte zunächst am fehlenden Modul, danach grün. Ruhebereich, Ränder, kurzer Raum und CSS-Umrechnung mit bekannten Sollwerten.
- Zwei Browser: unabhängige Ausschnitte, Chat über Raumenden, echtes Hover/Klicken auf verschobene Tür, Kartenbewegung, Rückwechsel und frischer Eintritt nach Neuladen bestanden.
- Editor: verschobenen Spawn auswählen, ändern, speichern, neu laden; Raumkoordinate 1190 bleibt erhalten; Vorschauweg und gesperrter Bearbeitungsregler geprüft.
- Typcheck und Clientbuild bestanden. Abschließende Vollsuite nach Reviewkorrekturen: `dotnet test --no-restore`, 191 bestanden, 0 Warnungen (155,2 Sekunden). Diffprüfung sauber. Beide Reviewachsen nach Korrekturen ohne offene Befunde.

## Review und Korrekturen
Review gegen Ausgangscommit `2cb3dff82ffe511c872e2010999aee4886200530` durch zwei getrennte Subagenten (Standards und Spec):
- Standards: gemeinsame Umrechnung auch im Editor verwendet, Sprechblasenparameter als sichtbare rechte Grenze benannt; ungültige Ausschnittsbreite lässt die Zeichenfläche bis zur serverseitigen Speicherablehnung auf voller Raumbreite. Nachprüfung ohne offene Befunde.
- Spec: Raummaße jetzt als positive ganze Zahlen bearbeitbar; Neuanlage mit breitem Hintergrund, Speichern und Neuladen als Browserablauf ergänzt. Rechter Eintritt prüft Offset 640. Tatsächlicher TCP-Abbruch aus dem rechten Ausschnitt prüft neue Sitzung am Start mit Offset 0. Nachprüfung ohne offene Befunde.
- Drei gezielte Scroll-Browsertests nach Korrekturen bestanden. Screenshotprüfung von Spiel und Editor bestanden. Ältere Schemas einschließlich Teilschemas, `content/rooms`, `worlds/reference` und `worlds/map` haben keinen Diff zum Ausgangscommit.

## Konkrete manuelle Abnahme
1. Server mit `dotnet run --project src/AdventureChat.Server -- --World:Root D:\adventure-chat\worlds\scroll --authoring` starten; angezeigte lokale Adresse in zwei Browsern öffnen und „Scrollwelt“ betreten.
2. In der Galerie nach rechts und zurück laufen. Kamera soll erst außerhalb der mittleren 35–65 % folgen, am Rand stoppen und im Stand ruhig bleiben. Unterwegs das Ziel ändern und rechts/links die goldenen Türflächen verwenden.
3. Einen Browser am anderen Raumende stehen lassen und chatten. Ausschnitte müssen unabhängig, Chat und Teilnehmer weiterhin gemeinsam sein. Kartenraum ebenso prüfen, Fenstergröße ändern und anschließend eine Verbindung unterbrechen/wiederherstellen.
4. `/editor.html` öffnen: Ausschnitt verschieben, Spawn oder Polygonpunkt am rechten Ende ändern, speichern und neu laden; Raumkoordinate im Inspektor kontrollieren. Im Vorschaumodus bis an beide Enden laufen. Einen neuen Raum mit breitem PNG, passenden Gesamtmaßen und kleinerer Ausschnittsbreite anlegen.
5. Eine bestehende Welt aus `worlds/reference` beziehungsweise `worlds/map` ohne Ausschnittsangaben starten: weiterhin vollständige Darstellung und bisherige Bewegung. Bewegungsgefühl und subjektive Editorbedienung bleiben menschliche Abnahme; automatisierte Tests ersetzen sie nicht.

## Übergabe · 04.11.2026
Implementierungscommit: `6dd96c7`. Nur lokale Arbeit; keine neuen Abhängigkeiten. Status `ready-for-human`, menschliche Abnahme steht aus. Offene Implementierungsbefunde: keine. Nach ausdrücklicher Abnahme Ticket 12 auf done setzen; Ticket 13 anschließend gesondert vorschlagen und bestätigen lassen.

## Nachtrag: Sprechblasen außerhalb des Ausschnitts
Benutzerfeedback am 04.11.2026: „das funktioniert schon super“, mit direktem Umsetzungsauftrag für Sprechblasen unsichtbarer Sprecher. Ticket für diese Ergänzung zurück auf in-progress. Blasen erscheinen am linken beziehungsweise rechten Ausschnittsrand mit seitlichem Zeiger in Richtung des Sprechers. Beim Wiedereintritt in den Ausschnitt folgt die Blase wieder der Figur mit unterem Zeiger. Laufzeit und Ausblenden bleiben erhalten.

Rot/Grün an der bestehenden öffentlichen Canvas-Renderergrenze nachgewiesen: beide Randrichtungen, Rückkehr zur Figur und abgelaufene Nachricht. Typcheck und Build grün. Kein Autorenformatbezug: ausschließlich lokale Darstellung vorübergehender Chatnachrichten; keine Änderung von Raumdaten, Schemas, Semantik oder unterstützten Formatversionen. Keine neuen Abhängigkeiten.

Zusätzliche manuelle Abnahme: Zwei Browser an entgegengesetzten Enden desselben Raums positionieren und abwechselnd schreiben. Im jeweils anderen Browser muss die Sprechblase am passenden Rand mit seitlichem Zeiger erscheinen. Während die Nachricht noch sichtbar ist zum Sprecher laufen und den Wechsel zur normalen Blase prüfen.

Nachtragsreview: Standards- und Spec-Prüfung durch getrennte Subagenten ohne offene Befunde. Gleichzeitige Blasen auf derselben Höhe können sich wie bisher überlagern; keine neue Stapel- oder Priorisierungsregel eingeführt.

Abschluss des Nachtrags: Vollsuite 192 Tests bestanden, 0 Warnungen (158,3 Sekunden); Typcheck/Build und Diffprüfung bestanden. Implementierungscommit `a046edb`. Status wieder ready-for-human; menschliche Abnahme der Ergänzung steht aus.
