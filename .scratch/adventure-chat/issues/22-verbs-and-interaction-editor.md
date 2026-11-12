# 22: Verbenleiste und Browsereditor für interaktive Objekte

**What to build:** Spieler wählen klassische Adventure-Verben und kombinieren sie mit einem Objekt zu einem sichtbaren Satz wie „Öffne Tür“. Autoren konfigurieren dieselben Zustände, Aktionen, direkten Verknüpfungen und Hindernisse vollständig im Browsereditor und prüfen sie in der gemeinsamen Vorschau.

**Blocked by:** [20](20-shared-object-actions.md), [21](21-state-dependent-navigation.md) sowie 18 müssen done sein; 06, 15 und 17 sind done. 18 ist derzeit ready-for-human und bleibt bis zur ausdrücklichen Abnahme ein Blocker für die abschließende Oberflächenintegration.

**Status:** draft

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

## Offene Detailentscheidungen vor Paketfreigabe

Exakte Anordnung/Größe der Verbenleiste, deutschsprachige Beschriftungen/Objektnamen für korrekte Satzbildung, Standardverb und Zurücksetzen nach Aktion; Trefferregel bei transparenten/überlappenden Objekten und Markierung zulässiger Ziele. Verhalten aus der Ferne aus 20 übernehmen. Versionierungsdialog und Umgang mit Lösch-/Umbenennungsreferenzen festlegen. Keine automatische vollständige Sprachgrammatik oder Lokalisierungsplattform einführen.

## Stand / Abnahme

10.11.2026: draft, ausschließlich geplant. Vor Umsetzung Paketumfang und Testgrenzen bestätigen lassen.
