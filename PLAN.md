# Umsetzungsplan
Status (12.11.2026): Tickets 01–17 und 20 sind menschlich abgenommen und done. Tickets 18/19 und 21 sind ready-for-human; 22/23 bleiben draft. Tatsächliche Implementierungs- und Testbelege im jeweiligen Ticket und active-state.md.

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
| 17 (done) | Verlässliche Bearbeitungen und Ungespeichert-Status im Raumeditor | 06, 07, 11–15 (done); menschlich abgenommen am 09.11.2026 |
| 18 (ready-for-human) | Gesamte Spieloberfläche an Fensterbreite und -höhe anpassen; Vergrößern per Button | keine; am 09.11.2026 bestätigt, menschliche Abnahme ausstehend |
| 19 (ready-for-human) | Referenzrunde und Draufsicht gestalterisch an die Taverne angleichen | 13, 16 (done); finale menschliche Abnahme ausstehend |
| [20 (done)](.scratch/adventure-chat/issues/20-shared-object-actions.md) | Zwei Spieler ändern gemeinsame Objektzustände samt direkten atomaren Verknüpfungen | 09, 10, 15 (done); menschlich abgenommen am 12.11.2026 |
| [21 (ready-for-human)](.scratch/adventure-chat/issues/21-state-dependent-navigation.md) | Tür-/Stuhlhindernisse und laufende Wege folgen sicher dem Zustand | 20 |
| [22 (draft)](.scratch/adventure-chat/issues/22-verbs-and-interaction-editor.md) | Verben mit Objektsatz und vollständige Konfiguration im Browsereditor | 20, 21, 18; 06, 15, 17 (done) |
| [23 (draft)](.scratch/adventure-chat/issues/23-interactive-reference-examples.md) | Tür, Stuhl und Schalter/Tor als erreichbare gemeinsame Referenzdemo | 20, 21, 22, 19 |

Tickets 14–16 wurden am 04.11.2026 auf Benutzerauftrag als draft angelegt. Nur Planung beauftragt; andere Agenten übernehmen die spätere Umsetzung nach Bestätigung von Umfang und Testgrenzen. Verbindliche Reihenfolge laut anschließendem Benutzerauftrag: 14 → 15 → 16 → 13. Keine automatische Parallelbeauftragung. Formatversionen fortlaufend anhand des tatsächlichen Stands wählen; Ticket 13 ergänzt seine Demo-Anforderungen erst nach der neuen Referenzwelt.

Die Tickets liegen einzeln unter [.scratch/adventure-chat/issues](.scratch/adventure-chat/issues/).
Die Reihenfolge 01–07 ist die empfohlene serielle Umsetzung. Ticket 06 hängt fachlich nur von 02 ab; das ist keine automatische Beauftragung paralleler Agenten.

## Neue Planung 20–23 · 10.11.2026

Die vier Erweiterungspakete aus SPEC.md sind angelegt; Ticket 20 wurde separat umgesetzt und abgenommen, Ticket 21 danach separat einschließlich Sicherheitsregel zur Umsetzung bestätigt. Serielle Reihenfolge: **20 → 21 → 22 → 23**. 22/23 warten auf die menschliche Abnahme ihrer jeweiligen Vorgänger. Zusätzlich benötigt 22 die noch offene Abnahme von 18; 23 die noch offene Abnahme von 19. Kein anderes Paket automatisch beginnen und keine parallele Umsetzung ableiten.

20 liefert nach Benutzerfeedback bereits eine erste klassische Verbenleiste mit Objektklick, Zustandsdarstellung und Mehrspielerkommandos; 21 ergänzt sichere Navigation; 22 vervollständigt das Autorenwerkzeug und die spätere Interaktionsintegration; 23 integriert erst danach Referenzinhalte. Datenverträge: 1.6.0 für Objektaktionen, 1.7.0 für mehrere Bedienpunkte und 1.8.0 für Ticket 21, ohne Änderungen eingefrorener Bündel. Die lokale Definition von Einschließen und die übrigen Paketentscheidungen für 21 wurden am 12.11.2026 vor Implementierungsbeginn ausdrücklich bestätigt; Details im Ticket.

## Meilensteine
M1 nach 02: Bewegung und Geometrie funktionieren an einem Beispiel.
M2 nach 05: Begehbarer Chat mit zwei handgeschriebenen Räumen.
M3 nach 07: dieselbe Erfahrung mit selbst im Editor erstellten Räumen.

## Statusmodell
draft → ready-for-agent → in-progress → ready-for-human → done.
blocked zusätzlich bei konkretem Hindernis mit Begründung. ready-for-agent erst nach Bestätigung von Paketumfang und Testgrenzen; done erst nach menschlicher Abnahme.
Ein technischer Review ersetzt keine Produktabnahme. Keine offenen Entscheidungen stillschweigend als beschlossen behandeln.
