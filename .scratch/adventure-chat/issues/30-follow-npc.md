# 30 – Freiwillig einem NPC durch Räume folgen

**What to build:** Ein NPC bietet im Gespräch eine Führung an. Nach aktiver Zustimmung folgt die eigene Figur automatisch über konfigurierte Stationen und Raumübergänge. Der NPC kann unterwegs vorbereitete Erläuterungen zeigen.

**Blocked by:** [27](27-npc-dialogue.md), [29](29-npc-movement.md).

**Status:** done

## Acceptance criteria

- [x] Vor Zustimmung bleibt der Spieler bei der normalen Bedienung. Nach Zustimmung folgt die Figur sichtbar zu mindestens zwei Stationen, darunter eine in einem anderen Raum. Mindestens eine Station zeigt eine zugehörige Erklärung.
- [x] Über die vereinbarte Abbruchbedienung erhält der Spieler sofort die Kontrolle zurück. Das gilt während des Laufens und nach einem Raumwechsel. Ein zweiter Besucher wird durch die Zustimmung des ersten nicht zum Folgen verpflichtet.
- [x] Bei einem gesperrten Übergang endet das Folgen sicher mit einer Rückmeldung. Raumwechsel und Laufweg nutzen die geprüften Weltübergänge.
- [x] Nach Abschluss, Abbruch und Verbindungsverlust bietet Nela nach ihrer Rückkehr später eine weitere Führung an. Die Rückkehr zum normalen Laufen und Chatten ist geprüft.
- [x] Die Referenzwelt bietet in der Startlounge eine kurze Führung zu zwei Stationen in verbundenen Räumen. Zustimmung, Erklärung und Abbruch können dort ausprobiert werden.
- [x] Der Abstand ist konfigurierbar und beträgt bei Nela 48 Raumpixel. Nach Abschluss verlässt sie den Garten über den realen Ausgang zur Lounge, während der Gast im Garten bleibt.

## Bestätigter Umfang und Testgrenzen

Am 22.11.2026 mit „ja“ bestätigt. Nela bietet in der Startlounge eine kurze Führung mit einer Station in der Lounge und einer im Tavernen-Garten an. Der Gast stimmt im Gespräch ausdrücklich zu. Nela führt jeweils einen Gast. Andere Besucher sehen die Figur unterwegs und steuern ihre eigene Figur selbst. **Führung beenden** gibt die Steuerung auch nach einem Raumwechsel sofort zurück. Kann der Server keinen gültigen Weg oder Übergang mehr finden, endet die Führung mit einer Erklärung. Abschluss, Abbruch und Wiederverbindung erlauben einen neuen Start im Gespräch.

Bestätigt sind die öffentliche Lade- und Speichergrenze und Browserabläufe mit zwei Besuchern. Die Prüfung umfasst gültige und ungültige Routendaten, unveränderte ältere Referenzwelten, Zustimmung, sichtbares Folgen, Erläuterungen, Raumwechsel, dynamische Sperre, Abbruch beim Laufen und nach einem Wechsel, Wiederverbindung und normales Laufen und Chatten danach. Das Bewegungsgefühl bleibt Teil der menschlichen Abnahme.

Nach Sichtfeedback am 22.11.2026 vergrößert sich der Abstand und wird in YAML konfigurierbar. Am Ende verlässt Nela den Garten durch die Tür zur Lounge. Ist kein Rückweg erreichbar, bleibt sie sichtbar stehen.

## Umsetzung und Kompatibilität

Die Führung startet erst nach **Spreche mit → Führung starten**. Der Server lässt Nela vorauslaufen und führt den Gast mit Abstand auf ihrem Weg. Die Erläuterungen erscheinen beim Gast unter dem Spielfeld. Andere Besucher sehen Nelas aktuelle Position. Die Führung verwendet die erlaubten Ausgänge und deren aktuelle Zielspawns. Eine Änderung der Raumgeometrie kann einen neuen gemeinsamen Weg auslösen. Fehlt ein gültiger Weg oder schließt sich der anstehende Ausgang, bleibt der Gast an seiner aktuellen Position und erhält eine Meldung.

Nela hält 48 Raumpixel Abstand, soweit Weg und Übergang das erlauben. Nach Abschluss, Abbruch und Verbindungsende geht sie selbstständig an ihre Ausgangsposition zurück. Bei Abschluss im Garten nimmt sie sichtbar die linke Tür. Die Vorschau im Raumeditor zeigt ihre normalen Gespräche. Die mehrraumige Führung ist im verbundenen Spiel verfügbar. Beim geführten Raumwechsel bleibt der Chat an den Raum gebunden, aus dem gesendet wird. Bei einer verspäteten Anfrage bleibt der Entwurf zum erneuten Senden im Eingabefeld.

Die optionalen Felder `tour` und `followDistance` erweitern das [Autorenformat](../../../CONTENT-FORMAT.md#npc-führungen-ab-1140) additiv auf **1.14.0**. Die Referenzwelt erhöht ihre Inhaltsversion auf **5.5.0**. Veröffentlichte Schema-Bündel und ältere Referenzfixtures bleiben bytegleich. Es gibt keine neuen Abhängigkeiten.

## Prüfbelege

- TypeScript-Typcheck, Clientbuild und .NET-Build bestanden. Alle 389 Tests bestehen (257 Server, 132 Browser in drei Gruppen), 0 übersprungene Tests und 0 Buildwarnungen. Veröffentlichte Schemata und ältere Testwelten gegen `9b8a75a` bytegleich geprüft.
- Die Browserfälle decken den konfigurierten Abstand in der Gartenstation, Nelas sichtbaren Rückweg, einzelne Zustimmung, Abbruch, Rückkehr zur Steuerung, gesperrten Ausgang, Wiederverbindung und raumgebundenen Chat ab. Die Abnahmekopie antwortet mit HTTP 200 und liefert den konfigurierten Abstand. Bildschirmaufnahme und Laufzeitbelege liegen lokal unter `.scratch/adventure-chat/evidence/30`.
- Die Browserprüfung schlug zunächst wegen des fehlenden Angebots fehl. Eine spätere Gegenprobe zeigte, dass der Gast mit der ersten Umsetzung an Nela vorbeilaufen konnte. Beide Fälle bestehen jetzt.
- Spezifikations- und Standardsreview wurden getrennt durchgeführt. Beide Nachprüfungen prüfen den gemeinsamen Laufweg, den atomaren Raumchat, den wiederherstellbaren Chatentwurf und die statisch wie dynamisch geprüfte Heimroute. Speichern einer Tour ohne letzten Rückweg wurde zuerst fälschlich akzeptiert und wird jetzt abgelehnt.

## Manuelle Abnahme

1. [Lokale Abnahmekopie](http://127.0.0.1:51715/) öffnen und die Referenzwelt betreten. In der Lounge links **Spreche mit** wählen und Nela anklicken. Die Figur geht zum Gesprächspunkt. Nela wartet auf **Führung starten**.
2. **Führung starten** wählen. Die Figur folgt Nela mit Abstand. Je eine Erklärung erscheint in der Lounge und im Tavernen-Garten. Im Garten **Führung beenden** wählen und anschließend wieder auf den Boden klicken und eine Chatnachricht senden.
3. Die Lounge in einem zweiten Browserfenster betreten. Eine erneute Führung bis zum Abschluss laufen lassen und prüfen, dass sich die zweite Figur jederzeit frei steuern lässt. Im Garten bleibt der Gast nach **„Die Führung ist abgeschlossen.“** stehen. Nela geht durch die linke Tür und bietet nach ihrer Rückkehr in der Lounge eine neue Führung an.

## Abnahme

Am 22.11.2026 ausdrücklich mit „abgenommen“ bestätigt. Lokaler Umsetzungscommit `011e24d`.
