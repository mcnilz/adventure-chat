# 27 – Einen NPC mit „Spreche mit“ ansprechen

**What to build:** Ein Autor platziert eine benannte computergesteuerte Figur im Raum. Der Spieler wählt „Spreche mit“, klickt die Figur an und erhält vorbereitete Gesprächsoptionen mit jeweils passender Antwort.

**Blocked by:** [22](22-verbs-and-interaction-editor.md).

**Status:** draft

## Acceptance criteria

- [ ] Eine konfigurierte Figur erscheint mit Name, Position und passender Grafik. Zwei Besucher sehen dieselbe Figur. Beim Verlassen und Wiederbetreten des Raums entsteht kein Doppelgänger.
- [ ] Die bestehende Verbenleiste führt mit „Spreche mit“ zu einem sichtbaren Aktionssatz. In Gesprächsreichweite lassen sich mindestens zwei unterschiedliche Aussagen wählen, auf die der NPC passend antwortet.
- [ ] Der Spieler kann das Gespräch verlassen und wieder laufen. Bei einem unerreichbaren Gegenüber oder einem vor dem Gespräch gewechselten Raum erhält er eine nachvollziehbare Rückmeldung.
- [ ] Gesprächsdaten verweisen eindeutig auf den NPC und seine Antwortoptionen. Ungültige Verweise werden an der Inhaltsgrenze erkannt.
- [ ] In der Referenzwelt ist ein NPC vom Einstieg aus auf kurzem Weg auffindbar und bietet mindestens zwei unterschiedliche Gesprächsoptionen an. Die Gesprächsinhalte werden über die regulären Autorendaten definiert.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Für den Einstieg genügt eine stationäre Figur mit Autorentext. Vor Freigabe klären, wo Antworten erscheinen, wer Gesprächsinhalte sieht und wie gleichzeitige Gespräche laufen. Öffentliche Inhalts- und Browsergrenzen prüfen Reichweite, Auswahl, Schließen, zwei Besucher, Raumwechsel und Trennung vom Raumchat.
