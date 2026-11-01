# Ticket 08 mit Matt-Pocock-Skills bearbeiten

Stand: 01.11.2026. Dies ist eine Ausführungsanleitung, keine Skill-Installation und kein Implementierungsnachweis.

## Einstieg

Ticket: [.scratch/adventure-chat/issues/08-mvp-stabilization.md](../../.scratch/adventure-chat/issues/08-mvp-stabilization.md).

AGENTS.md, active-state.md, GLOSSARY.md, Ticket und verlinkten Fachkontext lesen. Umfang und Testgrenzen gemäß Projektablauf bestätigen lassen. Diese Vorbereitung allein autorisiert keine Implementierung.

## Geprüfte Skill-Quellen

Originalrepository: https://github.com/mattpocock/skills

Geprüfte Revision: `4ae2317ddb22b046e26455e4f9c9b3aaaf62212d`.

| Skill | Originalanweisung | Verwendung |
| --- | --- | --- |
| implement | [SKILL.md](https://github.com/mattpocock/skills/blob/4ae2317ddb22b046e26455e4f9c9b3aaaf62212d/skills/engineering/implement/SKILL.md) | bestätigtes lokales Ticket umsetzen, regelmäßig prüfen, Review und lokaler Commit |
| tdd | [SKILL.md](https://github.com/mattpocock/skills/blob/4ae2317ddb22b046e26455e4f9c9b3aaaf62212d/skills/engineering/tdd/SKILL.md) | bestätigte öffentliche Testgrenzen, jeweils ein Verhalten red → green |
| code-review | [SKILL.md](https://github.com/mattpocock/skills/blob/4ae2317ddb22b046e26455e4f9c9b3aaaf62212d/skills/engineering/code-review/SKILL.md) | Standards und Spec getrennt gegen feste Git-Basis prüfen |

Die Quellen wurden für dieses Paket gelesen. Keine Skill-Dateien kopiert oder installiert. Eine tatsächlich installierte abweichende Revision vor Anwendung lesen. Bei fehlenden Skills deren Nichtverfügbarkeit nennen und fachlich gleichwertig lokal arbeiten; keinen Aufruf behaupten. Installation gehört nicht zum Auftrag.

## Git-Basis und Review-Reihenfolge

Der geprüfte code-review-Skill verwendet `git diff <Basis>...HEAD`; uncommittete Änderungen fehlen darin. implement nennt den Commit erst nach dem Review. Für dieses Ticket gilt deshalb ausdrücklich folgende projektspezifische Anpassung:

1. Vor Codeänderungen sauberen Ausgangszustand prüfen und tatsächliches HEAD als festen Basis-Commit festhalten. Vorhandene fremde Änderungen nicht committen oder verwerfen; Scope im Zweifel isolieren.
2. Ticket seriell implementieren und prüfen. Nur eigene Paketänderungen lokal committen, damit der Review-Skill einen vollständigen Diff bis HEAD erhält. Kein Push.
3. code-review ausdrücklich mit dieser Basis und dem Ticketpfad aufrufen. Beide Achsen Standards und Spec prüfen. Der Originalskill sieht dafür zwei parallele Review-Subagenten mit frischem Kontext vor; nur im Rahmen dieses ausdrücklich aufgerufenen Skills und bei vorhandener Unterstützung. Keine parallele Implementierung.
4. Reviewkorrekturen lokal umsetzen, gezielt prüfen und separat committen. Danach Diff der Korrekturen und betroffene ursprüngliche Befunde erneut prüfen. Wenn Änderungen nach dem vollständigen Suite-Lauf entstanden sind, einschlägige Gesamtchecks vor Übergabe erneut ausführen.
5. Test- und Reviewbelege dokumentieren; Status ready-for-human. Menschliche Abnahme bleibt separat.

Die hier genannte Reviewbasis 3d355bd ist der historische Befundstand. Sie darf nicht blind als Ausführungsbasis dienen, wenn zwischenzeitlich andere Änderungen hinzugekommen sind.

## Startprompt: Paket vorstellen

```text
Lies AGENTS.md, active-state.md, GLOSSARY.md und
.scratch/adventure-chat/issues/08-mvp-stabilization.md sowie
docs/work-packages/08-stabilization-workflow.md.
Prüfe am aktuellen Code, welche Befunde noch bestehen.
Stelle Ticket 08, Detailentscheidungen und die dort genannten Testgrenzen
kurz vor und warte gemäß AGENTS.md auf meine Bestätigung.
Noch keine Implementierung, keine Skills installieren, keine Remote-Schreibzugriffe.
```

## Startprompt: Nach Bestätigung umsetzen

```text
Nutze den installierten Matt-Pocock-Skill implement für Ticket 08:
.scratch/adventure-chat/issues/08-mvp-stabilization.md.
Paketumfang und Testgrenzen dieses Tickets sind bestätigt.
Beachte AGENTS.md und docs/work-packages/08-stabilization-workflow.md.

Halte vor Änderungen das tatsächliche HEAD als festen Review-Basis-Commit fest.
Bearbeite A bis E seriell. Nutze tdd an den bestätigten öffentlichen Grenzen:
jeweils ein realer roter Regressionstest, dann die minimale Korrektur.
Keine parallele Implementierung und keine neuen Spielfunktionen.

Für code-review gilt ausdrücklich die im Workflow beschriebene Anpassung:
eigene geprüfte Änderungen vor dem Skill-Aufruf lokal committen, sodass
git diff <Basis>...HEAD alle Paketänderungen umfasst. Review mit dieser Basis
und dem Ticket als Spec durchführen, Standards und Spec getrennt berichten.
Reviewkorrekturen prüfen und separat lokal committen. Kein Push, PR oder Deployment.

Regelmäßig Typcheck und betroffene Tests, zum Abschluss alle einschlägigen Checks.
Dokumentiere echte Resultate und Einschränkungen; fehlende Skills oder Werkzeuge
nennen, keine Ausführung behaupten. Aktualisiere Ticket und active-state.md auf
ready-for-human und liefere konkrete manuelle Abnahmeschritte. Nicht selbst abnehmen.
```

Die Skill-Auswahl im jeweiligen Host tatsächlich vornehmen; das bloße Einfügen eines Skillnamens installiert ihn nicht.

## Erwartete Checks in PowerShell 7

```powershell
npm --prefix src/AdventureChat.Client ci --ignore-scripts
npm --prefix src/AdventureChat.Client run typecheck
npm --prefix src/AdventureChat.Client run build
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build --no-restore
```

Der Locked Restore ist erst sinnvoll grün, nachdem die Lockfiles in Schritt E erstellt wurden. Playwright-Browser müssen in der im Projekt festgelegten Version installiert sein; den konkreten Installationsbefehl anhand des gebauten Testprojekts in README dokumentieren. Fehlende Browser dürfen nicht als bestandener Testlauf erscheinen.

## Manuelle Abnahme

1. Zwei Browser: A läuft, B tritt währenddessen ein; beide zeigen ohne Neustart der Route denselben Bewegungsfortschritt. Eintritt nach Ankunft zeigt A am Ziel.
2. Während des Laufens mehrfach Ziel ändern, Tab pausieren/fortsetzen: kein dauerhafter Rückstand und kein Durchqueren von Hindernissen.
3. Durch Tür wechseln und chatten: korrekte Teilnehmer und Raumzustellung. Echte Transportunterbrechung mit automatischem Reconnect, anschließend genau eine Figur je aktiver Sitzung.
4. Normalstart: Autorenoberfläche und API gesperrt. --authoring: lokal PNG-Raum bearbeiten, speichern, Neustart und erneut betreten.
5. YAML absichtlich ungültig machen: Start meldet Datei und Ursache. Danach Datei reparieren und gültige Welt normal starten.

Automatisierte Manipulations-/Origin-/Uploadtests ergänzen diese Bedienprüfung; sie sind nicht durch manuelles Durchklicken ersetzt.
