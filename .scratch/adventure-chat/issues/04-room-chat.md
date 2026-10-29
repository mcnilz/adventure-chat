# 04: Raumchat mit Verlauf und Sprechblasen benutzen

**What to build:** Teilnehmer im selben Raum können Text schreiben, Nachrichten im Verlauf lesen und den Sprecher an einer kurzlebigen Sprechblase erkennen.

**Blocked by:** 03

**Status:** draft

## Acceptance criteria

- [ ] Eine Nachricht erscheint genau einmal beim Absender und beim anderen aktuellen Raumteilnehmer.
- [ ] Absendername und Reihenfolge sind nachvollziehbar; der Server bestimmt die Absenderidentität.
- [ ] Sprechblasen verschwinden nach der festgelegten Zeit; der begrenzte Verlauf bleibt in derselben Raumsitzung lesbar.
- [ ] HTML-ähnlicher Inhalt wird als Text angezeigt und führt keinen Code aus.
- [ ] Leere, zu lange und zu häufige Nachrichten werden verständlich abgewiesen.
- [ ] Tippen und Klicken im Chat lösen keine Figurenbewegung aus.

## Testgrenzen – vor Beginn bestätigen
Zwei Browserkontexte und öffentliche Chatkommandos.

## Hinweise
Namen-/Nachrichtenlänge, Verlaufslimit, Sprechblasendauer und Nachrichtenrate vor Implementierung bestätigen. Noch keine Chatarchive.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

