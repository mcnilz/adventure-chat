# 29 – NPCs auf Wegen und in Aufenthaltsbereichen bewegen

**What to build:** Autoren geben einem NPC Wegpunkte für einen wiederkehrenden Weg. Optional kann eine Figur wechselnde erreichbare Ziele innerhalb eines markierten Bereichs wählen, damit ihr Weg variiert.

**Blocked by:** [21](21-state-dependent-navigation.md), [27](27-npc-dialogue.md).

**Status:** draft

## Acceptance criteria

- [ ] Eine Figur läuft eine konfigurierte Wegpunktfolge im Raum mit erkennbaren Lauf- und Standphasen ab. Zwei Besucher sehen denselben Verlauf und ein späterer Eintritt die aktuelle Position.
- [ ] Wenn ein zustandsabhängiges Hindernis einen Weg sperrt, nimmt der NPC einen gültigen Umweg oder hält nach der vereinbarten Regel sicher an.
- [ ] Der NPC lässt sich unterwegs ansprechen. Verhalten während und nach dem Gespräch ist für beide Besucher nachvollziehbar.
- [ ] Wenn der optionale Aufenthaltsbereich aufgenommen wird, demonstriert ein NPC wechselnde erreichbare Ziele innerhalb der Fläche. Die Bewegung bleibt für alle Besucher konsistent.
- [ ] Ein gut erreichbarer NPC in der Referenzwelt zeigt eine kurze Wegpunktfolge. Wird der Aufenthaltsbereich umgesetzt, lässt er sich dort ebenfalls beobachten.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Vor Freigabe Wiederholung, Haltezeiten und Verhalten bei Gesprächen festlegen. Wegpunkte bilden den ersten vorführbaren Ausschnitt. Inhaltsprüfung und zwei Browser decken Wegfolge, Blockade, spätere Raumansicht und Gespräch ab. Für Aufenthaltsbereiche Grenzen und Erreichbarkeit prüfen. Einzelne zufällige Zielentscheidungen sind für die Abnahme unerheblich.
