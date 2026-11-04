# 14: Weltbezogene Figurenwahl

**What to build:** Eine Welt definiert ihre verfügbaren Figurengrafiken. Besucher wählen vor dem Eintritt eine Figur; alle Teilnehmer sehen deren gewählte Grafik beim Stehen und Laufen.

**Blocked by:** 09, 10, 11 (done)

**Status:** ready-for-human

Am 04.11.2026 in diesem Implementierungschat ausdrücklich bestätigt: „Sehr gut. Wenn man schon bei der Auswahl ist, sollte man auch den Namen eingeben können. So umsetzen“. Bestätigte Grenzen: öffentliche Inhalts-, Hub-/Sitzungs- und Browser-/Canvasgrenzen inklusive Editor. Name wird gemeinsam mit Welt und Figur vor Eintritt eingegeben (24 Zeichen, vorbelegter editierbarer Gastname). Mehrfachwahl erlaubt; Auswahl bleibt bei Raumwechsel und Reconnect, Weltwechsel erneuert sie. Katalogfiguren gelten auch in Draufsicht; ohne Katalog unveränderte Darstellung. Defaultfigur explizit, maximal 32 Einträge, IDs bis 64 Zeichen, Namen bis 100 Zeichen, Framebreite/-höhe 1–256 ganzzahlige Pixel, fest vier Spalten und drei Reihen. Format 1.3.0 ist additive Minor-Erweiterung. Kein Wechsel in laufender Sitzung, keine neuen Abhängigkeiten.

## Ausgangslage und Fachkontext

- `worlds/reference/design-guide/index.html` beschreibt die neue Art Direction. Unter `worlds/reference/design-guide/assets/characters/` liegen Mara und Bruno mit 64 × 96 Pixeln pro Frame, vier Spalten und drei Reihen; zusätzlich Legacy-Varianten mit 48 × 64 Pixeln. Animationsqualität ist noch nicht abgenommen.
- `src/AdventureChat.Client/src/rendering/character-renderer.ts` verwendet bisher eine globale Datei `/app-assets/character.png`, feste 48 × 64-Frames und die separate Raumhöhe `actorHeight`.
- Relevante Verträge: `CONTENT-FORMAT.md`, `docs/work-packages/world-format-compatibility.md`, Ticket 10/11, `TESTING.md`, ADR 0001 und `GLOSSARY.md`.

## Acceptance criteria

- [x] Optionaler weltbezogener Figurenkatalog enthält stabile IDs, Anzeigenamen, relative PNG-Ressourcenpfade und benötigte Frameangaben. Konkrete YAML-Struktur, Grenzen und Defaultfigur vor Beginn festlegen. Das bestehende Richtungslayout (rechts, hinten, vorne; links gespiegelt) genügt; kein beliebiges Animationssystem.
- [x] Auswahl mit visueller Vorschau vor dem Weltbeitritt. Vorschlag: mehrere Teilnehmer dürfen dieselbe Figur wählen; Auswahl bleibt über Raumwechsel und Reconnect erhalten, bei Weltwechsel wird neu gewählt. Fehlende Welt oder nicht mehr verfügbare Auswahl führt verständlich zur erneuten Auswahl. Regeln vor Beginn bestätigen.
- [x] Server akzeptiert ausschließlich IDs aus dem Katalog der gewählten Welt; keine vom Teilnehmer gelieferten Bildpfade oder Frameangaben. Auswahl ist an die Sitzung gebunden und wird in Snapshots und Eintrittsereignissen konsistent übertragen. Manipulierte oder weltfremde IDs werden abgewiesen.
- [x] Zwei Welten mit gleichen Figuren-IDs und Ressourcenpfaden dürfen unterschiedliche Grafiken besitzen. Bildauslieferung und Caches bleiben weltgebunden; bestehende Pfad-, PNG- und Dateisystemschutzregeln gelten auch hier.
- [x] Renderer unterstützt mindestens 48 × 64 und 64 × 96 als Quellraster. Zielhöhe bleibt die Raum-Figurengröße, unabhängig von der Quellauflösung. Fußpunkt, Namensschild und Sprechblase stimmen mit der Zielhöhe überein. Stand, vier Laufphasen und alle Blickrichtungen funktionieren für lokale und entfernte Figuren.
- [x] Zusammenspiel mit Draufsicht ausdrücklich festlegen: Vorschlag ist unveränderte bisherige Draufsichtdarstellung für Welten ohne Figurenkatalog; Katalogverhalten in Draufsichträumen vor Beginn entscheiden und dokumentieren. Keine stillschweigend wirkungslose Auswahl anbieten.
- [x] Ohne Katalog bleibt das zugesicherte bisherige Figurenverhalten älterer Welten erhalten. Editor-Vorschau nutzt dieselbe Figurenauflösung und Darstellung; keine zweite Renderingimplementierung. Eine grafische Verwaltung des Weltkatalogs im Editor ist nicht Bestandteil dieses Tickets.
- [x] Fehlende/ungültige PNGs, doppelte IDs, ungeeignete Bildabmessungen und ungültige Frameangaben werden an der Inhaltsgrenze verständlich zurückgewiesen. Vorübergehender Bildladefehler im Browser verursacht keinen Absturz.
- [x] Neue optionale Formatfelder erhalten begründete Minor-Einstufung und ein neues unveränderliches Schema-Bündel. Konkrete Versionsnummer anhand des tatsächlichen Implementierungsstands wählen und mit Ticket 13/15 koordinieren; keine zukünftige Versionsnummer voraussetzen. Server unterstützt nur explizit aufgeführte Versionen; alte Bündel und Fixtures unverändert lassen.
- [x] Autorenformat, Versionsmatrix, Architektur, Glossar bei neuen Begriffen und Testbelege aktualisieren. Ergebnis auf ready-for-human setzen, konkrete manuelle Schritte dokumentieren; done erst nach ausdrücklicher Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Weltlade-/Inhaltsgrenze, Sitzungs-/Hubgrenze und Browser-/Canvasdarstellung. Gültiger Katalog sowie ungültige IDs, Pfade, PNGs und Framegrößen; zwei Welten mit gleichen IDs; zwei Browser mit verschiedenen Figuren; Eintritt, Raumwechsel, Reconnect und Weltwechsel; Größen-/Fußpunktdarstellung für beide Raster; bestehende Draufsicht und unveränderte ältere Fixtures. Gemeinsame Editor-Vorschau prüfen. Keine privaten Rendererfelder testen.

Automatisierung ersetzt keine Sichtprüfung von Silhouetten, angeschnittenen Frames, Laufkontinuität und Lesbarkeit. Bildkorrekturen der gelieferten Referenzfiguren gehören zur Inhaltsintegration in Ticket 16.

## Grenzen und Übergabe

Keine Accounts, Reservierung einer Figur, Persistenz über Browserneustarts, Kleidungsanpassung oder Wechsel während der laufenden Sitzung erforderlich. Keine neue Abhängigkeit ohne Projektprüfung. Ticket 16 integriert erst danach die echten Figuren in die Referenzwelt; dieses Ticket verwendet eigene neue Testfixtures und verändert die bestehende Referenzwelt nicht nebenbei.

## Umsetzung und Belege · 04.11.2026

- Bestätigter Umfang umgesetzt, einschließlich Name in der bestehenden Weltwahl (ohne Weltpakete bleibt der bisherige unmittelbare Legacy-Eintritt). Katalog-API, Sitzung und Ereignisse führen weltbezogene IDs; Renderer und Editor teilen dieselbe Darstellung. Keine neuen Abhängigkeiten.
- TDD-Nachweis an öffentlicher Inhaltsgrenze: `ValidCatalog_LoadsAndKeepsManifestStable` zunächst rot wegen nicht unterstützter Version 1.3.0, danach grün. Weitere Inhaltsfälle prüfen alte Versionen, Duplicate-ID, Default, Pfad, fehlendes/kaputtes PNG, Raster und Manifeständerung; 14 gezielte Fälle grün.
- Sieben neue Browsertests grün: Namenseingabe, zwei Teilnehmer mit unterschiedlichen Figuren, Raum-/Weltwechsel, Weltdefault und manipulierte ID, beide Raster und alle Richtungen/Stand/Laufphasen mit Pixelprüfung, echter Reconnect, gemeinsame Editor-Vorschau und Bildladefehler ohne Absturz. Keine privaten Rendererfelder getestet.
- `npm --prefix src/AdventureChat.Client run typecheck` und `run build` erfolgreich. Abschließendes `dotnet test --no-restore`: **213 bestanden, 0 Warnungen, 2 Projekte**, 141,6 Sekunden. Kein zusätzlicher Restore nötig; Lockfiles und Abhängigkeiten unverändert.
- Anfangs blockierten nicht passende neue Referenzbilder die vorhandenen Weltprüfungen. Deshalb letzte alte Referenzwelt aus b3d0b83 nach `tests/Fixtures/reference-v1` gesichert und Regressionstests darauf umgestellt. Alle fünf Dateien unabhängig gegen Git/LFS-Basis bytegetreu nachgeprüft; neue Inhalte unter `worlds/reference` unverändert. Die neuen synthetischen Figuren-Testwelten verändern keine alte Fixture.
- Ein eingeschränkter Restoreversuch scheiterte am vorhandenen NuGet-Lockzugriff; weitere Läufe mit vorhandenen Restore-Artefakten erfolgreich. Chromium benötigte Ausführungsrechte (EPERM im eingeschränkten Lauf); gezielte und vollständige abschließende Läufe mit diesen Rechten bestanden.
- Code-Review mit zwei getrennten Subagenten gegen Basis `7347fc5ad4eacc2e9210fc15c7a7564ad6fae913` einschließlich Arbeitsbaum und neuer Dateien: Standards **0 offene Befunde**, Spec **0 offene Befunde**. Screenshots von Auswahl und Spiel visuell geprüft; keine Layoutprobleme mit Name und Figurenhöhe. Diffprüfung erfolgreich.
- SemVer: 1.3.0 Minor, da nur optionale Manifestfelder und kompatible Defaults. Alte Schemas/Teilschemas, `content/rooms`, `worlds/map` und `worlds/scroll` unverändert. Das neue vollständige Bündel liegt separat unter `schemas/world/1.3.0`.

## Manuelle Abnahme

1. Lokal mit einer Prüfwelt starten:
   `dotnet run --project src/AdventureChat.Server -- --urls http://127.0.0.1:5084 --World:Root D:/adventure-chat/tests/Fixtures/characters-a --authoring`
2. `http://127.0.0.1:5084` in zwei unabhängigen Browserfenstern öffnen. Namen eingeben, einmal Small Hero und einmal Tall Hero wählen; Vorschau und Beschriftung prüfen, beide eintreten lassen.
3. Laufen und chatten; beide Namen und gewählten Grafiken müssen bei beiden Teilnehmern stimmen. Die farbigen Prüfblöcke codieren Richtungen/Frames; sie sind bewusst keine fertige Grafik. Fußpunkt, Namensschild und Sprechblase müssen beim Laufen stimmen. Der Ausgang liegt am Startpunkt (100,300) und wechselt in `next`.
4. Eine Verbindung kurz unterbrechen und wiederherstellen: gleicher Name/gleiche Figur, neue Sitzung am Start. Zur Weltwahl zurückgehen: Auswahl wird neu vorbelegt. Für echte zweite Welt zusätzlich `--World:Roots:0 D:/adventure-chat/tests/Fixtures/characters-b` beim Start angeben und zwischen beiden wechseln.
5. `http://127.0.0.1:5084/editor.html?worldId=characters-a` öffnen, Raum `hall` wählen und Vorschau aktivieren: Defaultfigur Small Hero und Bewegung prüfen.

Grenzen: Sichtprüfung/Politur der gelieferten Mara-/Bruno-Grafiken und die Reparatur/Integration der neuen Referenzhintergründe gehören zu Ticket 16. `worlds/reference` ist bis dahin wegen des bereits vorhandenen Bild-/Raumgrößenunterschieds nicht als Laufzeit-Abnahme geeignet. Keine neue Figurenverwaltung im Editor. Menschliche Abnahme steht aus; die gesetzten Kriterien markieren die nachgewiesene Umsetzung und ersetzen kein `done`.

Lokaler Implementierungscommit: Commit zu diesem Übergabestand auf dem aktuellen Branch (über `git log` auffindbar); kein Push.
