# 12: Horizontal scrollende Räume

**What to build:** Ein Autor erstellt einen Raum, der breiter ist als der sichtbare Bildausschnitt. Beim Laufen folgt die Kamera der eigenen Figur horizontal. Dies funktioniert sowohl mit bisheriger Adventure-Darstellung als auch mit der Kartenansicht, ohne Laufzeit-Zoom.

**Blocked by:** 09; für die Abnahme beider Darstellungen zusätzlich 11, für die Mehrwelt-Regressionsprüfung 10

**Status:** draft

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
