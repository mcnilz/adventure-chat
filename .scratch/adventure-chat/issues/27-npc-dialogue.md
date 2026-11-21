# 27 – Einen NPC mit „Spreche mit“ ansprechen

**What to build:** Ein Autor platziert eine benannte computergesteuerte Figur im Raum. Der Spieler wählt „Spreche mit“, klickt die Figur an und erhält vorbereitete Gesprächsoptionen mit jeweils passender Antwort.

**Blocked by:** [22](22-verbs-and-interaction-editor.md).

**Status:** ready-for-human

## Acceptance criteria

- [x] Eine konfigurierte Figur erscheint mit Name, Position und passender Grafik. Zwei Besucher sehen dieselbe Figur. Beim Verlassen und Wiederbetreten des Raums entsteht kein Doppelgänger.
- [x] Die bestehende Verbenleiste führt mit „Spreche mit“ zu einem sichtbaren Aktionssatz. In Gesprächsreichweite lassen sich mindestens zwei unterschiedliche Aussagen wählen, auf die der NPC passend antwortet.
- [x] Der Spieler kann das Gespräch verlassen und wieder laufen. Bei einem unerreichbaren Gegenüber oder einem vor dem Gespräch gewechselten Raum erhält er eine nachvollziehbare Rückmeldung.
- [x] Gesprächsdaten verweisen eindeutig auf den NPC und seine Antwortoptionen. Ungültige Verweise werden an der Inhaltsgrenze erkannt.
- [x] In der Referenzwelt ist ein NPC vom Einstieg aus auf kurzem Weg auffindbar und bietet mindestens zwei unterschiedliche Gesprächsoptionen an. Die Gesprächsinhalte werden über die regulären Autorendaten definiert.

## Bestätigter Umfang und Testgrenzen

Am 21.11.2026 mit „ja“ bestätigt. Eine stationäre Figur aus dem Figurenkatalog steht in der Startlounge. Mit „Spreche mit“ und Klick läuft der Besucher in Gesprächsreichweite. Ein privates Dialogfeld zeigt die vorbereiteten Optionen und die Antwort zur jeweiligen Auswahl. Zwei Besucher können unabhängig und gleichzeitig mit derselben Figur sprechen. Geprüft werden die öffentlichen Inhalts- und Browsergrenzen, einschließlich älterer Referenzfixtures, Reichweite, Auswahl, Schließen, Raumwechsel und Trennung vom Raumchat.

## Umsetzung

Nela steht links vom Einstieg in der Lounge. Ihre Position, der Gesprächspunkt und die zwei Antworten stehen in der [Raum-YAML](../../../worlds/reference/rooms/lounge.yaml). Die Figur verwendet das bestehende Bruno-Sprite aus dem Weltkatalog. Der Server prüft beim Ansprechen den aktuellen Raum, die Ankunft und die freie Verbindung. Namen und Positionen der NPCs kommen mit dem Raum-Snapshot. Die Gesprächsoptionen gehen mit der Antwort auf die Anfrage ausschließlich an den anfragenden Browser. Der Besucher kann per Schaltfläche, Escape oder Klick ins Spielfeld aussteigen.

Das optionale Feld `npcs` erweitert den [Autorenvertrag](../../../CONTENT-FORMAT.md#stationäre-npcs-und-gespräche-ab-1110) in Version 1.11.0. Referenzinhalt 5.2.0. Die Bearbeitung erfolgt in YAML, der Browsereditor bewahrt die Daten beim Speichern und zeigt die Figur in der Vorschau. Die veröffentlichten Schemata und die älteren Referenzfixtures wurden gegen `c24e2ac` geprüft. Dazu gehören 114 unveränderte versionierte Dateien unter `schemas`, `tests/Fixtures`, `worlds/map` und `worlds/scroll`. Keine neuen Abhängigkeiten.

## Prüfbelege

- Typcheck und Clientbuild bestanden. Gezielt 14 Tests bestanden (fünf Inhaltsfälle und neun Fälle im Browser-Testprojekt einschließlich der vorhandenen Türwechsel).
- Abschließende Vollsuite vom 21.11.2026 mit 371 bestandenen Tests (251 Server, 120 im Browser-Testprojekt), ohne Fehler oder übersprungene Tests. Das Browser-Testprojekt lief 11 Minuten und 10 Sekunden.
- Der erste Browsertest scheiterte am fehlenden Verb und bestand nach der Implementierung. Der neue Test für das Editor-Upgrade belegte den falschen Schema-Rückfall vor der Korrektur. Validierung, Upgrade, erneutes Speichern und Neuladen im selben Prozess bestehen. Der Test für den Türklick während des Hinlaufens belegte eine ausbleibende Übergabe an den Ausgang. Der korrigierte Ablauf prüft den tatsächlichen Raumwechsel und die lokale Abbruchmeldung.
- Spezifikations- und Standardsreview unabhängig durchgeführt. Rückmeldung beim Türklick, Formatdokumentation, Auswahl des Validators und Übereinstimmung der Dialogantwort zwischen Server und Browser korrigiert und geprüft.
- Spiel und Editor der lokalen Abnahmekopie liefern HTTP 200. Spielbeitritt, erste Antwort und Schließen wurden in der Kopie im Browser geprüft. Bildschirmaufnahme unter `.scratch/adventure-chat/evidence/27/acceptance-dialogue.png`.
- Drei ältere Bildschirmaufnahmen, die der bestehende Navigationstest erneut geschrieben hatte, wurden anhand der lokalen Originale bytegleich wiederhergestellt und geprüft.

## Manuelle Abnahme

1. [Lokale Abnahmekopie](http://127.0.0.1:64743/) in zwei Fenstern mit verschiedenen Namen öffnen. Nela steht links in der Lounge.
2. In beiden Fenstern **Spreche mit** wählen, über Nela zeigen und sie anklicken. Die Figuren laufen zu ihr. Sobald die Fragen die Verbenleiste ersetzen, im ersten Fenster **Wer bist du?**, im zweiten **Wo ist der Garten?** wählen. Zuerst spricht die eigene Figur, nach einer kurzen Pause antwortet Nela. Bei **Wer bist du?** folgt eine zweite Antwortblase. Per Linksklick ins Spielfeld oder mit **.** zur nächsten Blase springen. Jedes Fenster zeigt die eigenen Sprechblasen.
3. In einem Fenster **Gespräch beenden** oder Escape wählen, auf den Boden klicken und durch die rechte Tür in den Garten gehen. Nach der Rückkehr steht Nela einmal an derselben Stelle.
4. Vom rechten Teil der Lounge erneut Nela ansprechen und während des Hinlaufens auf die Gartentür klicken. Der Raumwechsel bricht das Gespräch ab und zeigt die Rückmeldung in der Aktionszeile.

Die menschliche Abnahme steht aus.

## Nachtrag zur Gesprächsbedienung

Am 21.11.2026 wurden Gesprächspausen, Nelas leicht verzögerte Antwort als Sprechblase und die Fragen anstelle der Verbenleiste angefordert. Die gewählte Frage steht zunächst über der eigenen Figur. Die Anzeigezeit richtet sich nach der Textlänge. Vor der Antwort und zwischen ihren Teilen vergehen jeweils 250 ms. Antworten werden an Satzgrenzen und bei langen Einzelabschnitten weiter geteilt. Während einer laufenden Blase oder Pause zeigt ein Linksklick ins Spielfeld oder die Punkt-Taste den nächsten Abschnitt. Die Punkt-Taste bleibt im Chat ein Zeichen. Nach der letzten Blase sind die Fragen wieder wählbar. Wird das Gespräch beendet, verschwinden die Blasen und die Verbenleiste kehrt zurück. Weltformat und Referenzinhalt bleiben kompatibel.

Typcheck, Build und neun gezielte Tests bestanden. Der Browser prüft die gezeichnete Frage und Antwort, den zweiten Besucher, mehrteilige Antworten mit und ohne Wortgrenzen, beide Tasten- und Mausabläufe sowie das Entfernen der Blase beim Abbruch. In der aktualisierten Abnahmekopie wurden der vollständige Dialogablauf und die lesbare Ausweichblase bei einem schmalen Ausschnitt im Browser geprüft. Die aktuelle Aufnahme liegt unter `.scratch/adventure-chat/evidence/27/lounge-dialogue.png`.

Die abschließende Vollsuite bestand mit 371 Tests (251 Server, 120 im Browser-Testprojekt). Der Browserlauf dauerte 11 Minuten und 40 Sekunden. Spezifikations- und Standardsreview führten zu Umbruch, Ausweichanzeige für kleine Ausschnitte, Zeichenreihenfolge und strengeren Browserprüfungen. Veröffentlichte Schemata, Weltdateien und Referenzfixtures wurden gegen `18510a8` geprüft. Drei durch den Testlauf neu geschriebene Aufnahmen von Ticket 21 wurden bytegleich wiederhergestellt.

Der Benutzer hat die aktualisierte Abnahmekopie ausprobiert und am 21.11.2026 „funktioniert soweit gut“ zurückgemeldet.
