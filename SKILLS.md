# Matt-Pocock-Skills und Codex-Übergabe
Recherche: 28.10.2026. Primärquelle: [mattpocock/skills](https://github.com/mattpocock/skills).
Keine Skills installiert, verändert oder in dieses Paket kopiert. Die Quellen wurden für diese Planung gelesen; Skill-Dateien müssen im Zielsystem verfügbar sein. Quellen auf main können sich ändern; später eine konkrete geprüfte Revision auswählen.

## Passende Werkzeuge
| Skill | Einsatz für dieses Projekt |
| --- | --- |
| setup-matt-pocock-skills | Vorhandene lokale Tracker-/Glossarkonfiguration prüfen und bei Bedarf anpassen |
| grill-with-docs | Nur noch echte fachliche Lücken klären und Begriffe festhalten |
| to-spec | Spätere Anforderungen in eine Spezifikation überführen; MVP-Spezifikation liegt schon vor |
| to-tickets | Neue Spezifikation in kleine vollständige Funktionen mit Abhängigkeiten zerlegen; MVP-Entwurf liegt schon vor |
| implement | Ein bestätigtes Ticket in einer frischen Sitzung umsetzen |
| tdd | An zuvor bestätigten öffentlichen Grenzen Verhalten schrittweise testen |
| code-review | Änderungen gegen Anforderungen und Projektregeln prüfen |
| retro | Nach einem Paket konkrete Verbesserungen der Arbeitsumgebung vorschlagen |

Empfehlung: für diesen MVP zunächst implement je Ticket. implement-spec ist für eine spätere ausdrücklich beauftragte Gesamtausführung mit parallelen Implementierern interessant; zunächst unnötig.

## Kompatibilität
Die offizielle implement-Anweisung erwartet eine Spezifikation oder Tickets, regelmäßige Checks, TDD und abschließenden Review; außerdem einen lokalen Commit. AGENTS.md präzisiert die Projektgrenzen.
Die lokalen Tickets verwenden das vorgesehene Schema mit Titel, Blocked by und abnehmbaren Ergebnissen. Anders als fertig freigegebene Tickets stehen sie bewusst zunächst auf draft.
Testgrenzen sind in TESTING.md als Vorschlag dokumentiert. Der spätere Agent muss sie bestätigen lassen; diese Datei behauptet keine bereits erfolgte Zustimmung.
User-invoked Skills müssen vom Benutzer aufgerufen werden. Ein Eintrag in AGENTS.md installiert sie nicht und macht ihren Aufruf nicht automatisch verfügbar.

## Startprompt ohne Skill-Abhängigkeit
```text
Lies AGENTS.md, active-state.md und das Ticket 01 unter
.scratch/adventure-chat/issues/. Lies den darin nötigen Fachkontext.
Das ist ein neues Projekt; es gibt noch keine Implementierung.
Schlage Paket 01 mit den relevanten Testgrenzen vor und warte auf meine
Bestätigung. Bewahre den vereinbarten MVP-Umfang und arbeite lokal.
Keine Skills installieren und kein Remote anlegen.
```

## Nach Bestätigung mit installiertem implement-Skill
Skill-Aufrufsyntax hängt vom Host ab; in Codex den tatsächlich verfügbaren Skill auswählen. Inhaltlicher Auftrag:
```text
Nutze implement für das von mir bestätigte Ticket 01.
Beachte AGENTS.md, SPEC.md und die bestätigten Grenzen aus TESTING.md.
TDD nur an diesen Grenzen, danach Code-Review und lokale Checks.
Berichte die Abnahmeschritte und aktualisiere Ticket sowie active-state.md.
Kein Push, kein externer Issue-Tracker und kein Deployment.
```
Für weitere Tickets Nummer und Pfad passend ersetzen. Bei fehlendem Skill Auftrag regulär ausführen und dessen Nichtverfügbarkeit offen benennen. Review mit Subagenten nur, wenn der ausdrücklich aufgerufene Skill dies vorsieht und die Umgebung es unterstützt; andernfalls Reviewumfang korrekt beschreiben.

## Installation später
Nicht Bestandteil dieses Auftrags. Offizielles Repository prüfen, benötigte Skills inklusive referenzierter Begleitdateien auswählen und eine geprüfte Revision verwenden. Kein Installationsbefehl mit ungeprüftem latest als notwendiger Projektstart. Die einmalige Installation von Agent-Skills ist von npm-Laufzeitabhängigkeiten der Anwendung zu unterscheiden.

## Primärquellen
- [Übersicht](https://github.com/mattpocock/skills/blob/main/README.md)
- [implement: Originalanweisung](https://github.com/mattpocock/skills/blob/main/skills/engineering/implement/SKILL.md)
- [to-spec: Originalanweisung](https://github.com/mattpocock/skills/blob/main/skills/engineering/to-spec/SKILL.md)
- [to-tickets: Originalanweisung](https://github.com/mattpocock/skills/blob/main/skills/engineering/to-tickets/SKILL.md)
- [tdd: Originalanweisung](https://github.com/mattpocock/skills/blob/main/skills/engineering/tdd/SKILL.md)
- [Setup: Originalanweisung](https://github.com/mattpocock/skills/blob/main/skills/engineering/setup-matt-pocock-skills/SKILL.md)

Die Dokumente dieses Pakets sind projektspezifische Planung, keine Kopien dieser Skills. Bei späteren Abweichungen die aktuell installierte Skill-Version lesen und Projektregeln beibehalten.

