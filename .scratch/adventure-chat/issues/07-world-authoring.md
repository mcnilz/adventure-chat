# 07: Selbst erstellte Räume verbinden und gemeinsam nutzen

**What to build:** Ein Autor verbindet im Editor zwei selbst erstellte Räume und kann die gespeicherte Welt anschließend mit anderen Teilnehmern als begehbaren Chat benutzen.

**Blocked by:** 05, 06

**Status:** done

## Acceptance criteria

- [x] Ausgang per Maus platzieren; Zielraum und Spawn aus vorhandenen Einträgen auswählen.
- [x] Fehlende Referenzen und unerreichbare Ausgänge verständlich melden.
- [x] Welt speichern und nach Neustart ohne Handänderung der YAML-Dateien laden.
- [x] Zwei unabhängige Browser laufen, chatten und wechseln durch die selbst erstellten Räume.
- [x] Editor-Vorschau und regulärer Client verwenden übereinstimmende Geometrie und Übergänge.
- [x] Lokale End-to-End-Anleitung umfasst Autorenmodus und getrennten Spielmodus.
- [x] MVP-Abnahmeszenario ist durchgespielt; verbleibende Grenzen und alle tatsächlich ausgeführten Checks sind dokumentiert.

## Testgrenzen – vor Beginn bestätigen
Alle zuvor bestätigten öffentlichen Grenzen; keine neue Test-API.

## Hinweise
Kein Live-Editieren einer besuchten Welt nötig: Speichern und Neuladen reichen. Keine öffentliche Bereitstellung.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
- Ausgeführte Checks:
  - `npm run typecheck` (0 Fehler)
  - `npm run build` (0 Fehler)
  - `dotnet test` (106/106 Tests erfolgreich bestanden: 71 Server-Unit/Integrationstests + 35 Playwright-E2E-Tests inklusive `WorldAuthoringTests`).
- Reviewbefunde:
  - Ausgangs-Platzierung mit Zielraum- und Ziel-Spawn-Auswahl im Editor (`EditorExitDialog`) integriert und visuell auf dem Canvas positionierbar.
  - Raum- und weltweite Validierung (`RoomValidator.ValidateWorld`, `/api/editor/validate-world`, Button `🌐 Welt prüfen`) prüft Unerreichbarkeit von Ausgängen über Wegfindung sowie Gültigkeit aller Zielraum- und Spawn-Referenzen.
  - Playwright E2E-Test `WorldAuthoringTests` verifiziert das gesamte MVP-Szenario: Zwei selbst erstellte und miteinander verknüpfte Räume werden gespeichert und von zwei unabhängigen Browser-Instanzen im Echtzeit-Multiplayer mit Wegfindung, Raumwechseln und Chat bespielt.
  - README.md wurde um die vollständige Anleitung für getrennten Autoren- (`--authoring`) und Spielmodus sowie Schritt-für-Schritt-Anleitung zur Welterstellung erweitert.
- Status: `ready-for-human` (Wartet auf menschliche Abnahme).

