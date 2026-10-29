# Adventure Chat – Planungspaket
Revision 1 · 28.10.2026 · Arbeitsname, endgültiger Name offen.

## Zweck
Dieses Paket übergibt die besprochene Produktidee an eine frische Codex-Sitzung. Es enthält keine Implementierung und keine installierten Skills. Anforderungen sind aus dem Gespräch abgeleitet; neue Detailentscheidungen sind ausdrücklich Vorschläge.

## Einstieg
1. [AGENTS.md](AGENTS.md): Arbeitsregeln und Lesereihenfolge.
2. [SPEC.md](SPEC.md): Umfang, User Stories und MVP-Abnahme.
3. [ARCHITECTURE.md](ARCHITECTURE.md): Technik und Zuständigkeiten.
4. [CONTENT-FORMAT.md](CONTENT-FORMAT.md): YAML und Bildressourcen.
5. [TESTING.md](TESTING.md): vorgeschlagene Testgrenzen.
6. [PLAN.md](PLAN.md): Abhängigkeiten und prüfbare Ergebnisse.
7. [SKILLS.md](SKILLS.md): Matt-Pocock-Workflow und Startprompts.
8. [active-state.md](active-state.md): aktueller Stand.
9. [GLOSSARY.md](GLOSSARY.md): gemeinsame Begriffe.

## Verwendung
Paket in einen eigenen Projektordner entpacken. Noch kein Projektname, Remote oder Repository vorgegeben. Existierende AGENTS.md beim Übernehmen zusammenführen, nicht überschreiben. Die lokalen Tickets unter [.scratch/adventure-chat/issues](.scratch/adventure-chat/issues/) gehören zu diesem dauerhaften Plan: nicht pauschal ignorieren oder löschen.

Alle Tickets sind Vorschläge, keine bereits abgenommenen Implementierungsaufträge. Zunächst Paket 01 mit vorgeschlagenen Testgrenzen bestätigen lassen. Danach jeweils ein Paket implementieren, prüfen und vorführen. Für spätere Sitzungen reichen AGENTS.md, active-state.md, das konkrete Ticket und dessen verlinkter Fachkontext.

## Festgelegt und noch offen
Festgelegt: Browser; TypeScript; wenige npm-Abhängigkeiten; .NET-Backend; neue Engine mit Adventure-Konzepten; YAML mit Schema; einfache Bilddateien; Browsereditor; MVP mit Bewegung, Räumen und Chat; Inventar und Skripting später.

Planungsannahmen: private Tests statt öffentlichem Dienst, ungefähr zehn gleichzeitige Besucher, Desktopbrowser mit Maus, Punktfigur für Wegfindung ohne Spielerkollision, flüchtige Sitzungen und Nachrichten. Diese Annahmen werden vor dem jeweiligen betroffenen Paket bestätigt. GitHub-Veröffentlichung ist eine spätere Option.

