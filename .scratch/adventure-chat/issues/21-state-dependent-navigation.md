# 21: Zustandsabhängige Hindernisse und sichere Durchgänge

**What to build:** Offene Türen geben Wege frei, geschlossene Türen sperren sie. Ein Stuhl bleibt ein Hindernis und verschiebt seine Hindernisfläche zwischen zwei festen Positionen. Server, Spiel und gemeinsame Vorschau berücksichtigen dieselbe aktuelle Geometrie, auch für bereits laufende Wege.

**Blocked by:** [20](20-shared-object-actions.md) muss done sein; 02, 05, 11–13 und 15 sind done.

**Status:** draft

## Auftrag und Freigabegrenze

Nur Planung am 10.11.2026 bestätigt; kein Implementierungsauftrag. Bestätigt ist die konservative Richtung: Eine Änderung verständlich ablehnen, wenn sie eine Figur überdeckt oder einschließt. Die folgende technische Eingrenzung ist ein Vorschlag für die separate Paketfreigabe, keine bereits bestätigte Definition. [SPEC](../../../SPEC.md), [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md).

## Umfang und vorgeschlagene Sicherheitsregel

- Optionale, ausdrücklich konfigurierte Hindernispolygone je Objektzustand sowie zustandsgebundene Freigabe bestehender Ausgänge. Keine Kollision aus PNG-Alpha ableiten. Bestehende begehbare Polygone bleiben die Grundfläche; wirksame Hindernisse werden zusätzlich ausgeschlossen.
- Zustandsposition und Hindernisfläche verwenden dieselben Raumkoordinaten beziehungsweise eine eindeutig dokumentierte lokale Transformation. Beim Stuhl wandert die Fläche mit; bei geöffneter Tür entfällt die konfigurierte Sperre. Zwei Endpositionen, kein kontinuierliches Schieben oder Physikmodell.
- Vor einer Aktion den gesamten resultierenden Zustand einschließlich aller direkten Verknüpfungen prüfen. Sicherheitsfehler verwerfen das gesamte Ergebnis einschließlich Schalterzustand. Positionen aller betroffenen Figuren zum selben serverseitigen Zeitpunkt aus ihren bestätigten Wegen bestimmen.
- **Überdecken (Vorschlag):** Der aktuelle Figurenfußpunkt wäre im resultierenden Navigationszustand nicht mehr gültig, insbesondere innerhalb oder auf dem gesperrten Rand einer neuen Hindernisfläche. Maßgeblich bleibt der bisherige Fußpunktvertrag, keine Prüfung des optischen Körpers und kein neuer Figurenradius. Randtoleranzen vor Umsetzung dokumentieren.
- **Einschließen (Vorschlag):** Für jede anwesende Figur in einem betroffenen Raum die vor der Änderung erreichbaren Sicherheitsanker bestimmen: gültige benannte Spawns und freigegebene, tatsächlich nutzbare Ausgänge mit gültigem Zielspawn. Nach der Änderung muss mindestens einer dieser Anker weiterhin erreichbar und gültig sein. Andernfalls ablehnen. Das gilt auch beim Schließen eines Ausgangs oder einer direkt verknüpften Sperre.
- Diese Prüfung bleibt räumlich auf betroffene Räume und die Gültigkeit direkt betroffener Übergangsziele begrenzt. Sie beweist keine globale Lösbarkeit der Welt oder zukünftiger Aktionsfolgen. Hat ein Raum vor der Aktion keinen erreichbaren Sicherheitsanker, verständlich ablehnen statt eine Rettungsroute zu erfinden. Geeignete Autorenanker sind daher Voraussetzung; ein zusätzlicher Spawn darf nicht still als Teleport angeboten werden.
- Bewusste konservative Folge: Eine Tür hinter einer Figur kann nicht geschlossen werden, wenn dadurch deren letzter erreichbarer bisheriger Sicherheitsanker verloren geht. Die konkrete Ankerregel vor Paketfreigabe anhand einer Tür-/Stuhlszene bestätigen; andere Definitionen wären eine bewusste Planänderung.
- Nach erfolgreicher Änderung laufende Wege gegen die neue Geometrie ab der aktuellen Position prüfen. Weiter gültige Wege dürfen weiterlaufen; ungültige Wege serverseitig zum bisherigen Ziel neu berechnen oder sicher am aktuellen gültigen Punkt stoppen, falls das Ziel unerreichbar ist. Kein Durchlaufen der neuen Sperre und kein Teleport. Zustands- und Wegänderung konsistent bestätigen; Navigation darf zwischenzeitlich keine alte Sperrversion benutzen.

## Acceptance criteria

- [ ] Zwei Teilnehmer können Tür und Stuhl bedienen; Wege und Ausgänge folgen dem bestätigten Objektzustand. Ein visuell offener Durchgang mit weiterhin fester Boden-Aussparung wird bei Inhaltsprüfung/Abnahme erkannt: die Grundfläche muss den geöffneten Weg tatsächlich erlauben.
- [ ] Stuhl blockiert in beiden Endpositionen an der jeweils richtigen Stelle. Öffnen/Schließen eines Durchgangs wirkt auch beim Klick auf einen Ausgang und auf Übergänge zu einem nun blockierten Zielspawn. Eintritt und Reconnect erzeugen keine Figur auf einem unzulässigen Spawn; vor Freigabe eine begrenzte Regel für Ablehnung/alternativen gültigen benannten Spawn festlegen, kein freies Versetzen.
- [ ] Aktion über dem aktuellen Fußpunkt einer stehenden oder laufenden Figur wird vollständig abgewiesen und verständlich begründet. Dasselbe gilt bei Verlust des letzten Sicherheitsankers nach der bestätigten Definition. Ein noch erreichbarer Anker erlaubt die Änderung. Figuren bleiben untereinander nicht kollidierend.
- [ ] Bei gleichzeitigem Aktions-, Bewegungs- und Übergangsbefehl entscheidet dieselbe serverseitige Ordnung wie in Ticket 20. Mehrfachwirkungen werden gemeinsam geprüft, auch für Figuren in anderen direkt betroffenen Räumen; fehlgeschlagene Sicherheitsprüfung ändert weder Objekte noch Wege.
- [ ] Eine während des Laufens entstehende Sperre führt zu gültigem Umweg oder sicherem Stoppen mit Rückmeldung. Freigabe ermöglicht einen neuen Zielbefehl. Keine automatische Fortsetzung zuvor gestoppter Befehle ohne ausdrückliche Regel.
- [ ] Inhaltsprüfung erkennt ungültige Polygone, Zustands-/Ausgangsreferenzen und ungültige Anfangsgeometrie/Spawns. Grenzen für Polygone, Zustände und direkte Wirkungen halten Prüfung endlich; kein allgemeiner Rätsel- oder Weltlösungsprüfer.
- [ ] Gemeinsame Darstellung/Vorschau zeigt den richtigen Zustand; die Vorschau verwendet dieselbe autoritative Navigations-/Sicherheitsregel statt einer zweiten Browser-Wegfindung. Vollständige Autorenbedienung folgt in 22.
- [ ] Nach separat veröffentlichtem 1.6.0 geplantes neues Format 1.7.0: additive optionale Geometrie-/Sperrregeln, ohne Angaben unveränderte Navigation aller alten Formate. Explizite Unterstützung erweitern, alte Schemas/Teilschemas unverändert lassen. Unveränderte ältere Referenzfixtures einschließlich enger Durchgänge nachweisen. Falls die Planung vor Veröffentlichung zusammengelegt werden soll, Versionsfolge ausdrücklich neu entscheiden, kein veröffentlichtes 1.6.0 ergänzen.
- [ ] Checks, Review, Randtoleranzen, Grenzen und menschliche Abnahme dokumentieren; ready-for-human erst nach Umsetzung und Prüfung.

## Vorgeschlagene Testgrenzen – vor Implementierung bestätigen

Öffentliche Inhaltsgrenze, echte serverseitige Bewegungs-/Aktions-/Übergangskommandos und Browserabläufe. Fälle: Tür offen/geschlossen; Stuhl an beiden Positionen; blockierter Spawn; Fußpunkt auf Rand/innerhalb/knapp außerhalb der Sperre; laufende Figur zum Aktionszeitpunkt; ein verbleibender Anker, kein verbleibender Anker und bereits ankerloser Ausgangszustand; direkt verknüpfte Mehrraumänderung mit vollständigem Rollback; konkurrierende Bewegung/Schließung; Route mit möglichem Umweg beziehungsweise unerreichbarem Ziel; zwei Browser und späterer Eintritt. Statische Hindernisse, enge Durchgänge, Scrollen/Tiefe und alle alten Fixtures als Regression.

Keine Tests privater Polygonzerlegung oder Navigationsknoten. Falls eine neue öffentliche Geometriegrenze nötig erscheint, begründen und gesondert bestätigen. Automatisierung beweist die definierte endliche Sicherheitsregel, keine beliebige räumliche oder weltweite Unmöglichkeit des Einschließens. Bediengefühl und verständliche Meldungen menschlich abnehmen. Typcheck, Clientbuild und vollständige einschlägige Suite am Paketende.

## Offene Detailentscheidungen vor Paketfreigabe

Ankerregel samt lokalem Geltungsbereich, Randtoleranzen/Polygonmodell, gültige Zustandsgeometrie an Spawn und Ausgang, Eintritt bei vorübergehend gesperrtem Start sowie genaue Rückmeldung bei Stoppen bestätigen. Endpositionen sind bestätigt; keine Prüfung eines kontinuierlich durchfahrenen Stuhlvolumens versprechen. Konkrete Datenlimits und Formatnummer anhand des dann veröffentlichten Stands festlegen.

## Stand / Abnahme

10.11.2026: draft, nur Planung. Keine Tests ausgeführt oder Sicherheitsdefinition als abgenommen dargestellt.
