# 04: Raumchat mit Verlauf und Sprechblasen benutzen

**What to build:** Teilnehmer im selben Raum können Text schreiben, Nachrichten im Verlauf lesen und den Sprecher an einer kurzlebigen Sprechblase erkennen.

**Blocked by:** 03

**Status:** ready-for-human

## Acceptance criteria

- [x] Eine Nachricht erscheint genau einmal beim Absender und beim anderen aktuellen Raumteilnehmer.
- [x] Absendername und Reihenfolge sind nachvollziehbar; der Server bestimmt die Absenderidentität.
- [x] Sprechblasen verschwinden nach der festgelegten Zeit; der begrenzte Verlauf bleibt in derselben Raumsitzung lesbar.
- [x] HTML-ähnlicher Inhalt wird als Text angezeigt und führt keinen Code aus.
- [x] Leere, zu lange und zu häufige Nachrichten werden verständlich abgewiesen.
- [x] Tippen und Klicken im Chat lösen keine Figurenbewegung aus.

## Testgrenzen – vor Beginn bestätigen
Zwei Browserkontexte und öffentliche Chatkommandos.

## Hinweise
Namen-/Nachrichtenlänge (max. 200 Zeichen), Verlaufslimit (50 Nachrichten), Sprechblasendauer (6s) und Nachrichtenrate (5 Nachrichten / 3s) bestätigt.

## Abschluss
- Ausgeführte Checks: `npm run typecheck` (0 Fehler), `npm run build` (0 Fehler), `dotnet test` (53/53 Tests bestanden: 34 Server-Tests, 19 Playwright-E2E-Tests).
- Reviewbefunde: Alle Akzeptanzkriterien erfüllt, XSS durch ausschließliche Text-Content-Zuweisung ausgeschlossen, Rate-Limiting und Raum-Isolation serverseitig über SignalR-Hub und SessionManager abgesichert.
- Status: `ready-for-human` (Wartet auf menschliche Abnahme).

