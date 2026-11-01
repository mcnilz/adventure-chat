# Umsetzungsplan
Status: Tickets 01–08 sind menschlich abgenommen; 09 ist bestätigt und in-progress, 10–13 sind geplante Entwürfe.

| Ticket | Vorführbares Ergebnis | Blocked by |
| --- | --- | --- |
| 01 | Lokal starten und eine Figur per Klick bewegen | keine |
| 02 | Mit servergeprüfter Wegfindung ein Hindernis umgehen | 01 |
| 03 | Zwei Browser sehen einander eintreten und laufen | 02 |
| 04 | Im selben Raum schreiben und Sprechblasen sehen | 03 |
| 05 | Zwischen zwei Räumen wechseln; Chat bleibt je Raum getrennt | 04 |
| 06 | Eigenen PNG-Raum im Editor erstellen, speichern und öffnen | 02 |
| 07 | Übergänge grafisch verbinden und die selbst erstellte Welt gemeinsam nutzen | 05, 06 |
| 08 | Multiplayer-Stabilisierung und Autorenbetrieb | 07 |
| 09 | Externe versionierte Weltpakete und Referenzwelt | 07, 08 |
| 10 | Weltwahl und isolierter Mehrweltbetrieb | 09 |
| 11 | Begehbare Karte und feste Figurengröße | 09; Gesamtabnahme: 10 |
| 12 | Horizontal scrollende Räume | 09; Gesamtabnahme: 10, 11 |
| 13 | Tiefenskalierung und drei Demo-Szenarien | 09, 11; Gesamtabnahme: 12 |

Die Tickets liegen einzeln unter [.scratch/adventure-chat/issues](.scratch/adventure-chat/issues/).
Die Reihenfolge 01–07 ist die empfohlene serielle Umsetzung. Ticket 06 hängt fachlich nur von 02 ab; das ist keine automatische Beauftragung paralleler Agenten.

## Meilensteine
M1 nach 02: Bewegung und Geometrie funktionieren an einem Beispiel.
M2 nach 05: Begehbarer Chat mit zwei handgeschriebenen Räumen.
M3 nach 07: dieselbe Erfahrung mit selbst im Editor erstellten Räumen.

## Statusmodell
draft → ready-for-agent → in-progress → ready-for-human → done.
blocked zusätzlich bei konkretem Hindernis mit Begründung. ready-for-agent erst nach Bestätigung von Paketumfang und Testgrenzen; done erst nach menschlicher Abnahme.
Ein technischer Review ersetzt keine Produktabnahme. Keine offenen Entscheidungen stillschweigend als beschlossen behandeln.

