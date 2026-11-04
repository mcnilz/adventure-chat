# 14: Weltbezogene Figurenwahl

**What to build:** Eine Welt definiert ihre verfügbaren Figurengrafiken. Besucher wählen vor dem Eintritt eine Figur; alle Teilnehmer sehen deren gewählte Grafik beim Stehen und Laufen.

**Blocked by:** 09, 10, 11 (done)

**Status:** draft

Am 04.11.2026 zur Planung beauftragt. Keine Implementierungsfreigabe; Details und Testgrenzen vor Beginn bestätigen. Die Tickets 14–16 werden von anderen Agenten umgesetzt, nicht im anlegenden Chat.

## Ausgangslage und Fachkontext

- `worlds/reference/design-guide/index.html` beschreibt die neue Art Direction. Unter `worlds/reference/design-guide/assets/characters/` liegen Mara und Bruno mit 64 × 96 Pixeln pro Frame, vier Spalten und drei Reihen; zusätzlich Legacy-Varianten mit 48 × 64 Pixeln. Animationsqualität ist noch nicht abgenommen.
- `src/AdventureChat.Client/src/rendering/character-renderer.ts` verwendet bisher eine globale Datei `/app-assets/character.png`, feste 48 × 64-Frames und die separate Raumhöhe `actorHeight`.
- Relevante Verträge: `CONTENT-FORMAT.md`, `docs/work-packages/world-format-compatibility.md`, Ticket 10/11, `TESTING.md`, ADR 0001 und `GLOSSARY.md`.

## Acceptance criteria

- [ ] Optionaler weltbezogener Figurenkatalog enthält stabile IDs, Anzeigenamen, relative PNG-Ressourcenpfade und benötigte Frameangaben. Konkrete YAML-Struktur, Grenzen und Defaultfigur vor Beginn festlegen. Das bestehende Richtungslayout (rechts, hinten, vorne; links gespiegelt) genügt; kein beliebiges Animationssystem.
- [ ] Auswahl mit visueller Vorschau vor dem Weltbeitritt. Vorschlag: mehrere Teilnehmer dürfen dieselbe Figur wählen; Auswahl bleibt über Raumwechsel und Reconnect erhalten, bei Weltwechsel wird neu gewählt. Fehlende Welt oder nicht mehr verfügbare Auswahl führt verständlich zur erneuten Auswahl. Regeln vor Beginn bestätigen.
- [ ] Server akzeptiert ausschließlich IDs aus dem Katalog der gewählten Welt; keine vom Teilnehmer gelieferten Bildpfade oder Frameangaben. Auswahl ist an die Sitzung gebunden und wird in Snapshots und Eintrittsereignissen konsistent übertragen. Manipulierte oder weltfremde IDs werden abgewiesen.
- [ ] Zwei Welten mit gleichen Figuren-IDs und Ressourcenpfaden dürfen unterschiedliche Grafiken besitzen. Bildauslieferung und Caches bleiben weltgebunden; bestehende Pfad-, PNG- und Dateisystemschutzregeln gelten auch hier.
- [ ] Renderer unterstützt mindestens 48 × 64 und 64 × 96 als Quellraster. Zielhöhe bleibt die Raum-Figurengröße, unabhängig von der Quellauflösung. Fußpunkt, Namensschild und Sprechblase stimmen mit der Zielhöhe überein. Stand, vier Laufphasen und alle Blickrichtungen funktionieren für lokale und entfernte Figuren.
- [ ] Zusammenspiel mit Draufsicht ausdrücklich festlegen: Vorschlag ist unveränderte bisherige Draufsichtdarstellung für Welten ohne Figurenkatalog; Katalogverhalten in Draufsichträumen vor Beginn entscheiden und dokumentieren. Keine stillschweigend wirkungslose Auswahl anbieten.
- [ ] Ohne Katalog bleibt das zugesicherte bisherige Figurenverhalten älterer Welten erhalten. Editor-Vorschau nutzt dieselbe Figurenauflösung und Darstellung; keine zweite Renderingimplementierung. Eine grafische Verwaltung des Weltkatalogs im Editor ist nicht Bestandteil dieses Tickets.
- [ ] Fehlende/ungültige PNGs, doppelte IDs, ungeeignete Bildabmessungen und ungültige Frameangaben werden an der Inhaltsgrenze verständlich zurückgewiesen. Vorübergehender Bildladefehler im Browser verursacht keinen Absturz.
- [ ] Neue optionale Formatfelder erhalten begründete Minor-Einstufung und ein neues unveränderliches Schema-Bündel. Konkrete Versionsnummer anhand des tatsächlichen Implementierungsstands wählen und mit Ticket 13/15 koordinieren; keine zukünftige Versionsnummer voraussetzen. Server unterstützt nur explizit aufgeführte Versionen; alte Bündel und Fixtures unverändert lassen.
- [ ] Autorenformat, Versionsmatrix, Architektur, Glossar bei neuen Begriffen und Testbelege aktualisieren. Ergebnis auf ready-for-human setzen, konkrete manuelle Schritte dokumentieren; done erst nach ausdrücklicher Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Weltlade-/Inhaltsgrenze, Sitzungs-/Hubgrenze und Browser-/Canvasdarstellung. Gültiger Katalog sowie ungültige IDs, Pfade, PNGs und Framegrößen; zwei Welten mit gleichen IDs; zwei Browser mit verschiedenen Figuren; Eintritt, Raumwechsel, Reconnect und Weltwechsel; Größen-/Fußpunktdarstellung für beide Raster; bestehende Draufsicht und unveränderte ältere Fixtures. Gemeinsame Editor-Vorschau prüfen. Keine privaten Rendererfelder testen.

Automatisierung ersetzt keine Sichtprüfung von Silhouetten, angeschnittenen Frames, Laufkontinuität und Lesbarkeit. Bildkorrekturen der gelieferten Referenzfiguren gehören zur Inhaltsintegration in Ticket 16.

## Grenzen und Übergabe

Keine Accounts, Reservierung einer Figur, Persistenz über Browserneustarts, Kleidungsanpassung oder Wechsel während der laufenden Sitzung erforderlich. Keine neue Abhängigkeit ohne Projektprüfung. Ticket 16 integriert erst danach die echten Figuren in die Referenzwelt; dieses Ticket verwendet eigene neue Testfixtures und verändert die bestehende Referenzwelt nicht nebenbei.
