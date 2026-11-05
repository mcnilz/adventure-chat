# Bildherkunft und Bearbeitungen

Die Raumhintergründe, Mara/Bruno und Legacy-Sheets wurden mit dem vorhandenen Design Guide vom Benutzer geliefert. Der Guide bezeichnet die Motive als generiert. Genaues Erzeugungsmodell, ursprüngliche Prompts, Urheber und weitergehende Nutzungsrechtsbelege liegen hier nicht vor. Dieses Projekt vergibt dafür keine erfundene Lizenz. Vor einer Weiterveröffentlichung sind die Rechte separat zu klären.

Ticket 16 erhält sämtliche gelieferten Originale bytegetreu. Die Spielbilder unter `assets/images/` entstehen lokal mit `scripts/build-reference-art.ps1`:

- `*-runtime.png`: proportionale Abtastung der 1280 × 720-Vorlagen auf 640 × 360, ohne Glättung. Der bestehende Autorenvertrag verlangt zur Raumgröße passende PNG-Abmessungen.
- `*-table.png`: nachgezogene Silhouetten und transparente Zwischenräume, mit unveränderten RGB-Pixeln aus den Originalräumen. Die Masken werden mit derselben Abtastung wie der Hintergrund verarbeitet. Ein ImageGen-Freistellungsversuch vom 05.11.2026 wich geometrisch ab und wurde nicht ins Weltpaket übernommen.
- `mara.png`, `bruno.png`: aus den gelieferten 256 × 288-Sheets; pro Frame einheitliche sichtbare Höhe 72 Quellpixel und Fußanker unten mittig. Das Spielraster ist 64 × 72 (Sheet 256 × 216); die 24 transparenten Kopfzeilen der ersten Spielkopien entfallen. Die gelieferten Originale mit 64 × 96 bleiben unverändert. Raumhöhe 128 ergibt nun 128 sichtbare Körperpixel statt bisher 72. Insbesondere Maras erste Rückenpose war zuvor deutlich höher. Kein Neuzeichnen der Anatomie.

Die Charakterisierung und Gestaltungsleitlinien im Design Guide bleiben erhalten. Seine Integrationsabschnitte wurden auf tatsächliche Dateipfade und Prüfungen aktualisiert. Eine technische Prüfung ersetzt keine gestalterische oder rechtliche Freigabe.

Formatversion 1.4.0 ist wegen Figurenkatalog und Raumobjekten erforderlich und bereits ausdrücklich unterstützt. Inhaltsversion 2.0.0 statt 1.0.1: Art Direction und navigierbare Raumgeometrie wurden grundlegend ersetzt; veröffentlichte Platzierungen sind nicht mehr unverändert zugesichert. Nach Benutzerfeedback zur Relation Figur/Möbel korrigiert 2.0.1 die Figurengröße; reine visuelle Patchkorrektur ohne geänderte Navigation. Keine neuen Schemafelder und keine Änderung der Interpretation des Autorenformats. Alte Schemas und `tests/Fixtures/reference-v1` aus b3d0b83 bleiben unverändert.
