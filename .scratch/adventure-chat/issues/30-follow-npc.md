# 30 – Freiwillig einem NPC durch Räume folgen

**What to build:** Ein NPC bietet im Gespräch eine Führung an. Nach aktiver Zustimmung folgt die eigene Figur automatisch über konfigurierte Stationen und Raumübergänge. Der NPC kann unterwegs vorbereitete Erläuterungen zeigen.

**Blocked by:** [27](27-npc-dialogue.md), [29](29-npc-movement.md).

**Status:** draft

## Acceptance criteria

- [ ] Vor Zustimmung bleibt der Spieler bei der normalen Bedienung. Nach Zustimmung folgt die Figur sichtbar zu mindestens zwei Stationen, darunter eine in einem anderen Raum. Mindestens eine Station zeigt eine zugehörige Erklärung.
- [ ] Über die vereinbarte Abbruchbedienung erhält der Spieler sofort die Kontrolle zurück. Das gilt während des Laufens und nach einem Raumwechsel. Ein zweiter Besucher wird durch die Zustimmung des ersten nicht zum Folgen verpflichtet.
- [ ] Bei einem gesperrten Übergang endet das Folgen sicher oder pausiert mit verständlicher Rückmeldung nach der vereinbarten Regel. Raumwechsel und Laufweg nutzen die geprüften Weltübergänge.
- [ ] Nach Abschluss, Abbruch und Verbindungsverlust ist klar, ob eine weitere Führung neu angeboten werden kann. Die Rückkehr zum normalen Laufen und Chatten ist prüfbar.
- [ ] Die Referenzwelt bietet eine vom Einstieg aus leicht auffindbare, kurze Führung. Zwei nahe Stationen in verbundenen Räumen genügen. Zustimmung, Erklärung und Abbruch können dort ausprobiert werden.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Vor Freigabe Abbruchbedienung, Wiederaufnahme nach Unterbrechung und gemeinsames Verhalten bei mehreren Gästen klären. Für die erste Fassung genügen eine feste Route und vorbereitete Stationstexte. Zwei Browser prüfen Zustimmung, unabhängige Kontrolle, Raumwechsel, dynamische Sperre, Abbruch und Verbindungsverlust.
