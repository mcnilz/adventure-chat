# Tourgrafiken · Inhalt 3.0.0

Diese portable Lieferung ergänzt die unveränderte Lounge-/Garten-Art-Direction. Entwurfsrichtung am 09.11.2026 mit „passt“ bestätigt; finale menschliche Abnahme im Spiel steht aus.

- [Bewegungsvorschau](index.html): echte Sheets bei 32px und vergrößert, alle Richtungen, Stand und einzelne Laufphasen, heller/dunkler Untergrund.
- [Vorher/Nachher](comparison.html): drei Räume neben den beiden Referenzen, bei breiten Räumen linkes Ende/Mitte/rechtes Ende, alte/neue Draufsicht-Sheets.
- [Originaler Lieferbeleg](DELIVERY.md), [Zellmessungen](technical-checks.json), [Montagezuordnung](assembly-records.json), [Kontaktbögen](qa/), [Generierungsquellen](sources/).

`backgrounds/` enthält die drei freigegebenen Zielbilder, `characters/` die fertigen beiden 512×384-Sheets. Das Spiel übernimmt diese fünf PNGs bytegleich. Jede 128er-Zelle zeigt 120 sichtbare Körperpixel, y8–127, binäres Alpha und gemeinsamen Anker (64,128). Zeilen rechts/hinten/vorne, Spalte 0 Stand und Laufkontakt; links spiegelt der vorhandene Renderer.

`sources/` erhält die unveränderten Erzeugungsoriginale einschließlich verworfener/korrigierter Posen und der fünf kanonischen Konzepte. `references/` archiviert die vor Ticket 19 verwendeten Vergleichsbilder bytegleich. Produktions-/Korrekturprompts und Lieferbelege sind unverändert übernommen. Pfadangaben in diesen historischen Belegen nennen teilweise den ursprünglichen lokalen Projektordner; relative Quellen liegen nun hier neben den Belegen. Der Anwendungsbuild benötigt weder diesen Ordner auf C: noch ein Bildmodell.

Die Vorschauseite wurde für das Weltpaket auf relative lokale Bild-/Guidelinks und den inzwischen eingebundenen Stand angepasst. Das unabhängige Messwerkzeug ist als ursprünglicher Lieferbeleg archiviert; seine historischen Quellpfade beziehen sich auf das Designprojekt. Montagewerkzeuge und ein separater Vorschauprozess sind keine Projektabhängigkeit und wurden nicht in den Build aufgenommen. Wiederherstellung aus dem Projekt: `pwsh scripts/build-reference-art.ps1`; das kopiert die fertige Lieferung und rekonstruiert die bisherigen Referenzbilder. `node scripts/generate-depth-art.js` schreibt nur noch nach map/scroll.

Lieferweg: installiertes Plugin, **Local Codex**, angefordert GPT-6.1 Sol, eingebautes Bildwerkzeug. Kein OpenDesign-Cloud-Erzeugungslauf, kein BYOK und keine Veröffentlichung. Das Bildmodell wurde nicht als offline verifiziert. Keine neue Modell-, Urheber- oder Lizenzbehauptung; siehe [Bildherkunft](../../../ART-PROVENANCE.md).

Die Laufgeometrie ist eine separate Spieleinbindung: Kartenweg (140,310) → (650,90) → (1220,180); Hügelpfad (100,300) → (320,140) → (570,285); Panorama (170,290) → (225,140) → (600,325) → (990,140) → (1130,300). Format 1.5.0 bleibt erhalten; neue Navigation/Anschlusspositionen begründen Inhaltsmajor 3.0.0. Quellenmessung und automatische Browserprüfungen nehmen Stil, anatomische Feinheiten oder Bewegungsgefühl nicht menschlich ab.
