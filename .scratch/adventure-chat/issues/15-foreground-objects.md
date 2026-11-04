# 15: Vordergrundobjekte und Verdeckung im Raumeditor

**What to build:** Autoren platzieren grafische Raumobjekte mit Tiefenordnung. Eine Figur hinter einem Tisch wird von dessen sichtbaren Pixeln teilweise verdeckt; vor dem Tisch bleibt sie davor sichtbar. Spiel und Editor-Vorschau verwenden dieselbe Darstellung.

**Blocked by:** 06, 09, 11 (done); Gesamtabnahme des Zusammenspiels zusätzlich 12 und 14

**Status:** draft

Am 04.11.2026 zur Planung beauftragt. Keine Implementierungsfreigabe; Datenvertrag, Gleichstandsregel und Testgrenzen vor Beginn bestätigen. Keine parallele Umsetzung automatisch autorisiert.

## Ausgangslage und Fachkontext

`rendering/renderer.ts` zeichnet derzeit den vollständigen Hintergrund und danach alle nach Fußpunkt-Y sortierten Figuren. Tischbilder sind im Hintergrund eingebrannt. Eine reine Kollisionsfläche verdeckt keine Figur. Ticket 13 schließt Verdeckung ausdrücklich aus und ist kein fachlicher Blocker dieses Tickets; Änderungen an Format/Renderer dennoch abstimmen.

Vor Umsetzung lesen: `CONTENT-FORMAT.md`, `ARCHITECTURE.md`, `docs/work-packages/world-format-compatibility.md`, ADR 0001, `TESTING.md` und bestehende Editor-/Ressourcenregeln. Die neue Funktion erweitert die bisherige Zurückstellung echter Vordergrundmasken in der Architektur bewusst.

## Acceptance criteria

- [ ] Optionale Raumobjektliste mit stabiler ID, relativem PNG-Pfad, Position und Zielgröße in logischen Raumpixeln sowie horizontaler Tiefenlinie in Raum-Y. Konkrete YAML-Felder, erlaubte Koordinaten/Größen, Anzahl- und Ressourcenlimits vor Umsetzung festlegen. Transparente PNG-Pixel ermöglichen Teilverdeckung.
- [ ] Vorschlag zur Ordnung: Objekt und Figuren gemeinsam nach Tiefen-Y zeichnen; Figuren hinter der Objektlinie zuerst, Figuren davor danach. Gleichstand deterministisch festlegen und testen; mehrere Objekte erhalten stabile Reihenfolge. Die Linie ist Autoreninformation, keine automatische Ableitung aus dem Bildrand.
- [ ] Transparente Bereiche bleiben durchsichtig (etwa zwischen Tischbeinen). Das Objekt darf Figuren ausschließlich über seine tatsächlich sichtbaren Pixel verdecken; keine rechteckige Überdeckung. Kein Flackern bei Bewegung über die Tiefenlinie.
- [ ] Verdeckung und Kollision sind getrennte Daten. Ein grafisches Objekt verändert nicht automatisch Wegfindung, erlaubte Fußpunkte oder Geschwindigkeit. Bestehende Hindernispolygone bleiben verantwortlich für Kollision; Editor macht diese Trennung verständlich.
- [ ] Editor erlaubt Anlegen, Bildzuordnung/PNG-Upload nach bestehenden Schutzregeln, Auswählen, Verschieben, Größe und Tiefenlinie bearbeiten sowie Löschen. Objektumriss und Tiefenlinie sind sichtbar; Überlappungen lassen sich eindeutig auswählen. Ungeeignete Werte werden verständlich angezeigt.
- [ ] Speichern und Neuladen erhält alle Angaben. Ungültige Daten überschreiben keinen gültigen Raum. Weltgebundene Ressourcenauflösung funktioniert in Bearbeitung, Vorschau und Spiel einschließlich identischer relativer Pfade in anderen Welten.
- [ ] Vorschau erlaubt eine Figur hinter und vor dem Objekt zu bewegen. Namen und Sprechblasen bleiben lesbar: Vorschlag ist eine separate Ebene über den Raumobjekten. Regel vor Beginn bestätigen und bestehende Randsprechblasen berücksichtigen.
- [ ] Raumobjekte verwenden dieselben Raumkoordinaten und dieselbe Kameraverschiebung wie Hintergrund und Figuren. Scrollen, CSS-Skalierung, Draufsicht und unterschiedliche Figurengrößen verändern die Tiefenentscheidung nicht. Eventuelle Tiefenskalierung aus Ticket 13 richtet sich weiter am Figurenfußpunkt aus.
- [ ] Alte Räume ohne Objektliste behalten ihre bisherige Darstellung. Optionale Erweiterung als Minor begründen, neues Schema-Bündel ergänzen, unterstützte Versionen explizit erweitern. Tatsächliche Versionsnummer mit 13/14 abstimmen; veröffentlichte Schemas/Teilschemas und ältere Fixtures unverändert lassen.
- [ ] Autorenformat, Architektur, Versionsmatrix, neue konkrete Glossarbegriffe und Testnachweise aktualisieren. Konkrete manuelle Abnahme hinter/vor einem Objekt in Spiel und Editor dokumentieren; ready-for-human, keine automatische Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Inhalts-Lade-/Speichergrenze, Editorbedienung und öffentliche Canvas-/Browserdarstellung. Fehlende und ungültige Ressourcen, Pfade, IDs, Größen und Linien; transparente Lücken, Figur dahinter/davor und Gleichstand; mehrere Objekte/Figuren; zwei Browser, unterschiedliche Figurengrößen, Kameraoffset und CSS-Skalierung; Editor-Roundtrip einschließlich PNG-Zuordnung und Löschen. Bestehende Wegfindung unverändert nachweisen; alte Fixtures unverändert laden und darstellen. Keine Tests privater Sortierlisten.

Visuelle Abnahme bewertet Maskenkanten und den glaubwürdigen Wechsel zwischen hinten und vorne. Eine neue kleine Testszene genügt hier; passgenaue Tischmasken der Referenzwelt gehören zu Ticket 16.

## Nicht im Umfang

Automatische Freistellung/Segmentierung, 3D, per-Pixel-Tiefenkarten, schräge Tiefenlinien, mehrere Tiefensegmente pro Objekt, Animation von Raumobjekten oder automatische Kollisionsableitung. Keine neue Malsoftware im Editor. Für komplizierte Motive können Autoren getrennte PNG-Objekte anlegen.
