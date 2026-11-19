# 22: Verbenleiste und Browsereditor für interaktive Objekte

**What to build:** Spieler wählen klassische Adventure-Verben und kombinieren sie mit einem Objekt zu einem sichtbaren Satz wie „Öffne Tür“. Autoren konfigurieren dieselben Zustände, Aktionen, direkten Verknüpfungen und Hindernisse vollständig im Browsereditor und prüfen sie in der gemeinsamen Vorschau.

**Blocked by:** [20](20-shared-object-actions.md), [21](21-state-dependent-navigation.md), [18](18-responsive-game-scaling.md), 06, 15 und 17 sind done. Paketfreigabe am 19.11.2026 erfolgt.

**Status:** ready-for-human

## Auftrag und Freigabegrenze

Am 10.11.2026 als Paket aus Verbenbedienung und Editor zur Planung bestätigt. Keine Implementierungsfreigabe. [SPEC](../../../SPEC.md) und [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md) gelten; keine Inventar-, Aufheben- oder Dialogfunktion hinzufügen.

## Umfang

- LucasArts-artige Verbenleiste links unten unter dem Spielfeld als vereinbartes Vorbild. Bestehende Aktions-/Statuszeile weiterverwenden und erweitern: ausgewähltes Verb plus benannter Objektkandidat, ausführbarer Satz und verständliches Aktionsergebnis. Erste echte Bedienung aus 20 weiterführen, kein zweites Kommandosystem bauen.
- Öffnen/Schließen, Ziehen/Drücken sowie bisherige Geh-/Raumwechselbedienung integrieren. Schalter verwendet ein ausdrücklich konfiguriertes passendes Verb, zum Beispiel Drücken; kein allgemeines neues Rätselverb voraussetzen. Verb und Aktions-ID sind unterscheidbar, der Server prüft weiter die erlaubte Aktion.
- Objekte verlässlich treffen und bei Überlappung eindeutig auswählen. Nur im aktuellen Zustand erlaubte Kombinationen ausführen; zwischen Auswahl und Serverantwort geänderter Zustand führt zu verständlicher Ablehnung. Hover, Verbwahl und Chatsenden lösen keine ungewollte Bewegung aus.
- Editor ergänzt bestehende Raumobjektverwaltung: Zustände/Anfangszustand, feste Position und Darstellung, erlaubte Verb-Aktion, Zielzustand und direkte Zielobjekte, Hindernisflächen sowie Ausgangssperren. Auswahl vorhandener IDs statt fehleranfälliger Freitextverknüpfung, Fehler am betroffenen Feld anzeigen.
- Zustände in der Bearbeitung einzeln ansehen; gemeinsame interaktive Vorschau für Aktionen, verbundene Wirkungen, Navigation und sichere Ablehnung. Vorschau-Laufzeit von gespeicherten Anfangszuständen und live bespielter Welt isolieren. Vorschau neu starten setzt nur ihre eigenen Zustände zurück; Auswahl eines Vorschauzustands schreibt keine Autorendaten.
- Bestehende Ungespeichert-/Speicherregeln aus 17 erhalten. Löschen/Umbenennen referenzierter Zustände/Objekte/Ausgänge darf keine stillen kaputten Beziehungen speichern. Kein generischer Graph-/Skripteditor.

## Acceptance criteria

- [ ] „Öffne Tür“, „Schließe Tür“, „Ziehe Stuhl“ und „Drücke Stuhl“ lassen sich über Verbwahl und Objektklick ausführen; Satz und Rückmeldung sind eindeutig. Schalter/Tor funktionieren über dasselbe Verfahren. Nicht erlaubte Kombinationen und sicherheitsbedingte Ablehnung sind verständlich.
- [ ] Bedienreihenfolge, Standardverb und Rückkehr zu Gehen sind vor Umsetzung festgelegt. Verben haben lesbare Beschriftungen, sichtbare Auswahl und Tastaturfokus; bestehende Tastatur-/Chatbedienung bleibt verwendbar.
- [ ] Verben, Status, Spielfeld und Chat passen weiterhin gemeinsam proportional in die Fensterfläche gemäß 18. Klicks stimmen nach CSS-Skalierung und horizontaler Kamera; Objekt-/Türtreffer sind deterministisch. Keine Bewegungsbefehle durch DOM-Bedienelemente.
- [ ] Autor kann ohne manuelle YAML-Änderung Tür, Stuhl mit zwei Positionen und Schalter mit verbundenem Tor samt Hindernis-/Durchgangsregeln anlegen, prüfen, speichern und nach Neuladen unverändert bearbeiten.
- [ ] Ungültige Anfangszustände, fehlende/doppelte IDs, verwaiste Verknüpfungen und ungültige Geometrie werden über dieselbe Inhaltsprüfung abgewiesen. Fehlgeschlagenes Speichern erhält gültige Dateien und den bearbeiteten Entwurf. Verschiedene Welten mit gleichen Objekt-IDs bleiben getrennt.
- [ ] Gemeinsame Vorschau verwendet Spielrenderer und autoritative Aktions-/Navigationsregeln. Änderungen dort beeinflussen weder reale Spieler noch gespeicherte Anfangszustände; erneuter Vorschau-Start zeigt den deklarierten Anfangszustand.
- [ ] Editor erhält beim Speichern sämtliche unterstützten alten Felder/Defaults. Kein stilles Formatupgrade: Interaktionsdaten nur nach ausdrücklicher Wahl einer passenden bereits unterstützten Formatversion; nötige Aktualisierung des Manifests ist sichtbar und gegen Teilfehler abgesichert. Konkreten Ablauf vor Freigabe bestätigen.
- [ ] Keine weitere Formatversion vorgesehen, soweit ausschließlich Verträge aus 20/21 bedient werden. Bei neuem Feld/anderer Interpretation Kompatibilitätsprüfung erneut durchführen und neues Bündel planen; niemals bereits veröffentlichte Schemas erweitern. Ältere unveränderte Fixtures laden, darstellen und über Kopien verlustfrei speichern.
- [ ] Tatsächliche Checks, Sichtreview und konkrete menschliche Abnahmeschritte dokumentieren; danach ready-for-human. Bedien-/Gestaltungsabnahme bleibt menschlich.

## Vorgeschlagene Testgrenzen – vor Implementierung bestätigen

Öffentliche Browser-/Editorbedienung, Canvasdarstellung und vorhandene Lade-/Speicher-/SignalR-Grenzen. Zwei Spieler mit konkurrierenden Zuständen, Satzbildung und Fehlern; Verbwahl per Maus/Tastatur, überlappende Objekte, Scrollen und beide Skalierungsmodi. Editor-Roundtrip aller Interaktionsdaten in temporärer Weltkopie, Löschen referenzierter Elemente, ungespeicherter Entwurf während Speichern/Wechsel, Version bewusst wählen und fehlerhaften Wechsel ohne Teilüberschreiben prüfen. Vorschau-Reset/Isolation und gemeinsame Renderer-/Navigationswirkung nachweisen. Ältere Fixtures unverändert erhalten; keine neue Test-API.

Typcheck, Clientbuild und vollständige einschlägige Suite. Automatisierung ersetzt keine menschliche Beurteilung des klassischen Bediengefühls, der Verständlichkeit der Sätze und des Editors. Keine neue direkte Abhängigkeit vorausgesetzt.

## Vorgeschlagene Detailentscheidungen zur Paketfreigabe

- Vorhandene Leiste links unter dem Spielfeld um „Öffnen“ und „Schließen“ ergänzen und mit der Gesamtoberfläche skalieren. Verb per Maus oder fokussiertem Button wählen, dann Objekt anklicken. Die Statuszeile zeigt die kurze deutsche Befehlsform mit dem vom Autor vergebenen Namen, etwa „Öffne Tür“. „Gehe zu“ bleibt Standard und wird nach Erfolg oder Ablehnung wieder aktiv. Das Hinlaufen und die lokale Rückmeldung aus 20 bleiben erhalten.
- Der Editor bietet die unterstützten Verben als Auswahl an und belegt passende bestehende Aktionsnamen vor. Aktions-IDs bleiben unabhängig. Deutsche und englische Aktionsbezeichnungen werden nach dem bisherigen Zuordnungsprinzip erkannt. Bei mehreren passenden Aktionen bleibt die vorhandene Auswahl verfügbar.
- Bei Objektaktionen gewinnt das oberste sichtbare Objekt in Zeichenreihenfolge. Vollständig transparente Pixel geben den Treffer frei. Beim Gehen hat ein Ausgang Vorrang, wenn er an dieser Stelle nutzbar ist. Die Statuszeile zeigt den Kandidaten und kennzeichnet, ob die Aktion im aktuellen Zustand verfügbar ist.
- Für die vollständige Bearbeitung einschließlich Hindernissen und Ausgangssperren wählt der Autor bei einer älteren Welt ausdrücklich das unterstützte Format 1.8.0. Der Editor zeigt die geplante Änderung am Weltmanifest. Die Welt wird vor dem Schreiben gemeinsam validiert. Bei einem Fehler bleiben gültige Dateien und der Entwurf erhalten. Ältere Welten lassen sich in ihrer bisherigen Formatversion öffnen und speichern.
- Löschen und Umbenennen referenzierter Einträge bleiben gesperrt, bis ihre Verknüpfungen im Editor angepasst sind. Die Vorschau nutzt die gemeinsamen Spielregeln und startet bei jedem Neustart mit den Anfangszuständen des Entwurfs.

## Stand / Abnahme

19.11.2026: Der Benutzer hat Paketumfang, Detailentscheidungen und öffentliche Testgrenzen mit „ja“ freigegeben.

19.11.2026: Die ausdrückliche Abnahme von Ticket 18 hat den letzten Blocker aufgehoben.

12.11.2026: Benutzerfeedback zieht eine erste Verbenleiste mit direkter Objektwahl in die Nacharbeit zu Ticket 20 vor. Die vollständige Editor-, Vorschau- und Navigationsintegration dieses Tickets bleibt gesondert geplant; der bisherige Blocker 18 ist dadurch nicht abgenommen.

10.11.2026: draft, ausschließlich geplant. Vor Umsetzung Paketumfang und Testgrenzen bestätigen lassen.

## Umsetzung und Prüfnachweise

- Das Spiel nutzt die bestehende Verbenleiste und den autoritativen Aktionsablauf. Öffnen/Schließen, aktueller Objektsatz, Verfügbarkeit und Rückmeldung sind eingebunden. Der Editor bearbeitet vorhandene Aktionsnamen und IDs getrennt. Er weist neue widersprüchliche Zuordnungen an den betroffenen Feldern ab und erhält bestehende Zuordnungen über Namen oder IDs.
- Der Editor bearbeitet Anfangszustände, Darstellung, Bedienpunkte, Aktionen, direkte Wirkungen, Hindernisse und Ausgangssperren. „Raum prüfen“ und „Welt prüfen“ beziehen den Entwurf in die gemeinsame Weltvalidierung ein. Die Vorschau verwendet den gemeinsamen Renderer und dieselben Regeln für Wege, Ausgänge und Aktionen. Beim Neustart lädt sie wieder die Anfangszustände des Entwurfs.
- Das Upgrade auf 1.8.0 verwendet den veröffentlichten Vertrag. Es validiert den bestehenden Inhalt und den Entwurf, dann ersetzt es Manifest und bearbeitete Raumdatei. Ein fehlgeschlagener Dateiaustausch stellt das bisherige Manifest wieder her. Bei einem abrupten Prozessabbruch zwischen den Ersetzungen bleibt die Welt ladbar, da auch der bisherige Raum gegen 1.8.0 geprüft wurde. Einen Prozessabbruch hat die Suite nicht simuliert.
- Gezielte Browserprüfungen decken Szenenklicks mit zwei Spielern, Verben per Tastatur, Editor-Roundtrip und Referenzschutz, unter Windows den fehlgeschlagenen Dateiaustausch sowie Vorschauaktionen, Raumwechsel und Neustart ab. Die Spielansicht und der Raumeditor wurden auf der lokalen Abnahmeinstanz gesichtet. Beide Seiten liefern HTTP 200.
- Typcheck und Clientbuild bestanden. Vier gezielte Browserfälle am endgültigen Stand grün. Abschließende Vollsuite mit **329 bestandenen Tests** (221 Server, 108 Browser), 0 Warnungen. Standards- und Spezifikationsreview durch separate Reviewer. Beanstandete Verbzuordnung, unsichtbare Ausgangssperren, Entwurfsprüfung, Fehlermarkierung und Vorschaustart wurden korrigiert und gezielt nachgeprüft.
- Keine neue Formatversion oder Abhängigkeit. Die Verben verwenden bestehende Datenfelder und die bisherigen Zuordnungen. Veröffentlichte Schemas, Testwelten und Referenzinhalte sind im Diff unverändert. Die Protokolle und die lokale Abnahmekopie liegen in `.scratch/adventure-chat/evidence/22`.
- Lokaler Implementierungscommit „Complete verb controls and interaction authoring (ticket 22)“. Menschliche Abnahme ausstehend.

## Manuelle Abnahme

1. [Spiel](http://127.0.0.1:55222/) in zwei Fenstern öffnen und „Navigationstest“ betreten. „Öffnen“ wählen, auf einen roten Türflügel in der Raummitte zeigen und „Öffne Tür“ prüfen. Anklicken und in beiden Fenstern die offene Tür beobachten. Mit der Tab-Taste „Schließen“ fokussieren und per Eingabetaste wählen. Nach der Aktion muss „Gehe zu“ aktiv sein. Dasselbe bei kleinerer Fenstergröße wiederholen.
2. [Editor für den Navigationstest](http://127.0.0.1:55222/editor.html?worldId=navigation) öffnen, `door` wählen und in „Zustand ansehen“ zwischen „Geschlossen“ und „Offen“ wechseln. Türposition, Hindernis und Ausgangssperre vergleichen. Die Raum-Vorschau startet geschlossen. Mit dem Vorschauverb „Öffnen“ öffnen, mit „Gehe zu“ durch den freigegebenen Ausgang rechts gehen und wieder zurückkehren. Nach „Bearbeiten“ und Neustart der Vorschau ist der Ausgang erneut gesperrt.
3. [Editor für die Interaktions-Testwelt](http://127.0.0.1:55222/editor.html?worldId=interactions) öffnen. Bei `switch` die Aktion und das Ziel `gate` im Nebenraum ansehen. Bei `chair` einen zusätzlichen Zustand anlegen und Position/Verbauswahl ausprobieren. Zum Test des Formatwechsels ein Zustandshindernis hinzufügen, oben bewusst 1.8.0 wählen, den Hinweis zum Manifest prüfen, speichern und neu laden.

Die Abnahmeumgebung enthält separate Kopien der synthetischen Testwelten. Dort gespeicherte Änderungen bleiben in diesen Kopien. Die Szenengestaltung der Referenzwelt ist Teil von Ticket 23.
