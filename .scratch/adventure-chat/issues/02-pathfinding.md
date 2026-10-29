# 02: Ein Hindernis mit servergeprüfter Bewegung umgehen

**What to build:** In einem nicht rechteckigen Beispielraum erreicht meine Figur Ziele über erlaubte Wege. Der Server nimmt Zielkommandos an und entscheidet den gültigen Weg.

**Blocked by:** 01

**Status:** draft

## Acceptance criteria

- [ ] Ein Ziel hinter einem ausgesparten Hindernis wird über einen begehbaren Umweg erreicht.
- [ ] Ein unzugängliches Ziel wird abgelehnt, ohne Figur zu teleportieren oder den erlaubten Bereich zu verlassen.
- [ ] Neues Ziel während Bewegung ersetzt den Weg ohne ungültigen Sprung.
- [ ] Ungültige oder nicht endliche Koordinaten werden am Server zurückgewiesen.
- [ ] Gleiche Ausgangslage und Ziele erzeugen reproduzierbare fachliche Ergebnisse.
- [ ] Fehlende/ungültige Raumgeometrie wird verständlich gemeldet.

## Testgrenzen – vor Beginn bestätigen
Öffentliche Raumkommandos plus ein Browserablauf; fachliche Route-Grenze nur falls zusätzlich bestätigt.

## Hinweise
Vor Beginn Polygonmodell, Randfälle und Toleranzen festlegen. Serverautorität und feste IDs bereits hier vorsehen.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

