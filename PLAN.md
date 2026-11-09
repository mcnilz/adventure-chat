# Umsetzungsplan
Status: Tickets 01–16 außer 13 sind menschlich abgenommen und done. Ticket 16 wurde am 09.11.2026 ausdrücklich abgenommen. Ticket 13 ist damit nicht mehr blockiert und bleibt bis zur Bestätigung von Paketumfang und Testgrenzen draft. Details und Belege im jeweiligen Ticket und active-state.md.

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
| 14 | Weltbezogene Figurenwahl mit passenden Sprites | 09, 10, 11 |
| 15 | Vordergrundobjekte mit Tiefenordnung im Spiel und Editor | 06, 09, 11; Zusammenspiel: 12, 14 |
| 16 | Neue Referenzwelt mit Mara/Bruno und Tischverdeckung | 14, 15 |
| 13 | Tiefenskalierung und drei Demo-Szenarien | 09, 11, 14, 15, 16; Gesamtabnahme: 12 (done) |

Tickets 14–16 wurden am 04.11.2026 auf Benutzerauftrag als draft angelegt. Nur Planung beauftragt; andere Agenten übernehmen die spätere Umsetzung nach Bestätigung von Umfang und Testgrenzen. Verbindliche Reihenfolge laut anschließendem Benutzerauftrag: 14 → 15 → 16 → 13. Keine automatische Parallelbeauftragung. Formatversionen fortlaufend anhand des tatsächlichen Stands wählen; Ticket 13 ergänzt seine Demo-Anforderungen erst nach der neuen Referenzwelt.

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
