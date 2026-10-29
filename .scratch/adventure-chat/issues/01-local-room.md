# 01: Lokal einen Raum betreten und die eigene Figur bewegen

**What to build:** Als Entwickler kann ich die Anwendung lokal starten, einen Beispielraum öffnen und eine eindeutig erkennbare Figur per Klick auf einer rechteckigen Bodenfläche bewegen. Dieser erste schmale Weg umfasst Browser, Serverauslieferung und sichtbare Bewegung.

**Blocked by:** None (can start immediately)

**Status:** ready-for-human

## Acceptance criteria

- [x] Dokumentierte PowerShell-7-taugliche Start-/Buildschritte funktionieren mit den benannten Voraussetzungen.
- [x] Browser zeigt PNG-Beispielhintergrund und eigene Figur mit Stand-/Laufzustand.
- [x] Klick innerhalb der Bodenfläche bewegt die Figur zeitbasiert; ein neuer Klick ersetzt das Ziel.
- [x] Klicks auf zukünftigen Chatbereich und außerhalb der Bodenfläche bewegen die Figur nicht.
- [x] Skalierung des Browserfensters erhält die logischen Zielkoordinaten.
- [x] TypeScript-Prüfung und vereinbarter Browser-Verhaltenstest bestehen.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung; noch kein Test einer finalen Mehrbenutzer-Schnittstelle.

## Hinweise
Minimaler Entwicklungsstart mit einheitlicher Pixelkoordinate; der erste Slice braucht noch kein vollständiges YAML-System.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
- **Umsetzung:**
  - ASP.NET Core (.NET 10) Serverprojekt unter `src/AdventureChat.Server`.
  - TypeScript-Client mit Canvas-2D-Renderer, zeitbasierter Bewegung und Aspect-Ratio-Erhaltung unter `src/AdventureChat.Client`.
  - PNG-Ressourcen für Raumhintergrund (`lounge.png`, 640x360) und animierte Spielfigur (`character.png`) mit Stand- und Laufphasen.
  - Automatisierte E2E-Playwright/NUnit-Tests unter `tests/AdventureChat.E2ETests`.
- **Checks:**
  - `npm run typecheck` (`tsc --noEmit`): 0 Fehler.
  - `dotnet build`: 0 Warnungen, 0 Fehler.
  - `dotnet test`: 5/5 Playwright-E2E-Tests erfolgreich (Initialisierung, Bewegung & Stopp, Zielüberschreibung bei Klick im Laufen, Klickfilterung auf Chat & Wänden, Fenstergrößenänderung).
- **Status:** ready-for-human. Menschliche Abnahme ausstehend.

