# Adventure Chat: Arbeitspakete 09–13

Stand: 01.11.2026. Nur Planung und Übergabe, keine implementierten Features.
Grundlage: Repositorystand 3d355bd65a43618264d1de4382cdc3c960b91cba und die anschließend bestätigten Anforderungen.

## Verwendung

1. Dieses ZIP direkt im Stammverzeichnis des vorhandenen Git-Repositorys entpacken. Es enthält keine zusätzliche umschließende Ordnerstufe.
2. Die fünf Tickets liegen danach im vorhandenen Markdown-Tracker unter `.scratch/adventure-chat/issues/`.
3. [Integrationsanweisung und Startprompts](docs/work-packages/world-packages-workflow.md) öffnen. Der erste Prompt lässt den Agenten die neuen Anforderungen gezielt in die bestehenden Stammdokumente übernehmen und Ticket 09 vorbereiten.
4. Vor Implementierung den tatsächlichen Status von Ticket 08 prüfen und die noch offenen Test-/Detailentscheidungen bestätigen. Die neuen Tickets bleiben bis dahin draft.

## Inhalt

| Ticket | Ergebnis |
| --- | --- |
| [09](.scratch/adventure-chat/issues/09-world-packages.md) | Externe Weltpakete, SemVer, versionierte Offline-Schemas und Kompatibilitätsprüfung |
| [10](.scratch/adventure-chat/issues/10-world-selection.md) | Weltwahl und getrennter Mehrweltbetrieb |
| [11](.scratch/adventure-chat/issues/11-map-view.md) | Begehbare Kartenansicht mit fester Figurengröße und Labyrinthprüfung |
| [12](.scratch/adventure-chat/issues/12-horizontal-scrolling.md) | Horizontal scrollende Räume, auch für Kartenansichten |
| [13](.scratch/adventure-chat/issues/13-depth-scaling.md) | Perspektivische Figurenskalierung und drei aussagekräftige Demo-Szenarien |

[Architekturvertrag zur Abwärtskompatibilität](docs/work-packages/world-format-compatibility.md)

Die Sammlung enthält Abnahmekriterien, vorgeschlagene Testgrenzen und Startprompts für tatsächlich verfügbare implement-, tdd- und code-review-Skills. Keine Skills installiert oder ausgeführt.

Das separat vorbereitete Stabilisierungsticket 08 ist nicht enthalten und bleibt erhalten. Es war im geprüften Repositorystand noch nicht vorhanden. Diese Lieferung ergänzt 09–13 und ersetzt 08 nicht.

Bestehende README.md, AGENTS.md, ARCHITECTURE.md, PLAN.md und andere Stammdokumente werden durch das ZIP nicht überschrieben. Deren gezielte Aktualisierung ist in der Integrationsanweisung konkret beschrieben. Falls gleichnamige neue Ticket- oder Übergabedateien schon existieren, vor dem Ersetzen vergleichen.

## Validierung dieser Lieferung

Fünf Ticketnummern, lokale Markdown-Verweise, Archivpfade und verlustfreies Probeentpacken wurden geprüft. Keine Software implementiert, keine Produkt-Builds oder Produkttests ausgeführt und keine Remote-Änderungen vorgenommen.

## Bestehende active-state.md aktualisieren

Die vorhandene Datei bleibt maßgeblich für den tatsächlichen Stand. Der Agent ergänzt die neue Planung gezielt, ohne Status, Abnahmen oder Testergebnisse zu erfinden. Ticket 08 bleibt erhalten. Ist es noch offen, kommt seine Bearbeitung vor 09. Tickets 09 bis 13 werden zunächst als geplante Entwürfe eingetragen. Dieses ZIP enthält bewusst keinen Ersatz der active-state.md.

Nach dem Entpacken diesen Prompt an Codex geben:

```text
Lies AGENTS.md, die aktuelle active-state.md und NEW-WORK-PACKAGES.md.
Integriere die neue Planung gemäß docs/work-packages/world-packages-workflow.md
in die vorhandenen Projektunterlagen. Aktualisiere insbesondere active-state.md
und PLAN.md anhand des tatsächlichen Ticketstands. Bewahre den Status von
Ticket 08 und bereits abgeschlossene Arbeiten; ergänze Tickets 09 bis 13 als
geplante Entwürfe. Prüfe Blocker und schlage den nächsten erlaubten Schritt vor.
Implementiere noch keine Features. Kein Push, kein Deployment.
```
