# 15: Vordergrundobjekte und Verdeckung im Raumeditor

**What to build:** Autoren platzieren grafische Raumobjekte mit Tiefenordnung. Eine Figur hinter einem Tisch wird von dessen sichtbaren Pixeln teilweise verdeckt; vor dem Tisch bleibt sie davor sichtbar. Spiel und Editor-Vorschau verwenden dieselbe Darstellung.

**Blocked by:** 06, 09, 11 (done); Gesamtabnahme des Zusammenspiels zusätzlich 12 und 14

**Status:** done

Am 04.11.2026 mit „ok“ für den vorgeschlagenen Umfang, Datenvertrag, Gleichstandsregeln und Testgrenzen freigegeben. Umsetzung erfolgt sequenziell; parallele Subagents nur für die beiden Reviewachsen.

## Ausgangslage und Fachkontext

`rendering/renderer.ts` zeichnet derzeit den vollständigen Hintergrund und danach alle nach Fußpunkt-Y sortierten Figuren. Tischbilder sind im Hintergrund eingebrannt. Eine reine Kollisionsfläche verdeckt keine Figur. Ticket 13 schließt Verdeckung ausdrücklich aus und ist kein fachlicher Blocker dieses Tickets; Änderungen an Format/Renderer dennoch abstimmen.

Vor Umsetzung lesen: `CONTENT-FORMAT.md`, `ARCHITECTURE.md`, `docs/work-packages/world-format-compatibility.md`, ADR 0001, `TESTING.md` und bestehende Editor-/Ressourcenregeln. Die neue Funktion erweitert die bisherige Zurückstellung echter Vordergrundmasken in der Architektur bewusst.

## Acceptance criteria

- [x] Optionale Raumobjektliste mit stabiler ID, relativem PNG-Pfad, Position und Zielgröße in logischen Raumpixeln sowie horizontaler Tiefenlinie in Raum-Y. Konkrete YAML-Felder, erlaubte Koordinaten/Größen, Anzahl- und Ressourcenlimits vor Umsetzung festlegen. Transparente PNG-Pixel ermöglichen Teilverdeckung.
- [x] Vorschlag zur Ordnung: Objekt und Figuren gemeinsam nach Tiefen-Y zeichnen; Figuren hinter der Objektlinie zuerst, Figuren davor danach. Gleichstand deterministisch festlegen und testen; mehrere Objekte erhalten stabile Reihenfolge. Die Linie ist Autoreninformation, keine automatische Ableitung aus dem Bildrand.
- [x] Transparente Bereiche bleiben durchsichtig (etwa zwischen Tischbeinen). Das Objekt darf Figuren ausschließlich über seine tatsächlich sichtbaren Pixel verdecken; keine rechteckige Überdeckung. Kein Flackern bei Bewegung über die Tiefenlinie.
- [x] Verdeckung und Kollision sind getrennte Daten. Ein grafisches Objekt verändert nicht automatisch Wegfindung, erlaubte Fußpunkte oder Geschwindigkeit. Bestehende Hindernispolygone bleiben verantwortlich für Kollision; Editor macht diese Trennung verständlich.
- [x] Editor erlaubt Anlegen, Bildzuordnung/PNG-Upload nach bestehenden Schutzregeln, Auswählen, Verschieben, Größe und Tiefenlinie bearbeiten sowie Löschen. Objektumriss und Tiefenlinie sind sichtbar; Überlappungen lassen sich eindeutig auswählen. Ungeeignete Werte werden verständlich angezeigt.
- [x] Speichern und Neuladen erhält alle Angaben. Ungültige Daten überschreiben keinen gültigen Raum. Weltgebundene Ressourcenauflösung funktioniert in Bearbeitung, Vorschau und Spiel einschließlich identischer relativer Pfade in anderen Welten.
- [x] Vorschau erlaubt eine Figur hinter und vor dem Objekt zu bewegen. Namen und Sprechblasen bleiben lesbar: Vorschlag ist eine separate Ebene über den Raumobjekten. Regel vor Beginn bestätigen und bestehende Randsprechblasen berücksichtigen.
- [x] Raumobjekte verwenden dieselben Raumkoordinaten und dieselbe Kameraverschiebung wie Hintergrund und Figuren. Scrollen, CSS-Skalierung, Draufsicht und unterschiedliche Figurengrößen verändern die Tiefenentscheidung nicht. Eventuelle Tiefenskalierung aus Ticket 13 richtet sich weiter am Figurenfußpunkt aus.
- [x] Alte Räume ohne Objektliste behalten ihre bisherige Darstellung. Optionale Erweiterung als Minor begründen, neues Schema-Bündel ergänzen, unterstützte Versionen explizit erweitern. Tatsächliche Versionsnummer mit 13/14 abstimmen; veröffentlichte Schemas/Teilschemas und ältere Fixtures unverändert lassen.
- [x] Autorenformat, Architektur, Versionsmatrix, neue konkrete Glossarbegriffe und Testnachweise aktualisieren. Konkrete manuelle Abnahme hinter/vor einem Objekt in Spiel und Editor dokumentieren; ready-for-human, keine automatische Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Inhalts-Lade-/Speichergrenze, Editorbedienung und öffentliche Canvas-/Browserdarstellung. Fehlende und ungültige Ressourcen, Pfade, IDs, Größen und Linien; transparente Lücken, Figur dahinter/davor und Gleichstand; mehrere Objekte/Figuren; zwei Browser, unterschiedliche Figurengrößen, Kameraoffset und CSS-Skalierung; Editor-Roundtrip einschließlich PNG-Zuordnung und Löschen. Bestehende Wegfindung unverändert nachweisen; alte Fixtures unverändert laden und darstellen. Keine Tests privater Sortierlisten.

Visuelle Abnahme bewertet Maskenkanten und den glaubwürdigen Wechsel zwischen hinten und vorne. Eine neue kleine Testszene genügt hier; passgenaue Tischmasken der Referenzwelt gehören zu Ticket 16.

## Nicht im Umfang

Automatische Freistellung/Segmentierung, 3D, per-Pixel-Tiefenkarten, schräge Tiefenlinien, mehrere Tiefensegmente pro Objekt, Animation von Raumobjekten oder automatische Kollisionsableitung. Keine neue Malsoftware im Editor. Für komplizierte Motive können Autoren getrennte PNG-Objekte anlegen.

## Umsetzung und Nachweise – 04.11.2026
- Bestätigter Vertrag: Format 1.4.0; optionale objects mit id/image/position/size/depthY, Position links oben, ganze Raumkoordinaten, positive vollständig im Raum liegende Größe, Tiefenlinie 0 bis Raumhöhe, maximal 128 Objekte. IDs 1–64 ASCII-Zeichen, case-sensitive eindeutig. PNG- und Pfadlimits bestehen weiter.
- Gemeinsamer ObjectRenderer, Tiefenordnung nach Raum-Y und ordinaler Objekt-ID; Figur gewinnt Gleichstand. PNG-Alpha bleibt erhalten. Namen/Sprechblasen folgen darüber; Räume ohne Objekte behalten auch ihre frühere Namens-Zeichenreihenfolge.
- Editor mit Auswahlliste, Bildzuordnung/Upload, Zahlenfeldern, Verschieben, Löschen, sichtbarem Umriss/Tiefenlinie. Alt+Klick wählt Geometrie unter einem Objekt. Ungültige Raumdaten überschreiben keinen gültigen Raum.
- Rot/Grün: neues Schema zunächst fehlend, danach akzeptiert; YAML-Objektrundlauf zunächst abgewiesen, danach erhalten. Reviewregression Eingabefeld 290 wurde bei Canvas-Hover auf 280 zurückgesetzt: zuerst rot reproduziert, aktive Eingabe anschließend erhalten.
- 14 gezielte Inhaltsfälle erfolgreich. Drei Browsertests prüfen Pixel vor/hinter/bei Gleichstand, transparente Lücke, 32/64-Pixel-Figuren, Kameraoffset, ordinalen Objektgleichstand, weltbezogene Objektpfade, zwei Browser mit Bewegung, Editor-Upload/Speichern/Neuladen/Löschen sowie Alt+Klick und Hoverregression.
- Abschließende Vollsuite: 230 bestanden, 0 Warnungen. Typcheck und Clientbuild bestanden. Nach Ergänzung der letzten Reviewfälle alle drei Objekt-Browsertests erneut erfolgreich. Ein überlappender Teststart traf eine gesperrte Testdatei; nach Ende der Vollsuite sequenziell erfolgreich wiederholt.
- Beide unabhängigen Subagent-Reviewachsen durchgeführt und nach Korrektur nachgeprüft: Standards 0 offen, Spec 0 offene Funktionsbefunde. Ergänzende Tests schließen gemeldete Sortier-/Weltpfad-/Alt-Klick-Lücken.
- Bildschirmbilder von Bearbeitung und Vorschau geprüft; neue synthetische Testgrafik ist absichtlich kein fertiges Tischmotiv. Referenzgestaltung bleibt Ticket 16. Keine neuen Abhängigkeiten.
- Minor-Begründung und explizite Matrix in CONTENT-FORMAT.md und Kompatibilitätsvertrag; ältere Schemas/Teilschemas sowie Referenzfixtures unverändert.

## Manuelle Abnahme (am 04.11.2026 bestätigt)
1. Eine Arbeitskopie von tests/Fixtures/foreground außerhalb der Fixture anlegen und Server im Autorenmodus mit dieser Welt starten, beispielsweise: dotnet run --project src/AdventureChat.Server -- --authoring --World:Root <absoluter-Pfad-zur-Arbeitskopie>.
2. hall im Editor öffnen. Im Bereich Raumobjekte table wählen. Position, Größe und Tiefenlinie ändern, per Maus verschieben und mit Alt+Klick den Boden unter dem Objekt auswählen. Zweites Objekt anlegen, PNG hochladen/zuordnen; überlappende Objekte über die Liste auswählen.
3. Speichern, Raum neu laden und Werte vergleichen. Ungültige Größe und fehlendes PNG prüfen: Speichern wird abgewiesen. Zweites Objekt löschen und erneut speichern/laden.
4. In der Vorschau von oberhalb nach unterhalb der Tiefenlinie laufen. Rote Testfläche verdeckt die Figur dahinter; transparente senkrechte Lücke lässt sie sichtbar. Vor der Linie bleibt die Figur davor. Namen und Sprechblasen prüfen.
5. Dieselbe Welt in zwei Browserfenstern betreten, gemeinsam vor/hinter dem Objekt laufen und chatten. Maskenkanten, Wechsel ohne Flackern und Lesbarkeit menschlich bewerten.

Alle markierten Kriterien beschreiben den implementierten Stand, keine menschliche Abnahme. Erst ausdrückliche Benutzerabnahme setzt done; danach Ticket 16 vorschlagen, anschließend 13.

## Menschliche Abnahme
Am 04.11.2026 nach Start der Testwelt foreground ausdrücklich vom Benutzer bestätigt: „funktioniert“. Ticket 15 ist done. Implementierungscommit: e485525. Die lokale Arbeitskopie .scratch/foreground-demo bleibt unverändert und wird nicht mitcommittet. Nächstes Paket: Ticket 16 vorschlagen und vor Umsetzung bestätigen lassen.
