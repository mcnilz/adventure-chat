# 29 – NPCs auf Wegen und in Aufenthaltsbereichen bewegen

**What to build:** Autoren geben einem NPC Wegpunkte für einen wiederkehrenden Weg. Optional kann eine Figur wechselnde erreichbare Ziele innerhalb eines markierten Bereichs wählen, damit ihr Weg variiert.

**Blocked by:** [21](21-state-dependent-navigation.md), [27](27-npc-dialogue.md).

**Status:** ready-for-human

## Acceptance criteria

- [x] Eine Figur läuft eine konfigurierte Wegpunktfolge im Raum mit erkennbaren Lauf- und Standphasen ab. Zwei Besucher sehen denselben Verlauf und ein späterer Eintritt die aktuelle Position.
- [x] Wenn ein zustandsabhängiges Hindernis einen Weg sperrt, nimmt der NPC einen gültigen Umweg oder hält nach der vereinbarten Regel sicher an.
- [x] Der NPC lässt sich unterwegs ansprechen. Verhalten während und nach dem Gespräch ist für beide Besucher nachvollziehbar.
- [x] Ein gut erreichbarer NPC in der Referenzwelt zeigt eine kurze Wegpunktfolge.

## Bestätigter Umfang und Testgrenzen

Am 22.11.2026 mit „ja“ bestätigt. Autoren konfigurieren eine zyklische Wegpunktfolge in YAML und prüfen sie in der isolierten Raumvorschau. Die Figur pausiert an jedem Wegpunkt für eine Sekunde. Bei einer Sperre sucht der Server einen gültigen Umweg oder hält die Figur auf begehbarem Boden an. Bei der nächsten Änderung der begehbaren Fläche versucht sie weiterhin dasselbe Ziel. Beim Ansprechen hält die Figur für beide Besucher sichtbar an und setzt nach dem letzten Gespräch ihren Weg fort. Ein konfigurierbarer Aufenthaltsbereich mit wechselnden Zielen bleibt als optionale Vertiefung offen.

Geprüft werden gültige und ungültige Angaben beim Laden und Speichern, ältere unveränderte Referenzwelten, die isolierte Vorschau und echte Browserabläufe mit zwei Besuchern. Dazu gehören Lauf- und Standphasen, ein späterer Eintritt, die Wegsperre und das Weiterlaufen nach Gesprächen. Bewegungsgefühl und Lesbarkeit bleiben menschliche Abnahme.

## Umsetzung und Kompatibilität

Ida läuft links vom Tisch im [Tavernen-Garten](../../../worlds/reference/rooms/garden.yaml) zwischen drei Punkten. Der Server berechnet die Wege mit der aktuellen Raumgeometrie und liefert beim Eintritt die aktuelle Position. Blockiert eine Zustandsänderung den verbleibenden Weg, berechnet der Server ihn zum selben Ziel neu. Kann Ida den Punkt nicht erreichen, hält sie an und wartet auf eine Änderung der begehbaren Fläche. Die Fußpunkt- und Sicherheitsankerregeln schützen auch bewegte NPCs.

**Spreche mit** hält Ida schon beim bestätigten Herangehen an. Jede aktive Unterhaltung hält die Figur weiter an. Beim Schließen, Weggehen, Raumwechsel oder Verbindungsende wird die Unterhaltung für diesen Besucher freigegeben. Ändert sich der Laufweg des Besuchers während des Herangehens, passt der Server die Frist an.

Format **1.13.0** ist eine additive Minor-Version. Das optionale Feld `waypoints` ergänzt den Autorenvertrag. Stationäre NPCs behalten ihr bisheriges Verhalten. Die Referenzwelt **5.4.0** ergänzt Ida. Die [Formatbeschreibung](../../../CONTENT-FORMAT.md#npc-wegpunkte-ab-1130) dokumentiert Validierung und Laufverhalten. Veröffentlichte Schema-Bündel und ältere Referenzfixtures bleiben bytegleich. Es gibt keine neuen Abhängigkeiten.

## Prüfbelege

- Typcheck, Clientbuild und Vollsuite am 22.11.2026 erfolgreich. 384 Tests bestanden, davon 256 Server- und 128 Browsertests, ohne übersprungene Tests oder Buildwarnungen. Der Browserlauf dauerte 13 Minuten und 39 Sekunden. Der Diff gegen `69721db` bestätigt unveränderte veröffentlichte Schemas und ältere Referenzfixtures.
- Die Ladeprüfung schlug zuerst wegen des fehlenden YAML-Felds fehl und bestand nach dem Einführen der Formatregeln. Der erste Bewegungsbrowsertest schlug zuerst am fehlenden bewegten NPC fehl. Die vier gezielten Browserprüfungen bestanden einschließlich tatsächlicher Eintrittsposition, vollständigem Umlauf, Haltezeit, Gesprächspause, Wegsperre und Wiederaufnahme sowie gespeichertem Formatwechsel und unabhängiger Vorschau.
- Spezifikations- und Standardsreview mit getrennten Nachprüfungen. Die Vorschau erhält den verbleibenden Weg, Umwege verlängern die Gesprächsfrist, und gleiche Zeichentiefe hat dieselbe Reihenfolge beim Zeichnen und Anklicken. Die Client-Simulation läuft außerhalb des Renderers. Laufende Korrekturen werden höchstens einmal pro Sekunde gesendet, zusätzliche Meldungen folgen auf Wechsel des Wegs oder der Laufphase.
- Spiel und Editor der [lokalen Abnahmekopie](http://127.0.0.1:58827/) antworteten mit HTTP 200. Zwei Browser zeigten Stillstand mit offenem Gespräch und Weiterlaufen nach dem letzten Schließen. In neun Sekunden Laufbetrieb empfing ein Browser 13 NPC-Meldungen mit zusammen 3.762 Byte JSON-Nutzlast. Während einer 1,5-sekündigen Gesprächspause kam keine NPC-Meldung. Browserbelege und Messwerte liegen lokal unter `.scratch/adventure-chat/evidence/29`.

## Manuelle Abnahme

1. [Lokale Abnahmekopie](http://127.0.0.1:58827/) öffnen und die Referenzwelt betreten. In der Startlounge rechts in den Tavernen-Garten gehen. Ida läuft links vom Tisch im Dreieck und hält an den Ecken kurz an.
2. **Spreche mit** wählen und Ida unterwegs anklicken. In einem zweiten Browserfenster ebenfalls in den Garten gehen. Ida steht dort an derselben Stelle. In beiden Fenstern mit ihr sprechen. Das erste Gespräch mit **Gespräch beenden** oder **Esc** schließen. Erst nach dem Schließen des zweiten Gesprächs geht Ida weiter.
3. Im [Raumeditor](http://127.0.0.1:58827/editor.html?worldId=reference) **Tavernen-Garten → Raum-Vorschau** öffnen. Ida dort ansprechen und prüfen, dass sie im Spielfenster unabhängig davon weiterläuft.

Menschliche Abnahme ausstehend. Lokaler Umsetzungscommit `Implement NPC waypoint patrols (ticket 29)`.
