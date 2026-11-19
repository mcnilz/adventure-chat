# 31 – Kombinierte Interaktionen in einer Referenzszene vorführen

**What to build:** Ein leicht auffindbarer Ablauf in der Referenzwelt verbindet Objektzustände, Figurenanimationen, zustandsabhängige NPC-Gespräche und eine freiwillige Führung. Dieselben Stationen lassen sich auch einzeln ausprobieren.

**Blocked by:** [25](25-object-state-transitions.md), [26](26-character-animations-for-object-actions.md), [28](28-state-dependent-dialogue.md), [30](30-follow-npc.md). Falls die Platzierung in die noch nicht abgenommene Gestaltung aus 19 eingreift, deren Freigabe vor der Integration berücksichtigen.

**Status:** draft

## Acceptance criteria

- [ ] Der Ablauf ist in der Referenzwelt vom Einstieg aus auf kurzem, verständlichem Weg erreichbar. Er ergänzt die Beispiele aus den Funktionstickets und verwendet vorhandene Räume und Stationen, wo sie passen. Sind eigene Räume sinnvoll, kann ein zentraler Startraum direkte Zugänge anbieten.
- [ ] Eine Objektinteraktion eröffnet oder verändert ein Gespräch. Der NPC bietet eine Führung über mehrere Stationen an. Der Besucher kann sie ablehnen oder abbrechen und die Stationen unabhängig davon benutzen.
- [ ] Ein zweiter Besucher kann die gemeinsamen Objektänderungen und Figurenanimationen nachvollziehen. Autor und Prüfer können dieselbe Szene auch in einer isolierten Arbeitskopie ausprobieren.

## Beispiel und vorgeschlagene Testgrenzen

Eine kurze Büroeinführung ist ein mögliches Motiv. Eine Klingel verändert den Gesprächseinstieg mit einem Empfangs-NPC. Optional kann ein Auslöser nach [32](32-object-triggered-npc-reactions.md) den NPC zum Eingang rufen. Der NPC bietet einen Rundgang an, der höhenverstellbaren Schreibtisch, Sitzplatz und Tafel als Stationen nutzt. Lage und Motive vor Inhaltsintegration festlegen. Einstieg, Auffindbarkeit, Gespräch, Rundgang, Abbruch und freie Nutzung im Browser sowie in isolierter Vorschau prüfen.
