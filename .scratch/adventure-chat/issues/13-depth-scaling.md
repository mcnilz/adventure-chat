# 13: Perspektivische Figurenskalierung und anschauliche Demo-Welten

**What to build:** Ein Autor hinterlegt im Raum einen kontinuierlichen Skalierungsverlauf. Figuren werden beim Laufen nach hinten kleiner und nach vorne größer. Mitgelieferte Demo-Welten demonstrieren sowohl horizontales Scrollen als auch räumlich nachvollziehbare Tiefenskalierung.

**Blocked by:** 09, 11, 14, 15, 16; Gesamtabnahme der Demo-Welten zusätzlich 12 (done)

**Status:** done

## Menschliche Abnahme · 09.11.2026

Benutzer: „abgenommen“, nach Bestätigung des Demo-Übergangs und der Sichtprüfung. Ticket 13 einschließlich Standardstart-Nachtrag ist damit ausdrücklich abgenommen. Die folgenden ready-for-human-Angaben beschreiben den historischen Stand vor dieser Abnahme. Implementierungscommit 15964f4; Standardstart-Nachtrag 833d13b.

Priorisierung am 04.11.2026 ausdrücklich geändert: Ticket 13 erst nach Abschluss der drei neuen Tickets 14 → 15 → 16 angehen. Diese Reihenfolge ist ein Benutzerauftrag, keine technische Notwendigkeit. Tiefenskalierung auf die dann vorhandene Figurenwahl, Vordergrundobjekte und überarbeitete Referenzwelt aufsetzen; keine Arbeiten aus 13 als Voraussetzung für 16 vorziehen. Konkrete Formatversion erst anhand des dann veröffentlichten Stands festlegen.

## Acceptance criteria

- [x] Optionale Tiefenangaben beschreiben zunächst zwei horizontale Bezugslinien in Raumkoordinaten mit je einem positiven Größenfaktor. Der hintere Bezug liegt oberhalb des vorderen; beide Y-Werte müssen verschieden sein. Die konkrete YAML-Struktur und sinnvolle numerische Grenzen werden vor Implementierung festgelegt.
- [x] Zwischen den Linien wird der Faktor linear und kontinuierlich anhand des Y-Werts des Figurenfußpunkts interpoliert. Außerhalb des Intervalls gilt der jeweilige Randfaktor; keine unkontrollierte Extrapolation. Das gilt für jede zulässige Position im Raum, unabhängig von X.
- [x] Endgültige Figurengröße ergibt sich aus fester Raumgröße aus Ticket 11 mal Tiefenfaktor. Skalierung bleibt am Fußpunkt verankert. Beim Laufen entstehen keine abrupten Größenwechsel; ganzzahlige Rasterung darf keine groben Skalierungsstufen erzwingen. Namen und Sprechblasen bleiben passend zur sichtbaren Figur positioniert und lesbar.
- [x] Skalierung hängt von der Raumposition ab, niemals von Bildschirmposition oder Kameraoffset. Horizontales Scrollen und CSS-Skalierung verändern den Tiefenfaktor nicht.
- [x] Spielerpositionen, Wegfindung, Ausgänge und Klickziele behalten ihre bisherige Raumkoordinaten-Semantik. Tiefenfaktor beeinflusst zunächst weder serverseitige Geschwindigkeit noch Hindernisabstand oder Kollision. Eine perspektivische Geschwindigkeitsanpassung wäre ein eigener Folgeumfang.
- [x] Ohne Tiefenangaben gilt Faktor 1. Alte Räume und Karten behalten ihre feste Figurengröße. Keine neue Tiefenkarte pro Pixel, keine 3D-Engine und keine Bereiche mit unterschiedlichen seitlichen Gradienten erforderlich.
- [x] Editor erlaubt Bezugslinien und Faktoren einzustellen und eine Vorschaufigur an verschiedenen Positionen zu zeigen. Nach Speichern und Neuladen bleibt derselbe Verlauf erhalten. Ungültige Faktoren, gleiche/verkehrte Bezugslinien und nicht endliche Werte werden verständlich zurückgewiesen.
- [x] Zwei Teilnehmer sehen bei gleicher Raumposition dieselbe Größe, auch mit unterschiedlichen Kameraoffsets und Browserfenstergrößen. Änderung folgt der lokal interpolierten, serverbestätigten Figurenposition.
- [x] Jede mitgelieferte Referenz- oder Demo-Welt enthält nach Einführung beider Features drei unterschiedliche Demonstrationsräume: (1) horizontaler Scrollraum mit fester Figurengröße, (2) Raum mit kontinuierlicher Tiefenskalierung, der vollständig im sichtbaren Ausschnitt liegt und nicht scrollt, (3) kombinierter Raum mit horizontalem Scrollen und Tiefenskalierung, beispielsweise ein breiter, nach hinten ansteigender Hügelweg. Alle drei Beispiele sind erreichbar und einzeln in der Demo-Anleitung beschrieben. Mindestens einer der scrollenden Demo-Räume ist ausdrücklich eine Kartenansicht mit Draufsicht-Figuren und festem Figurenmaßstab. Vorzugsweise übernimmt der reine Scrollraum diese Rolle; damit genügen weiterhin drei Räume. Seine Abnahme umfasst Scrollen nach links und rechts, korrekte Klickziele bei verschobenem Ausschnitt und Übergänge in normale Räume. Eine zusätzliche vierte Demo ist nicht erforderlich. Diese Vorgabe gilt für mitgelieferte Inhalte, nicht als zusätzliche Gültigkeitsbedingung für eigene oder ältere externe Welten.
- [x] Gestaltung erklärt die Tiefe optisch: beispielsweise ein breiter Weg, der vom Vordergrund nach hinten einen Hügel hinauf führt, mit passenden Größenverhältnissen, Wegverengung und klaren begehbaren Flächen. Vorne/hinten liegende Bezugspunkte sind tatsächlich erreichbar. Kein bloßer technischer Skalierungsversuch auf unpassendem Hintergrund; keine Klettermechanik erforderlich.
- [x] Demo-Anleitung nennt Welt, Räume und einen kurzen Weg zum Ausprobieren von Scrollen und Tiefe. Mindestens ein Ablauf kombiniert Laufen von vorne nach hinten und zurück mit horizontalem Scrollen. Bildherkunft und Lizenz bleiben dokumentiert.
- [x] Neue optionale Formatfelder erhalten kompatible Defaults und begründete SemVer-Einstufung. Neue Schema-Version ergänzen; veröffentlichte Schemas und alte Referenzfixtures unverändert lassen. Frühere Kompatibilitätsfixtures werden nicht nachträglich mit Demo-Features versehen.
- [x] Architektur, Autorenformat, Glossar, Testkonzept, PLAN.md und active-state.md sind aktualisiert. Review berücksichtigt Zusammenspiel der Tickets 11–13, Abwärtskompatibilität und tatsächliche visuelle Eignung der Demo. Menschliche Abnahme bleibt separat.

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

## Bestätigung · 09.11.2026
Benutzer bestätigt Umfang und Testgrenzen mit „ja ok“. Format 1.5.0, depthScale mit back/front (y, factor), 0 ≤ back.y < front.y ≤ Raumhöhe, Faktoren 0.25–2; gemeinsame Darstellung, Editor und drei Demo-Szenarien je mitgelieferter Welt. Abnahme Ticket 16 erfolgt. Reviewbasis: 7733562.

Am 09.11.2026 zusätzlich ausdrücklich bestätigt: optionales Draufsicht-Sheet je Katalogfigur. topDownImage in Format 1.5.0 verwendet dasselbe Raster; ohne Feld bleiben alte Welten unverändert. Schematische Mara-/Bruno-Sheets in der Karten-Demo.

## Umsetzung und Prüfbelege · 09.11.2026

Bestätigter Umfang implementiert: depthScale wird über öffentliche YAML-/Speicher-, HTTP- und Snapshot-Grenzen erhalten. Reine gemeinsame Höhenberechnung aus ungerundetem Raum-Y; Endfaktoren begrenzt, feste Grundhöhe unverändert. Figuren, Namensschilder und Sprechblasen verwenden dieselbe Höhe. Navigation, Geschwindigkeit, Kollision, Ausgänge und Kamera bleiben unabhängig. Bei aktivem Tiefenverlauf bleiben Beschriftungen auch bei einem Kopf oberhalb des Bildrands sichtbar.

Editor mit Aktivschalter und vier Zahlen; Speichern/Neuladen und gemeinsame Laufvorschau. Alle drei mitgelieferten Welten enthalten tour-map (fester Maßstab, Draufsicht, horizontaler Ausschnitt), tour-depth (Tiefe ohne Ausschnitt) und tour-combined (beides), jeweils über die bisherigen Räume erreichbar. IDs erhalten die ursprüngliche Reihenfolge in der Raumliste. Originale geometrische Demo-PNGs und schematische Mara-/Bruno-Draufsicht-Sheets sind mit scripts/generate-depth-art.js aus Node-Standardmodulen reproduzierbar. Keine neue Abhängigkeit; Herkunft in README und Referenz-Provenienz dokumentiert. Gelieferte Adventure-Sheets und alte Referenzfixture unverändert.

Format 1.5.0 ist eine additive Minor-Erweiterung: optionaler Tiefenverlauf und ausdrücklich bestätigtes optionales topDownImage mit bisherigen Defaults. Frühere Schema-Bündel einschließlich Teilschemas bleiben unverändert. Zusätzlich alte Karten-/Scrollwelten aus 7733562 unter tests/Fixtures/map-v1 und scroll-v1 eingefroren: alle zwölf Dateien unabhängig gegen Git-Blobs bzw. Git-LFS-SHA256 bytegetreu geprüft. Bestehende Karten-/Scrollregressionen prüfen diese Altformate; neue Tests prüfen aktuelle Demos separat. Inhaltsversionen reference 2.1.0, map/scroll 1.1.0 wegen zusätzlicher Räume und Verbindungen.

Rot/Grün: öffentliche YAML-/Snapshot-Grenze zunächst am fehlenden Tiefenfeld gescheitert; kontinuierliche Darstellung zunächst am fehlenden Modul; Beschriftungsrandfall zunächst an unsichtbaren Textpositionen. Nach Implementierung grün. Katalogprüfung weist topDownImage in älteren Versionen sowie fehlende PNGs zurück; pixelgenauer Browservergleich bestätigt Draufsicht-/Adventure-Auswahl beider Referenzfiguren. Ein anfänglicher Editor-Test wartete zu kurz auf das asynchrone Laden des Raums; auf tatsächliche Raum-ID korrigiert. Ein vorläufiger Gesamtlauf wurde für die abschließenden Fixture-/Raumreihenfolge-Anpassungen abgebrochen und nicht als Erfolg gewertet.

Abschließend: Typcheck und Clientbuild erfolgreich; 17 gezielte Inhaltsfälle und fünf Browserfälle grün. Vollsuite 256 Tests bestanden (182 Server, 74 Browser, keine Warnungen; Browserdauer 3 min 58 s). Diffprüfung sauber. Separate Standards- und Spec-Subagenten gegen 7733562: nach Korrektur der vertikalen Beschriftung, URL-Duplizierung und UTF-8-/Statusdokumentation keine offenen Befunde. Keine technische Prüfung als menschliche Abnahme gewertet.

Belege: [hintere Tiefe mit Kamera](../evidence/13/depth-combined-back.png), [vordere Tiefe](../evidence/13/depth-combined-front.png), [Hügelraum](../evidence/13/depth-room.png), [Editorvorschau](../evidence/13/depth-editor.png), [Draufsicht und Adventure-Sheets](../evidence/13/depth-reference-overhead.png).

## Konkrete menschliche Abnahme

1. Starten: `dotnet run --project src/AdventureChat.Server -- --World:Root D:\adventure-chat\worlds\reference --authoring`. Browser mit Strg+F5 neu laden. Mara bzw. Bruno in zwei Browsern wählen.
2. Lounge → Garten → Demo-Ausgang bei (540,330). Auf dem Kartenweg links/rechts laufen, nach Kameraverschiebung klicken und Draufsichtfigur prüfen. Links zurück zum Garten, rechts zum Hügelpfad.
3. Im Hügelpfad von (100,325) nach (320,140) und zurück laufen. Fußpunkt, ruhige Größenänderung, Name und Chatblase beurteilen. Rechtes Schild zum Panorama-Hügelweg.
4. (320,140) → (600,325) → (960,140) → (1180,325) laufen. Zweiten Browser am anderen Ende lassen; Größe derselben Figur bei verschiedenen Fenstergrößen vergleichen. Chat senden. Rechts zurück zum Kartenweg.
5. Editor mit worldId=reference öffnen; tour-depth wählen. Bezugslinien/Faktoren ändern, speichern, neu laden und Vorschau an verschiedenen Stellen ausprobieren. Ungültige Werte zurückweisen lassen. Schematische Draufsicht-Sheets und räumliche Gestaltung bleiben Sichtabnahme.
6. Dieselbe Runde in Kartenwelt (Karte → Garten, Demo bei 520,330) und Scrollwelt (Galerie, Demo bei 600,325) prüfen. Kurzanleitung auch in README.

Status ready-for-human; ausdrückliche Abnahme bleibt offen. Lokaler Implementierungscommit: „Implement perspective depth scaling and connected demo tours (ticket 13)“.

## Nachtrag: Standardstart · 09.11.2026

Benutzerauftrag: „ich will das ohne angabe von paramtern die referenz welt gestartet wird“. Ohne World:Root/World:Roots und ohne explizites Legacy-Content:Root lädt die Anwendung nun automatisch die aktuelle Referenzwelt. Im Repository wird die Quellwelt verwendet (auch für Autorenänderungen); eine eigenständig gebaute Anwendung enthält die erforderlichen Manifest-, Raum- und Bilddateien und verwendet diese Kopie. Explizite Konfigurationen bleiben vorrangig. Keine neue Abhängigkeit, kein Formatbezug, keine Änderung veröffentlichter Schemas oder ihrer Interpretation.

Öffentlicher HTTP-Starttest zuerst rot (leere Weltliste), danach grün. Gebaute Debug-Publikation zusätzlich außerhalb des Repositorys in temporärem Verzeichnis ohne Argumente gestartet: Weltliste und aktuelle Lounge über HTTP erfolgreich geprüft. Lokale Verbindungen waren bei eingeschränkter Ausführung blockiert; Prüfung mit lokalen Ausführungsrechten erfolgreich. Temporäre Liefer-/Testdateien anschließend entfernt. Bestehender Legacy-API-Test nennt seine Inhaltsquelle nun ausdrücklich.

Typcheck/Clientbuild und abschließende Vollsuite erfolgreich: 257 Tests (183 Server, 74 Browser; keine Warnungen). Separate Standards-/Spec-Nachprüfung gegen 15964f4: keine offenen Befunde nach README-Korrektur. Diffprüfung sauber. Status weiterhin ready-for-human; keine vorweggenommene Abnahme. Lokaler Nachtragscommit: „Start bundled reference world by default“.

Abnahme: Im Verzeichnis src/AdventureChat.Server einfach `dotnet run` ausführen; Referenzwelt mit Mara/Bruno und Lounge muss ohne Weltargument angeboten werden. Zum Editor weiterhin ausdrücklich `--authoring` verwenden.
