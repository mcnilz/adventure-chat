# 25 – Sichtbare Übergänge zwischen Objektzuständen konfigurieren

**What to build:** Autoren können einer Objektaktion einen sichtbaren Übergang zwischen zwei definierten Objektzuständen zuordnen. Verschiedene Bedienelemente können ausdrücklich auf dieselbe Zielinstanz wirken. Darstellung und erreichter Zustand sind für Besucher konsistent.

**Blocked by:** [21](21-state-dependent-navigation.md), [22](22-verbs-and-interaction-editor.md).

**Status:** ready-for-human

## Acceptance criteria

- [x] Ein Autor kann für eine zulässige Zustandsänderung einen einfachen sichtbaren Übergang konfigurieren und die Gegenrichtung separat festlegen. Dieselbe Konfiguration lässt sich auf ein anderes geeignetes Objekt übertragen.
- [x] Zwei getrennte Bedienelemente können die Zustandswerte desselben Zielobjekts über vorhandene direkte Verknüpfungen setzen. Zwei Browser stimmen nach der Aktion, bei konkurrierender Bedienung und bei späterem Eintritt im gültigen Zielzustand überein.
- [x] Die Darstellung einer laufenden oder abgelehnten Änderung hat ein definiertes Ergebnis. Für Änderungen der Begehbarkeit gelten die vorhandenen Navigations- und Sicherheitsregeln.
- [x] Ein erreichbares Beispiel in der Referenzwelt zeigt Hin- und Rückrichtung. Eine Tischplatte fährt über zwei Tasten sichtbar hoch und herunter. Ein zweites geeignetes Objekt in einer Arbeitskopie belegt die Wiederverwendung.

## Bestätigte Entscheidungen und Testgrenzen

Am 20.11.2026 ausdrücklich zur Umsetzung freigegeben. Im Raumeditor lässt sich eine kurze Bewegung zwischen festen Positionen je Richtung konfigurieren. Die Werkstatt erhält zwei Hubtasten für dieselbe Tischplatte. Eine Arbeitskopie zeigt die Wiederverwendung am Stuhl.

Nach erfolgreicher Sicherheitsprüfung übernimmt der Server den Zielzustand und seine Navigation sofort. Während der Bewegung werden weitere Zustandsänderungen desselben Objekts abgelehnt. Bei konkurrierender Bedienung entscheidet die Serverreihenfolge. Abgelehnte Aktionen starten keine Animation. Eintritt während und nach der Bewegung verwendet den Serverzustand und gegebenenfalls den verstrichenen Anteil.

Geprüft werden Inhalte laden und speichern, gültige und ungültige Daten, ältere unveränderte Fixtures sowie echte Browser für beide Richtungen, konkurrierende Bedienung, Ablehnung und späteren Eintritt. Editorvorschau verwendet dieselbe Darstellung. Die Lesbarkeit der Bewegung bleibt menschliche Sichtabnahme.

## Umsetzung und Kompatibilität

Übergänge stehen am jeweiligen Zielobjekt als gerichtete Zustandspaare. Eine Objektaktion startet den passenden Übergang an sich selbst oder über ihre direkte Verknüpfung an einem anderen Objekt. Die Darstellung bewegt ein gleich großes PNG zwischen zwei Positionen. Dauer und Gegenrichtung sind getrennt konfigurierbar. Die Werkstatt verwendet 1800 ms zum Heben und 1400 ms zum Senken. Der Browsertest konfiguriert und prüft dieselbe Funktion zusätzlich am Stuhl einer temporären Arbeitskopie der Navigations-Testwelt.

Weltformat **1.9.0** ist eine Minor-Erweiterung mit optionalen Angaben. Bestehende Objekte behalten ohne Übergänge die sofortige Darstellung. Alte Welten bleiben ohne ausdrücklich gewählten Formatwechsel schreibbar. Alle 112 geprüften Dateien der älteren Schemata, Testfixtures und mitgelieferten Altwelten sind unverändert. Die Referenzwelt hat Inhaltsversion **5.0.0**, weil das neue Gestell eine zuvor begehbare Stellfläche belegt. Keine neuen Abhängigkeiten. Die genaue visuelle Phase kann sich bei verzögerter Übertragung zwischen Browsern unterscheiden. Der Zielzustand und seine Geometrie bleiben serverseitig eindeutig.

## Prüfbelege am 20.11.2026

- Die Tests für den neuen YAML-Vertrag und die serverseitige Sperre sind zuerst fehlgeschlagen und nach der jeweiligen Umsetzung bestanden. Die Inhaltsprüfung umfasst beide Richtungen, ungültige Zustände und Dauern sowie Ablehnung durch Format 1.8.0. Die Serverprüfung deckt sofort wirksame Geometrie, Ablehnung ohne Animation, Eintritt während der Bewegung und die getrennten Dauern ab.
- Die beiden Browsertests prüfen die Tasten, Zwischen- und Endlagen im Canvas, die während der Fahrt abgelehnte Gegenaktion, gleichzeitige Bedienung und Eintritt während und nach der Fahrt. Der Editorablauf prüft expliziten Formatwechsel, einen ungültigen Dauerwert, Speichern, Neuladen und sichtbare Hin- und Rückfahrt am Stuhl.
- Typcheck und Clientbuild bestanden. Abschließende Vollsuite mit **348 bestandenen Tests** (234 Server, 114 Browser), ohne übersprungene Tests und ohne Buildwarnungen. Ergebnisse unter `.scratch/adventure-chat/evidence/25/test-results`.
- Unabhängige Prüfungen zu Standards und Anforderungen sind nach den Korrekturen ohne offene Befunde. Das gemeinsame Zeichnen setzt laufende Übergänge bei weiteren Zustandsmeldungen fort, und der Animationszustand liegt getrennt von den Netzwerkdaten.
- Lokaler Umsetzungscommit `Animate configured object state transitions (ticket 25)`. Menschliche Sichtabnahme ausstehend.

## Manuelle Abnahme

1. [Lokale Abnahmekopie](http://127.0.0.1:56682/) in zwei Browserfenstern öffnen. Von der Lounge durch die rechte Tür in den Garten gehen. Die Holztür im efeubewachsenen Steinbogen führt zur Werkstatt.
2. Rechts an der Rückwand stehen die grüne Auf- und die rote Ab-Taste. Je eine Figur mit **Gehe zu** vor eine Taste stellen. Mit **Benutzen** erst auf die grüne Taste klicken. Beide Fenster sollen dieselbe Platte hochfahren sehen. Bei sofortiger Gegenbedienung bleibt der Zielzustand erhalten und die Gegenaktion meldet, dass sich das Objekt noch bewegt. Nach Erreichen der oberen Stellung mit der roten Taste senken.
3. Im [Editor der Abnahmekopie](http://127.0.0.1:56682/editor.html?worldId=reference) **Werkstatt** und das Objekt `lift-plate` wählen. Unter **Sichtbare Übergänge** die zwei Richtungen ansehen. Optional die Dauer einer Richtung in der Kopie ändern, speichern und nach dem Neuladen in der Vorschau mit den beiden Tasten prüfen.

Spieleinstieg und Editorstatus der getrennten Abnahmekopie wurden über HTTP geprüft. Laufende Abnahmekopie: Prozess 11696, Verzeichnis `.scratch/adventure-chat/evidence/25/acceptance-05bc7702`. Pixelbelege der beiden Endlagen liegen unter `.scratch/adventure-chat/evidence/25`.
