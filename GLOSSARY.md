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

| Figurenkatalog | Optionales weltbezogenes Verzeichnis verfügbarer Figurengrafiken mit stabiler ID, Name, PNG und Quellraster |
| Defaultfigur | Explizit benannte Figur des Weltkatalogs für vorbelegte Auswahl und Editor-Vorschau |
| Quellraster | Framebreite und Framehöhe des PNG; unabhängig von der Zielhöhe im Raum |

| Raumobjekt | Grafisches PNG-Element mit stabiler ID, Position, Zielgröße und Tiefenlinie; unabhängig von Kollision |
| Tiefenlinie | Horizontale Raum-Y-Linie eines Objekts; Figuren dahinter werden von sichtbaren PNG-Pixeln verdeckt, bei gleicher Tiefe steht die Figur davor |

| Tiefenverlauf | Optionale hintere/vordere horizontale Bezugslinie mit je einem Faktor; linear interpoliert und außerhalb begrenzt |

## Geplante interaktive Raumobjekte (20–23)

**Interaktives Raumobjekt:** Ein Raumobjekt mit benannten Zuständen und erlaubten Aktionen. Ohne solche Definition bleibt ein Raumobjekt rein grafisch.

**Objektzustand:** Der aktuelle benannte Zustand eines interaktiven Raumobjekts, beispielsweise eine offene Tür oder ein zurückgeschobener Stuhl.

**Anfangszustand:** Der vom Autor festgelegte Objektzustand beim Start einer Welt.

**Objektaktion:** Eine für einen Ausgangszustand ausdrücklich erlaubte Handlung mit festgelegten Zustandswirkungen, etwa Öffnen oder Ziehen.

**Direkte Objektverknüpfung:** Die ausdrückliche Zuordnung einer Objektaktion zu Zustandswirkungen an einem anderen Objekt derselben Welt. Sie löst keine weiteren Aktionen aus.

**Zustandsabhängiges Hindernis:** Eine ausdrücklich definierte gesperrte Bodenfläche, deren Lage oder Wirksamkeit vom Objektzustand abhängt.

**Verbenleiste:** Die sichtbare Auswahl von Handlungen, die mit einem Raumobjekt zu einem Aktionssatz kombiniert werden.
