# Bildherkunft und Bearbeitungen

Die Raumhintergründe, Mara/Bruno und Legacy-Sheets wurden mit dem vorhandenen Design Guide vom Benutzer geliefert. Der Guide bezeichnet die Motive als generiert. Genaues Erzeugungsmodell, ursprüngliche Prompts, Urheber und weitergehende Nutzungsrechtsbelege liegen hier nicht vor. Dieses Projekt vergibt dafür keine erfundene Lizenz. Vor einer Weiterveröffentlichung sind die Rechte separat zu klären.

Ticket 16 erhält sämtliche gelieferten Originale bytegetreu. Die Spielbilder unter `assets/images/` entstehen lokal mit `scripts/build-reference-art.ps1`:

- `*-runtime.png`: proportionale Abtastung der 1280 × 720-Vorlagen auf 640 × 360, ohne Glättung. Der bestehende Autorenvertrag verlangt zur Raumgröße passende PNG-Abmessungen.
- `*-table.png`: nachgezogene Silhouetten und transparente Zwischenräume, mit unveränderten RGB-Pixeln aus den Originalräumen. Die Masken werden mit derselben Abtastung wie der Hintergrund verarbeitet. Ein ImageGen-Freistellungsversuch vom 05.11.2026 wich geometrisch ab und wurde nicht ins Weltpaket übernommen.
- `mara.png`, `bruno.png`: bytegleiche Kopien der neuen Lieferung unter `design-guide/assets/characters/native128/`. Sheets 512 × 384 mit 128 × 128-Frames und 125 sichtbaren Körperpixeln, Fußanker unten mittig. Bei Raumhöhe 128 zeichnet der Renderer ohne Größenänderung. Ältere 64 × 96-Figurenoriginale bleiben erhalten. Die vorherige Vergrößerung der 64 × 72-Spielraster entfällt.

Die Charakterisierung und Gestaltungsleitlinien im Design Guide bleiben erhalten. Seine Integrationsabschnitte wurden auf tatsächliche Dateipfade und Prüfungen aktualisiert. Eine technische Prüfung ersetzt keine gestalterische oder rechtliche Freigabe.

Formatversion 1.4.0 ist wegen Figurenkatalog und Raumobjekten erforderlich und bereits ausdrücklich unterstützt. Inhaltsversion 2.0.0 statt 1.0.1: Art Direction und navigierbare Raumgeometrie wurden grundlegend ersetzt; veröffentlichte Platzierungen sind nicht mehr unverändert zugesichert. Nach Benutzerfeedback zur Relation Figur/Möbel korrigiert 2.0.1 die Figurengröße; reine visuelle Patchkorrektur ohne geänderte Navigation. Keine neuen Schemafelder und keine Änderung der Interpretation des Autorenformats. Alte Schemas und `tests/Fixtures/reference-v1` aus b3d0b83 bleiben unverändert.

Inhaltsversion 2.0.2 ersetzt die hochskalierten Spielgrafiken durch die native128-Lieferung aus Open Design; visuelle Patchkorrektur bei unveränderter Navigation, IDs und Formatinterpretation. Laut Lieferbeleg wurden die Figuren generativ neu gezeichnet und anschließend in ein gemeinsames Raster mit binärem Alpha und reduzierter Palette gebracht. Herkunft, Quellen und Prüfgrenzen sind im unverändert erhaltenen [Lieferbeleg](design-guide/assets/characters/native128/HERKUNFT-UND-PRUEFUNG.md) dokumentiert; daraus folgt keine neue Lizenzbehauptung.

Das gelieferte Python/Pillow-Werkzeug bleibt bewusst lokal und unversioniert als Archiv der Grafikbearbeitung; es wird durch den Anwendungsbuild weder aufgerufen noch benötigt. Es wurde für die Integration nicht ausgeführt und nicht als Projektwerkzeug aufgenommen. Der unveränderte Lieferbeleg beschreibt die ursprüngliche lokale Lieferung einschließlich dieses Werkzeugs. Ein sauberer Checkout reproduziert die Spielgrafiken mit dem bestehenden PowerShell-Bildbuild aus den versionierten fertigen PNG-Sheets, ohne Python/Pillow; keine neuen Projektabhängigkeiten. Profil-Gegenphasen sind laut Lieferung subtil; Bewegungsgefühl und menschliche Sichtabnahme bleiben offen.

## Demo-Ergänzungen Ticket 13 · 09.11.2026
Die damaligen drei demo-*.png-Hintergründe und mara-top-down.png/bruno-top-down.png waren originale schematische Pixelzeichnungen aus scripts/generate-depth-art.js, ohne externe Quellen. Ticket 19 ersetzt ausschließlich diese fünf Referenzgrafiken. Andere Welten und die gelieferten Adventure-Originale bleiben unverändert.

## Lokale Lieferung Ticket 19 · 09.11.2026

Die Gestaltung entstand über das installierte Plugin mit **Local Codex**, angefordertem Modell GPT-6.1 Sol und vorhandener ChatGPT-Anmeldung. Kein OpenDesign-Cloud-Erzeugungslauf und kein BYOK. Der Ablauf nutzte das eingebaute Bildwerkzeug; das ist keine Zusicherung eines vollständig offline laufenden Bildmodells. Exaktes Bildmodell, Urheber und zusätzliche Lizenzbelege wurden nicht geliefert und werden nicht erfunden. Der Benutzer bestätigte die Entwurfsrichtung mit „passt“; die endgültige Spielabnahme bleibt separat.

Portable Lieferung: [tour-v3](design-guide/assets/tour-v3/README.md). Die drei freigegebenen Hintergrund-Zielfassungen wurden bytegleich übernommen. Ihre breiteren Generierungsoriginale bleiben gesondert erhalten; proportionaler Beschnitt auf 1280×360 bzw. 640×360, keine Stauchung. Beide Draufsichtfiguren wurden anhand der kanonischen Entwürfe neu in Richtungen/Posen erzeugt und durch gezielte Posekorrekturen ergänzt. Die Rastermontage beschneidet und reinigt Alpha, skaliert proportional ohne Glättung und montiert Zellen. Sie erfindet keine fehlenden Posen.

Die finalen Sheets sind 512×384, vier Spalten/drei Reihen rechts/hinten/vorne, links im bestehenden Renderer gespiegelt. Jede Zelle enthält 120 sichtbare Körperpixel (y8–127), binäres Alpha und Fußanker (64,128). Bei 32px-Rahmen bleiben 30 sichtbare Pixel. Generierungsquellen, Prompts, Montagezuordnung, technische Messungen und Kontaktbögen sind getrennt archiviert. Technische Messungen nehmen weder Anatomie noch Bewegungsgefühl oder Rechte ab.

`scripts/build-reference-art.ps1` kopiert die fünf fertigen Tourbilder bytegleich ins Spiel und rekonstruiert weiter die unveränderten früheren Referenzbilder. Kein Generator oder zusätzliche Bibliothek ist beim Anwendungsbuild erforderlich. `generate-depth-art.js` schreibt ausschließlich nach map/scroll. Keine neue direkte Abhängigkeit.

Inhalt **3.0.0** statt 2.1.0: Neue Laufpolygone/Hindernisse und an die sichtbaren Wege verschobene Spawns/Ausgänge machen frühere Platzierungen teilweise ungültig. Stabile IDs und Verbindungen, Perspektiven, Raum-/Ausschnittgrößen, Höhen, Tiefenfaktoren und Geschwindigkeiten bleiben erhalten. Format **1.5.0**, dessen Interpretation und alle veröffentlichten Schemas bleiben unverändert. Unveränderte ältere Fixtures prüfen die Abwärtskompatibilität; dies ist eine neue Inhaltsausgabe, kein neues Autorenformat.
