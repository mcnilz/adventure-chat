# 01: Lokal einen Raum betreten und die eigene Figur bewegen

**What to build:** Als Entwickler kann ich die Anwendung lokal starten, einen Beispielraum öffnen und eine eindeutig erkennbare Figur per Klick auf einer rechteckigen Bodenfläche bewegen. Dieser erste schmale Weg umfasst Browser, Serverauslieferung und sichtbare Bewegung.

**Blocked by:** None (can start immediately)

**Status:** draft

## Acceptance criteria

- [ ] Dokumentierte PowerShell-7-taugliche Start-/Buildschritte funktionieren mit den benannten Voraussetzungen.
- [ ] Browser zeigt PNG-Beispielhintergrund und eigene Figur mit Stand-/Laufzustand.
- [ ] Klick innerhalb der Bodenfläche bewegt die Figur zeitbasiert; ein neuer Klick ersetzt das Ziel.
- [ ] Klicks auf zukünftigen Chatbereich und außerhalb der Bodenfläche bewegen die Figur nicht.
- [ ] Skalierung des Browserfensters erhält die logischen Zielkoordinaten.
- [ ] TypeScript-Prüfung und vereinbarter Browser-Verhaltenstest bestehen.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung; noch kein Test einer finalen Mehrbenutzer-Schnittstelle.

## Hinweise
Minimaler Entwicklungsstart mit einheitlicher Pixelkoordinate; der erste Slice braucht noch kein vollständiges YAML-System.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

