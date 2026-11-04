# Gemeinsame Begriffe
| Begriff | Bedeutung |
| --- | --- |
| World / Welt | Sammlung verbundener Raumdefinitionen |
| Room / Raum | Hintergrund, begehbare Flächen, Spawns und Ausgänge |
| Room definition / Raumdefinition | Gespeicherte, versionierte Autorendaten |
| Room state / Raumzustand | Flüchtige Teilnehmer und bestätigte Bewegungen |
| Actor / Figur | Sichtbare Repräsentation einer Teilnehmersitzung |
| Session / Sitzung | Serverseitig gebundene aktuelle Teilnahme |
| Spawn | Benannter gültiger Startpunkt in einem Raum |
| Exit / Ausgang | Erreichbarer Übergang zu Raum und Spawn |
| Walkable area / begehbare Fläche | Erlaubter Bereich für den Figurenfußpunkt |
| Footpoint / Fußpunkt | Positionsanker der Figur auf dem Boden |
| Route / Weg | Vom Server bestätigte begehbare Strecke |
| Snapshot | Vollständiger aktueller Zustand für Eintritt/Resynchronisierung |
| Room chat / Raumchat | Nachrichten ausschließlich für aktuelle Raumteilnehmer |
| Editor / Raumeditor | Lokales Browserwerkzeug für Raumdefinitionen |
| Ticket / Arbeitspaket | Abnehmbarer vertikaler Ausschnitt der Funktion |

## Geplante Welt- und Darstellungsbegriffe
| Begriff | Bedeutung |
| --- | --- |
| Weltpaket | Manifest, Räume und Ressourcen einer unabhängig bereitgestellten Welt |
| Welt-ID | Stabile Identität und Geltungsbereich einer Welt |
| Start-Spawn | Eintrittspunkt im Startraum einer Welt |
| Formatversion | SemVer-Version des öffentlichen Autorenvertrags und seiner Interpretation |
| Inhaltsversion | SemVer-Version einer konkreten Weltveröffentlichung |
| Schema-Bündel | Lokale, unveränderliche Schemas einer veröffentlichten Formatversion |
| Abwärtskompatibilität | Erhalt zugesicherter Bedeutung unterstützter älterer Welten auf neueren Servern |
| Legacy-Raumformat | Bestehendes Raumformat mit ganzzahligem `version: 1` |
| Draufsicht | Raumdarstellung mit von oben sichtbarer Figur |
| Figurengröße | Feste Rahmenhöhe in logischen Raumpixeln (16–128, Standard 64), Anker unten mittig am Fußpunkt |
| Bildschirmskalierung | Anpassung der Darstellung an den verfügbaren Bildschirm |
| Laufgeschwindigkeit | Serverbestimmte Bewegungsgeschwindigkeit, unabhängig von der optischen Größe |
| Kameraoffset | Lokale Verschiebung des sichtbaren Raumausschnitts |
| Tiefenfaktor | Positionsabhängiger Faktor für die Figurengröße mit festem Fußpunkt |
