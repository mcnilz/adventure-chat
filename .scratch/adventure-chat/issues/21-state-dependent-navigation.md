# 21: Zustandsabhängige Hindernisse und sichere Durchgänge

**What to build:** Offene Türen geben Wege frei, geschlossene Türen sperren sie. Ein Stuhl bleibt ein Hindernis und verschiebt seine Hindernisfläche zwischen zwei festen Positionen. Server, Spiel und gemeinsame Vorschau berücksichtigen dieselbe aktuelle Geometrie, auch für bereits laufende Wege.

**Blocked by:** [20](20-shared-object-actions.md) muss done sein; 02, 05, 11–13 und 15 sind done.

**Status:** done

## Auftrag und Freigabegrenze

Planung am 10.11.2026 bestätigt; Paketumfang, Sicherheitsdefinition, Eintritts-/Stoppregel, Format 1.8.0 und öffentliche Testgrenzen am 12.11.2026 separat mit „ja“ zur Implementierung freigegeben. Umsetzung geprüft und am 12.11.2026 mit „funktioniert“ menschlich abgenommen. [SPEC](../../../SPEC.md), [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md).

## Umfang und bestätigte Sicherheitsregel

- Optionale, ausdrücklich konfigurierte Hindernispolygone je Objektzustand sowie zustandsgebundene Freigabe bestehender Ausgänge. Keine Kollision aus PNG-Alpha ableiten. Bestehende begehbare Polygone bleiben die Grundfläche; wirksame Hindernisse werden zusätzlich ausgeschlossen.
- Zustandsposition und Hindernisfläche verwenden dieselben Raumkoordinaten beziehungsweise eine eindeutig dokumentierte lokale Transformation. Beim Stuhl wandert die Fläche mit; bei geöffneter Tür entfällt die konfigurierte Sperre. Zwei Endpositionen, kein kontinuierliches Schieben oder Physikmodell.
- Vor einer Aktion den gesamten resultierenden Zustand einschließlich aller direkten Verknüpfungen prüfen. Sicherheitsfehler verwerfen das gesamte Ergebnis einschließlich Schalterzustand. Positionen aller betroffenen Figuren zum selben serverseitigen Zeitpunkt aus ihren bestätigten Wegen bestimmen.
- **Überdecken:** Der aktuelle Figurenfußpunkt wäre im resultierenden Navigationszustand nicht mehr gültig, insbesondere innerhalb oder auf dem gesperrten Rand einer neuen Hindernisfläche. Maßgeblich bleibt der bisherige Fußpunktvertrag, keine Prüfung des optischen Körpers und kein neuer Figurenradius. Randtoleranzen vor Umsetzung dokumentieren.
- **Einschließen:** Für jede anwesende Figur in einem betroffenen Raum die vor der Änderung erreichbaren Sicherheitsanker bestimmen: gültige benannte Spawns und freigegebene, tatsächlich nutzbare Ausgänge mit gültigem Zielspawn. Nach der Änderung muss mindestens einer dieser Anker weiterhin erreichbar und gültig sein. Andernfalls ablehnen. Das gilt auch beim Schließen eines Ausgangs oder einer direkt verknüpften Sperre.
- Diese Prüfung bleibt räumlich auf betroffene Räume und die Gültigkeit direkt betroffener Übergangsziele begrenzt. Sie beweist keine globale Lösbarkeit der Welt oder zukünftiger Aktionsfolgen. Hat ein Raum vor der Aktion keinen erreichbaren Sicherheitsanker, verständlich ablehnen statt eine Rettungsroute zu erfinden. Geeignete Autorenanker sind daher Voraussetzung; ein zusätzlicher Spawn darf nicht still als Teleport angeboten werden.
- Bewusste konservative Folge: Eine Tür hinter einer Figur kann nicht geschlossen werden, wenn dadurch deren letzter erreichbarer bisheriger Sicherheitsanker verloren geht. Die konkrete Ankerregel vor Paketfreigabe anhand einer Tür-/Stuhlszene bestätigen; andere Definitionen wären eine bewusste Planänderung.
- Nach erfolgreicher Änderung laufende Wege gegen die neue Geometrie ab der aktuellen Position prüfen. Weiter gültige Wege dürfen weiterlaufen; ungültige Wege serverseitig zum bisherigen Ziel neu berechnen oder sicher am aktuellen gültigen Punkt stoppen, falls das Ziel unerreichbar ist. Kein Durchlaufen der neuen Sperre und kein Teleport. Zustands- und Wegänderung konsistent bestätigen; Navigation darf zwischenzeitlich keine alte Sperrversion benutzen.

## Acceptance criteria

- [x] Zwei Teilnehmer können Tür und Stuhl bedienen; Wege und Ausgänge folgen dem bestätigten Objektzustand. Ein visuell offener Durchgang mit weiterhin fester Boden-Aussparung wird bei Inhaltsprüfung/Abnahme erkannt: die Grundfläche muss den geöffneten Weg tatsächlich erlauben.
- [x] Stuhl blockiert in beiden Endpositionen an der jeweils richtigen Stelle. Öffnen/Schließen eines Durchgangs wirkt auch beim Klick auf einen Ausgang und auf Übergänge zu einem nun blockierten Zielspawn. Eintritt und Reconnect erzeugen keine Figur auf einem unzulässigen Spawn; vor Freigabe eine begrenzte Regel für Ablehnung/alternativen gültigen benannten Spawn festlegen, kein freies Versetzen.
- [x] Aktion über dem aktuellen Fußpunkt einer stehenden oder laufenden Figur wird vollständig abgewiesen und verständlich begründet. Dasselbe gilt bei Verlust des letzten Sicherheitsankers nach der bestätigten Definition. Ein noch erreichbarer Anker erlaubt die Änderung. Figuren bleiben untereinander nicht kollidierend.
- [x] Bei gleichzeitigem Aktions-, Bewegungs- und Übergangsbefehl entscheidet dieselbe serverseitige Ordnung wie in Ticket 20. Mehrfachwirkungen werden gemeinsam geprüft, auch für Figuren in anderen direkt betroffenen Räumen; fehlgeschlagene Sicherheitsprüfung ändert weder Objekte noch Wege.
- [x] Eine während des Laufens entstehende Sperre führt zu gültigem Umweg oder sicherem Stoppen mit Rückmeldung. Freigabe ermöglicht einen neuen Zielbefehl. Keine automatische Fortsetzung zuvor gestoppter Befehle ohne ausdrückliche Regel.
- [x] Inhaltsprüfung erkennt ungültige Polygone, Zustands-/Ausgangsreferenzen und ungültige Anfangsgeometrie/Spawns. Grenzen für Polygone, Zustände und direkte Wirkungen halten Prüfung endlich; kein allgemeiner Rätsel- oder Weltlösungsprüfer.
- [x] Gemeinsame Darstellung/Vorschau zeigt den richtigen Zustand; die Vorschau verwendet dieselbe autoritative Navigations-/Sicherheitsregel statt einer zweiten Browser-Wegfindung. Vollständige Autorenbedienung folgt in 22.
- [x] Nach 1.7.0 für mehrere Objekt-Bedienpunkte geplantes neues Format 1.8.0: additive optionale Geometrie-/Sperrregeln, ohne Angaben unveränderte Navigation aller alten Formate. Explizite Unterstützung erweitern, alte Schemas/Teilschemas unverändert lassen. Unveränderte ältere Referenzfixtures einschließlich enger Durchgänge nachweisen.
- [x] Checks, Review, Randtoleranzen, Grenzen und menschliche Abnahme dokumentieren; ready-for-human erst nach Umsetzung und Prüfung.

## Bestätigte Testgrenzen

Öffentliche Inhaltsgrenze, echte serverseitige Bewegungs-/Aktions-/Übergangskommandos und Browserabläufe. Fälle: Tür offen/geschlossen; Stuhl an beiden Positionen; blockierter Spawn; Fußpunkt auf Rand/innerhalb/knapp außerhalb der Sperre; laufende Figur zum Aktionszeitpunkt; ein verbleibender Anker, kein verbleibender Anker und bereits ankerloser Ausgangszustand; direkt verknüpfte Mehrraumänderung mit vollständigem Rollback; konkurrierende Bewegung/Schließung; Route mit möglichem Umweg beziehungsweise unerreichbarem Ziel; zwei Browser und späterer Eintritt. Statische Hindernisse, enge Durchgänge, Scrollen/Tiefe und alle alten Fixtures als Regression.

Keine Tests privater Polygonzerlegung oder Navigationsknoten. Falls eine neue öffentliche Geometriegrenze nötig erscheint, begründen und gesondert bestätigen. Automatisierung beweist die definierte endliche Sicherheitsregel, keine beliebige räumliche oder weltweite Unmöglichkeit des Einschließens. Bediengefühl und verständliche Meldungen menschlich abnehmen. Typcheck, Clientbuild und vollständige einschlägige Suite am Paketende.

## Detailentscheidungen der Paketfreigabe

Lokale Ankerregel, Fußpunkt ohne zusätzlichen Radius, Ablehnung gesperrter vorgesehener Spawns ohne Ersatz und Stopp mit lokaler Sprechblase ohne automatische Fortsetzung am 12.11.2026 bestätigt. Konkretes Polygonmodell, Randtoleranz, Datenlimits und 1.8.0-Vertrag im Umsetzungsstand unten dokumentiert. Ausschließlich Endpositionen; keine Prüfung eines kontinuierlich durchfahrenen Stuhlvolumens und keine globale Weltlösbarkeitsgarantie.

## Stand / Abnahme

12.11.2026: Benutzer bestätigt die bereitgestellte Umsetzung mit „funktioniert“. Ticket 21 ist menschlich abgenommen und done. Implementierungscommit bfa5189. Ausschließlich Status-/Abnahmedokumentation geändert; kein Produkt- oder Formatwechsel und kein neuer Funktionslauf erforderlich. Ticket 18/19 bleiben ready-for-human, 22/23 draft.

### Umsetzung und konkretisierter Vertrag (12.11.2026)

- Format 1.8.0 ergänzt ausschließlich optionale `ObjectState.obstacles` und `blockedExits`; absolute Raumkoordinaten ohne implizite Bildtransformation. Neue synthetische Welt `tests/Fixtures/navigation`, keine Überarbeitung der Referenzwelt aus 19/23. Die roten PNG-Muster sind Kopien eigener Testbilder aus foreground.
- Maximal vier Polygone je Zustand mit je 3–16 Punkten, höchstens 256 deklarierte dynamische Vertices über alle Zustände eines Raums; höchstens 32 unterschiedliche eigene Ausgangsreferenzen je Zustand. Bestehende Grenzen (128 Objekte, 32 Zustände, 64 Aktionen, 32 direkte Wirkungen) bleiben erhalten.
- Dynamische Hindernisse einschließlich Rand sind gesperrt, Punkt-/Segmentabstand bis einschließlich `1e-6` Raumpixel gilt als Randkontakt. Alte Polygone bleiben unverändert interpretiert; vorhandene 4px-Ecksichtprüfung und 14px-Navigationsknoten bleiben erhalten. Kein zusätzlicher Figurenradius.
- Vor Übernahme gemeinsame Projektion aller Figurenwege auf denselben Serverzeitpunkt; gesamte direkte Wirkung einschließlich anderer Räume wird bei ungültigem Fußpunkt oder Verlust sämtlicher zuvor erreichbarer lokaler Anker verworfen. Auch Räume mit unmittelbar ungültig werdendem Übergangsziel werden geprüft. Bereits fehlender erreichbarer Anker führt zur Ablehnung. Keine globale Lösbarkeitsgarantie.
- Neue Wege, Ausgangsbenutzung und Eintritt prüfen wirksame Geometrie/Spawns. Gesperrten vorgesehenen Spawn ohne Ersatz ablehnen. Weiterhin gültige Wege bleiben bestehen; ungültige werden zum bisherigen Ziel neu berechnet oder sicher am aktuellen Fußpunkt gestoppt, ohne automatische Fortsetzung.
- Jede Objektmeldung ersetzt auch den aktuellen Navigations-/Wegezustand vollständig. Das verhindert dauerhaften Verlust einer älteren Wegkorrektur, wenn eine neuere rein visuelle Aktion beim Browser zuerst eintrifft. Stoppmarker bleiben bis zum nächsten gültigen Bewegungsauftrag/Raumwechsel erhalten; lokale Meldungen werden anhand der Bewegungsrevision dedupliziert. Eintritt verwendet kanonische Raum-IDs, damit andere Großschreibung keine Figur aus der Sicherheitsprüfung ausschließt.
- Vorschau verwendet dieselben Navigations-/Aktions-/Sicherheitsregeln auf isolierten Raumdraft-/Weltkopien. Objektklick probiert die erste derzeit erlaubte Aktion aus und hält die Vorschaufigur zuvor an. Keine Änderung an Autorendaten oder Livewelt; vollständige Autoren- und Verbwahl folgt in 22.

### Bisherige Prüfbelege

**Abschluss:** Typcheck und Clientbuild erfolgreich; letzte vollständige Suite **328 bestanden (221 Server, 107 Browser), 0 Warnungen, 478.8s**. Keine Produktänderung nach diesem Lauf. Alle 25 neuen Server-/Inhaltsfälle und sechs Browserfälle enthalten. Standards: 0 offene Befunde; Spec: 0 offene Befunde nach Korrekturen und Nachprüfung. Markdown-Ziele und Diffprüfung erfolgreich. Status done; menschliche Endabnahme am 12.11.2026 mit „funktioniert“. Lokaler Implementierungscommit: `Implement state-dependent navigation and safe passages (ticket 21)`, kein Push.

- Türbewegung an öffentlicher Sitzungsgrenze zuerst rot (geschlossene Tür ließ Weg zu), nach Navigationsintegration grün. Fußpunkt-/Ankerablehnung zuerst rot, nach atomarer Vorprüfung grün. Format-1.8.0-Inhaltsprüfung zuerst rot (Bündel fehlte), danach grün.
- Browserintegration zuerst rot: Beim Umschreiben weltbezogener Bildadressen gingen Zustandshindernisse verloren. Übernahme von `obstacles`/`blockedExits` korrigiert, echte Browserabläufe grün.
- Unabhängiger Standardsreview: keine konkreten Befunde. Unabhängiger Spec-Review fand zwei P1-Befunde (überholte Navigationsmeldung, nichtkanonische Raum-ID). Beide über öffentliche Kommandos zuerst rot nachgewiesen und korrigiert; erneuter Spec- und Standardsreview ohne weitere konkrete Befunde. Browserprüfung mit absichtlich vertauschter SignalR-Zustellung grün. Keine parallele Implementierung durch Subagenten.
- Kompatibilitätsabgleich gegen `4d110681fde69ff3d60c03b055c63e4c65ea352b`: 192 vorhandene Schema-/Fixture-/Welt-/Raumdateien ohne Inhaltsänderung; 103 LFS-Bilder SHA256-bytegleich. 30 schon vorhandene CRLF-Textcheckouts gegenüber kanonischen Git-LF-Blobs berücksichtigt, nicht umgeschrieben. Bericht: `../evidence/21/compatibility.json`.
- Typcheck/Clientbuild, 25 gezielte Server-/Inhaltsfälle und sechs Browserfälle grün. Erster abgeschlossener Gesamtlauf: 327 bestanden (221 Server, 106 Browser), 0 Warnungen, 455.4s; der aus 20 bekannte Editorvorschau-Timeout trat nicht auf. Zusätzlicher Browserfall für gedrosselte Animation zuerst rot (trotz abgeschlossenem Serverweg noch laufender Client am alten Weg), nach Übernahme gleicher Bewegungsrevision ausschließlich aus neuerer Objektmeldung grün. Erneuter Spec-Review ohne konkrete Regression. Reguläre Bewegungsmeldungen benötigen weiterhin eine höhere Revision; überholte Wege bleiben gesperrt. Abschließender Gesamtlauf einschließlich dieses sechsten Browserfalls: 328 bestanden, 0 Warnungen (siehe Abschluss oben).
- Ein erster Gesamtlauf wurde wegen Reviewkorrekturen abgebrochen. Ein paralleler zusätzlicher Browserbuild scheiterte an der Dateisperre des laufenden Testhosts (kein Produktfehler); ein isolierter Lauf mit eingeschränkten Browserrechten lief in ein 90s-Umgebungstimeout. Nach Ausführung mit nötigen lokalen Browserrechten gelang der beabsichtigte Rot/Grün-Nachweis. Keine dieser Umgebungsstörungen als Produktabnahme gewertet.

### Lokale Abnahme vorbereiten

Separate geprüfte Programmkopie und Welt: `.scratch/adventure-chat/evidence/21/app-reviewed` und `world`, URL `http://127.0.0.1:55027/`, PID 40064. HTTP200 und Eintritt/Sichtprüfung im Browser bestätigt. Der erste ersetzte Vorschauprozess (PID 42604) wurde gezielt beendet; bestehende Abnahmeumgebungen 18/19 und der bisherige Editor wurden nicht verändert. Die neue Testwelt verwendet einfache rote Muster, keine endgültige Gestaltung. Momentaufnahmen: `closed.png`, `open.png`, `stopped.png` im Belegordner.

1. URL in zwei Tabs öffnen, jeweils eintreten. „Benutzen“ und die hohe rote Tür in der Mitte anklicken: Öffnen erlaubt den Weg auf die rechte Raumseite und zum Ausgang. Erneutes Benutzen schließt. Kleinen roten Stuhl links mit Ziehen/Drücken zwischen seinen Endpositionen bewegen; die Hindernisfläche muss folgen.
2. Bei offener Tür die zweite Figur in den Durchgang stellen: Schließen durch die erste Figur muss vollständig mit lokaler Fehlermeldung abgelehnt werden. Danach zweite Figur rechts starten lassen und zurück nach links schicken; noch vor dem Durchgang schließen: sicherer Stopp rechts mit lokaler Sprechblase, kein Raumchat. Erneutes Öffnen darf den gestoppten Auftrag nicht selbst fortsetzen.
3. `http://127.0.0.1:55027/editor.html?worldId=navigation` öffnen und Raumvorschau starten. Geschlossene Tür blockiert den Weg; Objektklick öffnet, danach Weg freigegeben. Vorschau verlassen: Draft und Livewelt bleiben unverändert. Sichtbare Gestaltung, Bediengefühl und Verständlichkeit der Meldungen menschlich prüfen.

Keine neuen Abhängigkeiten. SemVer: additive Minor-Erweiterung mit unveränderten Defaults und eigener ausdrücklicher Unterstützung; ältere Bündel und Fixtures bleiben erhalten. Menschliche Endabnahme am 12.11.2026 mit „funktioniert“ erfolgt.

12.11.2026: Benutzer bestätigt Paketvorschlag mit „ja“: Fußpunkt einschließlich Sperrrand, Erhalt mindestens eines zuvor erreichbaren lokalen Sicherheitsankers, blockierten vorgesehenen Eintritt/Übergang ablehnen ohne Ersatzspawn, ungültige laufende Wege zum bisherigen Ziel neu berechnen oder lokal mit Sprechblase stoppen, keine automatische Fortsetzung. Format 1.8.0 und die vorgeschlagenen öffentlichen Testgrenzen bestätigt. Implementierungsbasis `4d110681fde69ff3d60c03b055c63e4c65ea352b`. Ältere Schemas/Fixtures bleiben unverändert. Bestehende unversionierte Belege unter evidence/20 werden nicht verändert.

10.11.2026: draft, nur Planung. Keine Tests ausgeführt oder Sicherheitsdefinition als abgenommen dargestellt.
