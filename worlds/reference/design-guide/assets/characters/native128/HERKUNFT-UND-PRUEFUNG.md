# Native128 – Mara und Bruno

## Lieferung
- mara.png und bruno.png: transparente RGBA-Sheets, 512 × 384 Pixel, 4 × 3 Frames à 128 × 128.
- kontaktbogen.png: beide Figuren, alle 24 Frames, Raster und gelbe Fußanker.
- mara-vier-richtungen.gif und bruno-vier-richtungen.gif: rechts/hinten/vorne/links nebeneinander, zyklisch Spalten 0–3, 160 ms je Phase. Links ist exakt die gespiegelte Rechtsansicht. Die Vorschauen zeigen 128-Pixel-Frames ohne Vergrößerung.
- previews/: acht PNGs à 640 × 360: je Raum und Figur vor/hinter dem Tisch, Frontansicht Spalte 0.
- sources/: unveränderte neue Generierungsergebnisse; build_native128.py: Verarbeitung und Vorschaukomposition; technical-checks.json: technische Messwerte.

## Vertrag und Einbaugrenze
Reihen: rechts, hinten, vorne. Spalte 0 ist Stand und erste Laufphase. Lokaler Fußanker (64,128), sichtbare Höhe 125 Pixel, drei Pixel oberer Rand. Keine zusätzlichen Richtungen oder Metadaten im Autorenformat erforderlich. Die Messdatei ist nur ein Prüfbeleg und keine Runtime-Abhängigkeit. Die Einbindung übernimmt der Coding-Agent. Originale, Spielkopien, Renderer und Raumdefinitionen wurden nicht ersetzt.

## Herkunft und Verarbeitung
Benutzervorlagen: worlds/reference/design-guide/assets/characters/mara.png und bruno.png; Raumstil aus assets/rooms/lounge.png und garden.png; Art Direction aus design-guide/index.html; Größenvergleich aus .scratch/adventure-chat/evidence/16/reference-lounge-front-bruno.png. Zu den Vorlagen liegen keine vollständigen Urheber- oder Lizenzbelege vor; es wird keine Lizenz zugewiesen.

Die Figuren wurden in dieser Sitzung mit dem integrierten Bildgenerierungswerkzeug neu erzeugt, nicht aus den alten Figuren hochskaliert. Die genaue Modellversion wird vom Werkzeug nicht ausgewiesen. Promptinhalt: eigenständige Neuzeichnung anhand der Charakter- und Raumreferenzen, schlanke Adventure-Proportionen, 4 × 3 vollständige Laufposen, gleiche Identität und Größe, native Zielhöhe 125 Pixel, drei Richtungen, transparenter Hintergrund, keine Rasterlinien oder Beschriftungen. Mara: dunkler Bob, Weinrot/Bluse/Petrol. Bruno: graues Haar, Nase/Schnurrbart, Ockerweste/helles Hemd. Originale der neuen Generierungen sind im Unterordner sources erhalten.

Die neuen Generierungen wurden pro Rasterzelle auf ihren Alphainhalt zugeschnitten und per nächstem Nachbarn auf 125 Pixel Körperhöhe gebracht, auf die gemeinsame Unterkante gesetzt und in das endgültige Raster montiert. Dies ist eine Rasteraufbereitung der Neuzeichnungen, keine Vergrößerung der Altfiguren. Alpha-Schwelle 128, eine gemeinsame Palette mit bis zu 40 Farben pro Figur, kein Dithering, keine Glättung oder Schärfung. Die Generierungsauflösung und Zellbegrenzungen stehen in technical-checks.json.

Raumkomposition: unveränderte 640 × 360-Runtime-Hintergründe und vorhandene Tischmasken aus worlds/reference/assets/images. Fußpunkte Lounge hinten (330,258), Garten hinten (330,278), vorne jeweils (330,330). Hinter dem Tisch wird die Original-Tischmaske nach der Figur gezeichnet, davor die Figur nach dem Hintergrund. Keine perspektivische Figurenskalierung. Die Referenzgrößenrelation bleibt erhalten; die explizit gewünschten 125 sichtbaren Pixel sind drei Pixel kleiner als die bisher 128 sichtbaren Körperpixel.

## Prüfungen und Grenzen
Automatisch gemessen: Sheetformat, RGBA, zwölf nichtleere Zellen, sichtbare Höhe, Fußunterkante, Seitenränder und binäres Alpha. Bewegungsgefühl, Gesichtsidentität und Pixelstil lassen sich dadurch nicht abnehmen. Visuelle Befunde werden separat festgehalten; menschliche Sichtabnahme bleibt offen. GIF-Vorschauen sind Darstellungsdateien, keine zusätzlichen Autorenformat-Vorgaben.

## Wiederherstellung
build_native128.py mit den beiden Pfaden zu sources/mara-generated.png und sources/bruno-generated.png ausführen. Benötigt Python 3.12 und Pillow 12.0.0; Pillow ist ausschließlich ein lokales Bildwerkzeug (Pillow-Lizenz), keine Spielabhängigkeit. Es wurden keine Projektabhängigkeiten oder Lockfiles verändert. Die Datei enthält die lokalen Quell- und Zielpfade dieses Handoffs.

## Durchgeführte Sichtprüfung
Alle 24 Rasterzellen und alle acht finalen Raumansichten wurden visuell betrachtet. Größenrelation, schlanke Silhouetten und Tischverdeckung sind in diesen Standbildern nachvollziehbar; die Figuren werden in den Raumvorschauen 1:1 gezeichnet. Keine abgeschnittenen Gliedmaßen oder Nachbarframe-Reste erkennbar. Die erste Generierung wiederholte einige Gegenphasen zu stark. Eine gezielte Bildkorrektur pro Figur hat die gegenüberliegenden Beinphasen überarbeitet, besonders in Front/Rücken. Diese Erststände bleiben unter sources/first-pass erhalten. Die Korrekturprompts verlangten explizit gegenüberliegende anatomische Beine in Spalten 0/2 und 1/3 bei unveränderter Figur und festem Fußpunkt.

Die GIF-Dateien wurden auf vier Frames mit je 160 ms geprüft; die opaken Figurenpixel der Linksansicht entsprechen exakt den gespiegelten Rechtsframes. Die Phasenfolge wurde anhand des Kontaktbogens beurteilt; eine Echtzeit-Wiedergabe im Spiel fand nicht statt. Vierphasiger Gang und ruhige Körpervolumina sind angelegt; die Profil-Gegenphasen bleiben in der kleinen Darstellung subtil. Natürlichkeit des Gangs, Standwirkung von Spalte 0, feine Gesichtskonsistenz und endgültige stilistische Einbindung bleiben menschliche Sichtabnahme. Keine technische Spieleinbindung und keine Freigabe behauptet.
