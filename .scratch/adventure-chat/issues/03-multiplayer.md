# 03: Zwei Browser sehen einander laufen

**What to build:** Zwei voneinander isolierte Browser betreten denselben Raum mit Anzeigenamen und sehen Eintritt, Bewegung und Verlassen der anderen Figur.

**Blocked by:** 02

**Status:** draft

## Acceptance criteria

- [ ] Zwei getrennte Browserkontexte sehen jeweils genau zwei eindeutig benannte Figuren.
- [ ] Bewegung eines Teilnehmers erscheint flüssig im anderen Browser.
- [ ] Ein später eintretender Teilnehmer erhält auch bereits laufende Bewegungen korrekt.
- [ ] Verlassen oder Verbindungsabbruch entfernt die betreffende Figur.
- [ ] Verbindungsverlust ist sichtbar; nach erneutem Eintritt bleibt kein alter Doppelgänger zurück.
- [ ] Ein Client kann weder fremde Figur-ID noch fremde Sitzung übernehmen.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentliche Echtzeitkommandos.

## Hinweise
Vor Beginn Reconnect als frischen Eintritt und private Testzielgröße bestätigen; keine persistente Identität einführen.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
Noch nicht umgesetzt. Hier später Checks mit Ergebnissen, Reviewbefunde und menschliche Abnahme nachtragen.

