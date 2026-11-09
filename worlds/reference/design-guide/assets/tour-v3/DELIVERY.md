# Ticket 19 · Endgrafiken / lokale Übergabe

Die vom Benutzer mit „passt“ freigegebenen Motive und der gezeigte erhöhte, leicht schräge Draufsichtstil wurden fortgeführt. Diese Lieferung entsteht ausschließlich im aktuellen lokalen Designprojekt. `D:\adventure-chat`, Ticket 18, bestehende Spiel-/Editorserver, `.codex/config.toml` und globale Einstellungen wurden nicht verändert. Die ursprüngliche Entwurfsmappe mit ihren Bildern und Dokumenten bleibt erhalten.

## Tatsächliche Dateien

| Datei | Lieferung |
| --- | --- |
| [characters/mara-top-down.png](characters/mara-top-down.png) | Transparentes Mara-Lauf-Sheet, exakt 512×384 |
| [characters/bruno-top-down.png](characters/bruno-top-down.png) | Transparentes Bruno-Lauf-Sheet, exakt 512×384 |
| [backgrounds/kartenweg.png](backgrounds/kartenweg.png) | Freigegebener Kartenweg, bytegleich, 1280×360 |
| [backgrounds/huegelpfad.png](backgrounds/huegelpfad.png) | Freigegebener Hügelpfad, bytegleich, 640×360 |
| [backgrounds/panorama.png](backgrounds/panorama.png) | Freigegebenes Panorama, bytegleich, 1280×360 |
| [index.html](index.html) | Lokales Laufstudio mit beiden Figuren, vier Richtungen, Stand, Einzelphasen, 160ms-Zyklus und Untergrundwechsel |
| [qa/mara-contact.png](qa/mara-contact.png), [qa/bruno-contact.png](qa/bruno-contact.png) | Separate QA-Kontaktbögen aller 24 Posen, mit Zellrahmen, oberer Reserve und Fußankermarkierung |
| [qa/target-size.png](qa/target-size.png) | Alle 24 Posen im tatsächlichen 32px-Zielrahmen; Mara links, Bruno rechts, Zeilen rechts/hinten/vorne |
| [technical-checks.json](technical-checks.json) | Unabhängige Dekodierung, Abmessungen, SHA-256, Alpha und Zellmessungen |

Die laufende lokale Adresse steht in [LOCAL-PREVIEW.md](LOCAL-PREVIEW.md). Aktueller Vorschau-Einstieg: [Laufstudio](http://127.0.0.1:54094/production/). Der separate Vorschauprozess bindet ausschließlich an 127.0.0.1; die Seite wurde per HTTP mit Status 200 erreicht. Alternativ `production/index.html` direkt im Browser öffnen. Alle Referenzen, Bilder, Stile und Skripte sind lokal; kein CDN, Webfont oder externer Dienst ist zum Anzeigen erforderlich.

## Sheetvertrag und tatsächliche Aufbereitung

- Je vier Spalten und drei Reihen mit 128×128 pro Zelle. Reihe0 rechts, Reihe1 hinten/aufwärts, Reihe2 vorne/abwärts. Spalte0 ist Stand und zugleich erste Laufphase; Phasenfolge Kontakt, Durchgang, Gegenkontakt, zweiter Durchgang.
- Links ist ausschließlich die im bestehenden Renderer vorgesehene Spiegelung der Rechtsreihe in der Vorschau. Kein linker Sheetbereich und keine für die Montage synthetisierten Spiegel-/Drehposen.
- Alle 24 Körper besitzen nach unabhängiger Messung exakt 120 sichtbare Pixel Höhe, von y8 bis einschließlich y127. Geometrischer Rahmenanker (64,128); die sichtbaren Silhouetten sind mittig innerhalb von einem halben Pixel zentriert.
- Alle 24 Zellen sind belegt, ohne deckende Seitenrandpixel, mit jeweils genau einer zusammenhängenden 8-Nachbarschaft-Silhouette. Je Figur zwölf unterschiedliche RGBA-Frame-Hashes. Keine identischen Kopien als Gangphasen.
- Fertige Alpha-Werte ausschließlich 0 oder 255. Transparente Ausgabepixel besitzen RGB0. Keine QA-Linien, Beschriftungen oder Bodenschatten in die Spiel-Sheets eingebrannt.
- Bei nächster-Nachbar-Abtastung auf 32×32 wurden je Figur wiederum zwölf unterschiedliche Ziel-Frames und in allen 24 Fällen exakt 30 sichtbare Pixel Höhe gemessen. [Zielgrößenmessungen](qa/target-size-checks.json).

Die Montage nutzt vorhandene lokale Standardwerkzeuge: Node-Standardbibliothek für PNG-Dekodierung/-Montage und Windows `System.Drawing` als unabhängigen zweiten Dekoder/Messer. Keine Bibliothek wurde installiert. Die erlaubte deterministische Verarbeitung besteht aus Alpha-Schwelle128, Entfernung kleiner isolierter Komponenten, Beschnitt des tatsächlichen Körpers, proportionaler nächster-Nachbar-Abtastung auf 120px Höhe und Zellmontage. Es wurden keine fehlenden Gliedmaßen oder Posen durch Code gezeichnet, bewegt, gedreht oder gespiegelt. Verwendete Ausschnitte, Skalierungen und Quellen je Zelle: [assembly-records.json](assembly-records.json). Messwerkzeug: [tools/verify-and-contact.ps1](tools/verify-and-contact.ps1).

## Tatsächlicher Generierungsweg und Quellen

Alle neuen Figurenzeichnungen stammen aus demselben eingebauten Werkzeug `image_gen__imagegen`, das bereits die Entwurfsbilder geliefert hatte. Kein OpenDesign-Cloud-Lauf, keine zusätzliche Anbieter-/BYOK-Anmeldung, kein direkter API-/CLI-Generierungsfallback, keine Installation und keine Subagenten. Daraus folgt keine Zusicherung vollständiger Offline-Bildberechnung. Das Werkzeug hat kein Bildmodell, keine Urheberangabe und keine Lizenz offengelegt; solche Angaben werden nicht erfunden.

Es wurden zunächst sechs Reihen mit je vier Posen erzeugt. Die Sichtprüfung führte zu gezielten Korrekturen der Gegenphasen beider Rückreihen und Brunos rechter Reihe. Einzelne Armschwünge wurden anschließend mit demselben Bildwerkzeug korrigiert; nur die betroffenen Posen wurden montiert. Eine verworfene einzelne Bruno-Pose mit abweichender Kopf-/Volumenwirkung bleibt ebenfalls als Original erhalten. **Alle 13 tatsächlich gelieferten Generierungs-PNGs** liegen unverändert unter `production/sources/`; ausgewählte und verworfene Quellen werden nicht vermischt oder als zusätzliche fertige Figuren ausgegeben.

- [sources/all-generations.json](sources/all-generations.json): vollständiger Quellenbestand mit Abmessungen und SHA-256.
- [sources/rows.json](sources/rows.json): ausgewählte Reihen und konkrete Einzelposen-Overrides einschließlich ursprünglicher Werkzeugpfade und Kopiengleichheit.
- [sources/rows-initial.json](sources/rows-initial.json): ursprüngliche sechs Reihen vor Korrekturen.
- [generation-prompts.json](generation-prompts.json), [correction-prompts.json](correction-prompts.json), [single-pose-correction.json](single-pose-correction.json), [final-arm-correction.json](final-arm-correction.json), [back-arm-correction-prompts.json](back-arm-correction-prompts.json): tatsächliche Referenzen und Prompts der Erzeugungen/Korrekturen.

Jeder Auftrag erhielt das freigegebene kanonische Mara-/Bruno-Konzept. Die ersten Reihen erhielten zusätzlich die tatsächlichen Adventure-Sheets als Identitätsreferenz; gezielte Folgekorrekturen erhielten die jeweilige erzeugte Reihe als Bearbeitungsreferenz. Das ursprüngliche kanonische Konzept wurde nicht bloß gedreht oder gestreckt.

Die drei Hintergründe wurden **nicht neu erzeugt** und **nicht weiter beschnitten**. SHA-256 und unabhängige Kopienprüfung stehen in [backgrounds/provenance.json](backgrounds/provenance.json) sowie `technical-checks.json`. Die vorherigen großen Erzeugungsquellen unter `../assets/sources/` bleiben erhalten. Für diese benutzergelieferten Referenzen gelten weiterhin die Grenzen der ursprünglichen Herkunftsdatei: Modell-/Urheber-/Rechtebelege werden nicht ergänzt, wenn sie nicht vorliegen.

## Sichtprüfung und echte Grenzen

Alle 24 endgültigen Posen wurden in den Kontaktbögen einzeln betrachtet und gegen die tatsächlichen kanonischen Figuren/Adventure-Referenzen verglichen. Zusätzlich wurde der Kontaktbogen im 32px-Zielmaßstab betrachtet. [Beobachtungen je Pose](qa/visual-review.json).

Sichtbefund: Mara bleibt über dunklen Bob, Weinrot/helle Bluse/Petrol und schlankere Silhouette erkennbar; Bruno über graues Haar, Schnurrbart, Ocker/helles Hemd und breiteren Körper. Rückenansichten zeigen Hinterkopf und Rücken ohne irrtümliche Frontgesichter. Rechtsprofile zeigen rechtsgerichtete Anatomie und sichtbare Oberseiten; Frontansichten behalten die erhöhte Kamera. Kontakt-/Gegenkontakt und beide Durchgänge unterscheiden sich; Armschwünge wurden gezielt nachgearbeitet. Keine sichtbaren Nachbarfigurenstücke oder Beschriftungen in den finalen Zellen.

Die Messungen belegen Raster, Alpha, Körperhöhe, Zentrierung, zusammenhängende Silhouette und unterschiedliche Pixelbilder. Sie beweisen **keine perfekte Anatomie, vollkommen konstantes wahrgenommenes Volumen oder angenehm wirkende Gehbewegung**. Kopf-/Schulterformen ändern sich perspektivbedingt zwischen den Richtungen; feinste Volumenwirkung beim Richtungswechsel sowie der vierphasige Rhythmus benötigen menschliche Betrachtung im Laufstudio. Die zyklische Wiedergabe wurde hier implementiert, aber nicht unabhängig in einem realen Browser abgespielt oder als menschlich abgenommen behauptet. Das ist keine Gesamtabnahme des Tickets.

## Anschlussarbeit des aufrufenden Agenten

1. Im Laufstudio jede Richtung und Einzelphase bei 32px und vergrößert, auf hellem/dunklem Grund beurteilen; Stand ist Kontaktphase0. Erst menschliche Freigabe bestätigt das Bewegungsgefühl.
2. Hintergrund-/Sheetdateien anschließend in die Referenzwelt integrieren. Laufpolygone an die sichtbaren Wege anpassen; die früheren Entwurfs-Overlays sind nicht endgültig. Stabile IDs und vereinbarte Anschlüsse sowie Tiefenwerte erhalten.
3. Schutz vor Überschreiben durch den alten Demo-Generator, Herkunfts-/Ticketpflege, Versionseinstufung und Spiel-/Editor-/Regressionsprüfungen im Spielprojekt übernehmen.

Für diese getrennte Grafiklieferung wurden keine Dateien im Spielprojekt geändert, kein Commit erzeugt und keine Spiel-Builds oder Spieltests ausgeführt. Bestehende Entwurfsdokumente wurden nicht zur aktuellen Produktionsabnahme umgeschrieben.
