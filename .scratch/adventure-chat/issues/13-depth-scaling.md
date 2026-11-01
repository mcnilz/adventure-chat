# 13: Perspektivische Figurenskalierung und anschauliche Demo-Welten

**What to build:** Ein Autor hinterlegt im Raum einen kontinuierlichen Skalierungsverlauf. Figuren werden beim Laufen nach hinten kleiner und nach vorne größer. Mitgelieferte Demo-Welten demonstrieren sowohl horizontales Scrollen als auch räumlich nachvollziehbare Tiefenskalierung.

**Blocked by:** 09, 11; Gesamtabnahme der Demo-Welten zusätzlich 12

**Status:** draft

## Acceptance criteria

- [ ] Optionale Tiefenangaben beschreiben zunächst zwei horizontale Bezugslinien in Raumkoordinaten mit je einem positiven Größenfaktor. Der hintere Bezug liegt oberhalb des vorderen; beide Y-Werte müssen verschieden sein. Die konkrete YAML-Struktur und sinnvolle numerische Grenzen werden vor Implementierung festgelegt.
- [ ] Zwischen den Linien wird der Faktor linear und kontinuierlich anhand des Y-Werts des Figurenfußpunkts interpoliert. Außerhalb des Intervalls gilt der jeweilige Randfaktor; keine unkontrollierte Extrapolation. Das gilt für jede zulässige Position im Raum, unabhängig von X.
- [ ] Endgültige Figurengröße ergibt sich aus fester Raumgröße aus Ticket 11 mal Tiefenfaktor. Skalierung bleibt am Fußpunkt verankert. Beim Laufen entstehen keine abrupten Größenwechsel; ganzzahlige Rasterung darf keine groben Skalierungsstufen erzwingen. Namen und Sprechblasen bleiben passend zur sichtbaren Figur positioniert und lesbar.
- [ ] Skalierung hängt von der Raumposition ab, niemals von Bildschirmposition oder Kameraoffset. Horizontales Scrollen und CSS-Skalierung verändern den Tiefenfaktor nicht.
- [ ] Spielerpositionen, Wegfindung, Ausgänge und Klickziele behalten ihre bisherige Raumkoordinaten-Semantik. Tiefenfaktor beeinflusst zunächst weder serverseitige Geschwindigkeit noch Hindernisabstand oder Kollision. Eine perspektivische Geschwindigkeitsanpassung wäre ein eigener Folgeumfang.
- [ ] Ohne Tiefenangaben gilt Faktor 1. Alte Räume und Karten behalten ihre feste Figurengröße. Keine neue Tiefenkarte pro Pixel, keine 3D-Engine und keine Bereiche mit unterschiedlichen seitlichen Gradienten erforderlich.
- [ ] Editor erlaubt Bezugslinien und Faktoren einzustellen und eine Vorschaufigur an verschiedenen Positionen zu zeigen. Nach Speichern und Neuladen bleibt derselbe Verlauf erhalten. Ungültige Faktoren, gleiche/verkehrte Bezugslinien und nicht endliche Werte werden verständlich zurückgewiesen.
- [ ] Zwei Teilnehmer sehen bei gleicher Raumposition dieselbe Größe, auch mit unterschiedlichen Kameraoffsets und Browserfenstergrößen. Änderung folgt der lokal interpolierten, serverbestätigten Figurenposition.
- [ ] Jede mitgelieferte Referenz- oder Demo-Welt enthält nach Einführung beider Features drei unterschiedliche Demonstrationsräume: (1) horizontaler Scrollraum mit fester Figurengröße, (2) Raum mit kontinuierlicher Tiefenskalierung, der vollständig im sichtbaren Ausschnitt liegt und nicht scrollt, (3) kombinierter Raum mit horizontalem Scrollen und Tiefenskalierung, beispielsweise ein breiter, nach hinten ansteigender Hügelweg. Alle drei Beispiele sind erreichbar und einzeln in der Demo-Anleitung beschrieben. Mindestens einer der scrollenden Demo-Räume ist ausdrücklich eine Kartenansicht mit Draufsicht-Figuren und festem Figurenmaßstab. Vorzugsweise übernimmt der reine Scrollraum diese Rolle; damit genügen weiterhin drei Räume. Seine Abnahme umfasst Scrollen nach links und rechts, korrekte Klickziele bei verschobenem Ausschnitt und Übergänge in normale Räume. Eine zusätzliche vierte Demo ist nicht erforderlich. Diese Vorgabe gilt für mitgelieferte Inhalte, nicht als zusätzliche Gültigkeitsbedingung für eigene oder ältere externe Welten.
- [ ] Gestaltung erklärt die Tiefe optisch: beispielsweise ein breiter Weg, der vom Vordergrund nach hinten einen Hügel hinauf führt, mit passenden Größenverhältnissen, Wegverengung und klaren begehbaren Flächen. Vorne/hinten liegende Bezugspunkte sind tatsächlich erreichbar. Kein bloßer technischer Skalierungsversuch auf unpassendem Hintergrund; keine Klettermechanik erforderlich.
- [ ] Demo-Anleitung nennt Welt, Räume und einen kurzen Weg zum Ausprobieren von Scrollen und Tiefe. Mindestens ein Ablauf kombiniert Laufen von vorne nach hinten und zurück mit horizontalem Scrollen. Bildherkunft und Lizenz bleiben dokumentiert.
- [ ] Neue optionale Formatfelder erhalten kompatible Defaults und begründete SemVer-Einstufung. Neue Schema-Version ergänzen; veröffentlichte Schemas und alte Referenzfixtures unverändert lassen. Frühere Kompatibilitätsfixtures werden nicht nachträglich mit Demo-Features versehen.
- [ ] Architektur, Autorenformat, Glossar, Testkonzept, PLAN.md und active-state.md sind aktualisiert. Review berücksichtigt Zusammenspiel der Tickets 11–13, Abwärtskompatibilität und tatsächliche visuelle Eignung der Demo. Menschliche Abnahme bleibt separat.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Skalierungs-/Darstellungsgrenze, Inhaltsvalidierung, Editor und Browserabläufe. Endpunktfaktoren, Zwischenwert, Begrenzung außerhalb der Linien, unveränderter Fußpunkt, fehlende Angaben und ungültige Werte prüfen. Kameraoffset und CSS-Skalierung dürfen keinen Einfluss auf den Tiefenfaktor haben. Editor-Roundtrip und ältere unveränderte Fixtures absichern.

Visuelle Abnahme ergänzt numerische Tests: Vorne/hinten laufen, Fußpunkt und ruhigen Größenverlauf beobachten; dabei scrollen und zweiten Teilnehmer vergleichen. Ein Inhaltscheck prüft die Feature-Beispiele aller mitgelieferten Demo-Welten, ersetzt aber nicht die menschliche Beurteilung, ob deren räumliche Gestaltung schlüssig ist.

## Vorgehen

1. Bezugslinien-/Faktorvertrag, bestehenden Fußpunkt und Darstellung prüfen; konkrete Felder, Wertebereiche und öffentliche Testgrenzen bestätigen lassen.
2. Skalierungsverlauf in Spiel und Editor implementieren, mit Grundgröße und Kamera kombinieren.
3. Sinnvolle Demo-Räume gestalten und jede mitgelieferte Demo-Welt auf Scroll- und Tiefenbeispiel prüfen.
4. Schema, Kompatibilität, lokale Checks und Review abschließen; ready-for-human und konkrete Abnahmeanleitung dokumentieren.

## Nicht im Umfang

Seitlich unterschiedliche Tiefenfelder, 3D-Geometrie, automatische Tiefenschätzung, Klettern, Verdeckung durch Vordergrundobjekte, perspektivisch veränderte Geschwindigkeit und Zoomsteuerung.
