# 02: Ein Hindernis mit servergeprüfter Bewegung umgehen

**What to build:** In einem nicht rechteckigen Beispielraum erreicht meine Figur Ziele über erlaubte Wege. Der Server nimmt Zielkommandos an und entscheidet den gültigen Weg.

**Blocked by:** 01

**Status:** ready-for-human

## Acceptance criteria

- [x] Ein Ziel hinter einem ausgesparten Hindernis wird über einen begehbaren Umweg erreicht.
- [x] Ein unzugängliches Ziel wird abgelehnt, ohne Figur zu teleportieren oder den erlaubten Bereich zu verlassen.
- [x] Neues Ziel während Bewegung ersetzt den Weg ohne ungültigen Sprung.
- [x] Ungültige oder nicht endliche Koordinaten werden am Server zurückgewiesen.
- [x] Gleiche Ausgangslage und Ziele erzeugen reproduzierbare fachliche Ergebnisse.
- [x] Fehlende/ungültige Raumgeometrie wird verständlich gemeldet.

## Testgrenzen – vor Beginn bestätigen
Öffentliche Raumkommandos plus ein Browserablauf; fachliche Route-Grenze nur falls zusätzlich bestätigt.

## Hinweise
Vor Beginn Polygonmodell, Randfälle und Toleranzen festlegen. Serverautorität und feste IDs bereits hier vorsehen.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
- **Umsetzung:**
  - `RoomGeometry` mit Außenpolygon (`OuterBoundary`) und Hindernispolygonen (`Obstacles`) für die Taverne Lounge definiert.
  - `PathfindingService` in C# mit Sichtbarkeitsgraphen (`Visibility Graph`) und euklidischem A*-Algorithmus inklusive Pfadglättung (`String Pulling`) implementiert.
  - Endpunkte `GET /api/rooms/{id}` und `POST /api/rooms/{id}/path` mit Validierung nicht-endlicher Koordinaten (`NaN`, `Infinity`), Raumexistenz und Begehbarkeitsprüfung realisiert.
  - TypeScript-Client (`AdventureGame`, `math.ts`) an die servervalidierte Wegfindung angebunden mit unterbrechungsfreier Zielablösung während des Laufens.
- **Checks:**
  - `npm run typecheck` (`tsc --noEmit`): 0 Fehler.
  - `dotnet build`: 0 Warnungen, 0 Fehler.
  - `AdventureChat.ServerTests`: 15 NUnit-Tests erfolgreich (Geometrie, Kollision, Visibility Graph, A*, API-Endpunkte).
  - `AdventureChat.E2ETests`: 8 Playwright-E2E-Tests erfolgreich (Hindernisumrundung, Ablehnung von Hindernisklicks, nahtloser Richtungswechsel im Lauf).
