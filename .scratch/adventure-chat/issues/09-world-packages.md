# 09: Externe, versionierte Weltpakete laden und bearbeiten

**What to build:** Ein Betreiber startet den Server mit einer extern gespeicherten Welt. Weltmanifest, Räume und Bilder liegen zusammen außerhalb des Anwendungsrepositorys. Der Server prüft Formatkompatibilität, Schema und fachliche Konsistenz; der lokale Editor bearbeitet genau diese Welt. Eine mitgelieferte Referenzwelt zeigt den vollständigen Ablauf.

**Blocked by:** 08 (vor Implementierung lokales Stabilisierungsticket und Status nachweisen), 07

**Status:** done

## Acceptance criteria

- [ ] Eine Welt hat stabile ID, Anzeigenamen, Formatversion als SemVer, Inhaltsversion als SemVer sowie Startraum und Start-Spawn. Ein dokumentiertes Manifest verbindet die Welt mit ihren Raumdateien und Bildern.
- [ ] Weltpakete liegen in eigenständigen Verzeichnissen. Serverkonfiguration nimmt ein oder mehrere explizite Weltverzeichnisse entgegen; exakte Konfigurationssyntax wird dokumentiert. Eigene Welten erfordern keine Änderungen am Anwendungsrepository. Paket 09 demonstriert eine einzelne Welt; Mehrwelt-Sitzungen folgen in 10.
- [ ] Mitgelieferte Referenzwelt lässt sich in ein externes Verzeichnis kopieren und von dort spielen und im expliziten lokalen Autorenmodus bearbeiten. Keine Schreibzugriffe auf installierte Anwendungsdateien erforderlich.
- [ ] Bei genau einer Welt führt der Einstieg zu deren gültigem Start-Spawn. Raumwechsel bleiben innerhalb dieser Welt. Nach Serverneustart erfolgt ein frischer Eintritt am Start-Spawn; kein persistierter Spielzustand.
- [ ] Für jede veröffentlichte Formatversion existiert ein unveränderliches, eindeutig versioniertes Schema-Bündel für Manifest und Raumdateien. Feld für Formatversion, Schema-ID, Pfade und Auswahlregeln stimmen überein. Keine stille Validierung gegen das neueste Schema.
- [ ] Server prüft zuerst die Versionsdeklaration und die unterstützte Formatversion, danach die Dokumente gegen die exakt zugehörigen Schemas, anschließend die fachlichen Regeln. Fehlende, ungültige oder nicht unterstützte Versionen werden ausdrücklich abgelehnt.
- [ ] Schemaauswahl funktioniert offline mit mitgelieferten Dateien. Dokumentierte YAML-Language-Server-Zuordnung ermöglicht Completion und Strukturprüfung im Texteditor. Ein Schema-Kommentar ist kein vertrauenswürdiger Laufzeitselektor; maßgeblich ist das validierte Versionsfeld.
- [ ] Existenz und Eindeutigkeit von IDs, Start-Spawn, Raum- und Spawnverweisen sowie Bildern und Geometrie werden geprüft. Fehlermeldungen benennen Welt, Datei, Feld und Ursache; Zeile, soweit verfügbar. Kein stilles Überspringen defekter Dateien.
- [ ] Pfade werden relativ zur Welt aufgelöst. Absolute Ressourcenpfade, Traversal und Ausbruch über symbolische Links/Junctions werden verhindert. Spieler erhalten weder lokale Serverpfade noch Schreibzugriff. Schema-Referenzen verursachen keine externen Netzwerkzugriffe.
- [ ] Der Editor verwendet dieselben Prüfungen und schreibt eine ausdrücklich unterstützte Version. Speichern ändert weder Format- noch Inhaltsversion stillschweigend. Eine ausdrückliche Versionsänderung validiert vor dem Schreiben; neue Felder dürfen nicht unter alter Versionsdeklaration gespeichert werden. Nicht unterstützte Dateien werden nicht verlustbehaftet geöffnet oder überschrieben.
- [ ] Bestehende Raumdateien mit `version: 1` und das bestehende Schema werden als Legacy-Format dokumentiert und unverändert als Kompatibilitätsfixtures erhalten. Keine automatische Migration oder Umdeutung zu `1.0.0`. Ein dokumentierter manueller Übernahmeweg nutzt diese Räume über einen begrenzten Legacy-Lesepfad im Weltpaket, ohne bestehende YAML-Dateien umzuschreiben. Die konkrete Zuordnung im neuen Manifest wird vor Implementierung festgelegt und getestet.
- [ ] Unterstützte Versionen, SemVer-Regeln und Leser-/Schreiberverhalten sind dokumentiert. Ältere Referenzwelten derselben unterstützten Major-Version laufen auf neueren Servern weiterhin mit gleicher zugesicherter Semantik. Eine neuere Welt wird von einem älteren Server nur bei nachgewiesener Unterstützung akzeptiert.
- [ ] Keine Inventar-, Skripting-, automatische Migrations-, Hot-Reload-, Upload- oder Spielzustandspersistenzfunktionen. Keine zusätzliche Architekturplattform oder Schema-Registry als Dienst.
- [ ] ARCHITECTURE.md, CONTENT-FORMAT.md, SPEC.md, TESTING.md, GLOSSARY.md, AGENTS.md, PLAN.md und active-state.md sind widerspruchsfrei aktualisiert; tatsächliche Checks und manuelle Abnahme sind getrennt dokumentiert.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Inhaltsladegrenze mit temporären Dateisystem-Welten, bestehende HTTP-/Editor-Endpunkte und ein Browserablauf. Kein Test privater Loader-Methoden; Fixtures sind unveränderte veröffentlichte Daten.

Gezielte Fälle: externe Referenzwelt laden, speichern und erneut laden; falsches oder fehlendes Versionsfeld; explizit nicht unterstützte Zukunftsversion und Major-Version; Schemafehler; semantisch ungültige Referenz; fehlendes Bild; Pfadausbruch; doppelte Welt-ID; Offline-Laden; ältere unterstützte Fixture und heutige Fixture auf demselben Server; Editor verhindert Datenverlust und Versionslügen; unveränderte Legacy-Räume werden übernommen. Veröffentlichtes Schema bleibt bytegleich. Testbelege zu früher unterstützten Versionen nicht durch Neugenerieren der Fixtures ersetzen.

## Vorgehen

1. Bestehende Lade-/Editorgrenzen und Ticket 08 prüfen. Manifest, Legacy-Zuordnung, Versionsmatrix, Fehlermodus und Testgrenzen konkret vorschlagen und bestätigen lassen.
2. Einen externen Weltordner mit Referenzinhalt durchgehend laden, validieren und betreten.
3. Versionierte Offline-Schemas und fachliche Prüfung mit negativen Fällen ergänzen.
4. Editor auf ausgewählte Welt begrenzen, Legacy-Übernahme und ältere Fixtures absichern.
5. Dokumentation, lokale Checks und Review abschließen; Status ready-for-human, noch nicht done.

## Fachkontext
[Architekturregel](../../../docs/work-packages/world-format-compatibility.md). Root-AGENTS.md und bestehende SPEC.md gelten mit den ausdrücklich bestätigten Erweiterungen aus diesem Paket.

## Ergebnis und Nachweise · 01.11.2026
Umfang und öffentliche Testgrenzen bestätigt. Externe Weltpakete, Weltformat `1.0.0` mit `legacy-1`, Offline-Schemas, Start-Spawn, Referenzwelt und lokaler Editorbetrieb sind implementiert. Ältere Raumdateien und ursprüngliches Schema sind unverändert. Die Unterstützungsmatrix enthält ausschließlich `1.0.0`; diese Erstveröffentlichung deutet die bestehende ganzzahlige Raumversion nicht um. Weitere SemVer-Folgen gelten gemäß Kompatibilitätsvertrag. Vor erster Veröffentlichung wurden die Schema-Dateien nach dem Review korrigiert und anschließend bytegenau abgesichert.

Checks: Offline-Restore im Locked Mode aus vorhandenem Paketcache (NuGet-Audit deaktiviert), Client-Typcheck und Build sowie .NET-Build erfolgreich. Vollständiger .NET-/Browserlauf nach Reviewkorrekturen: 163 bestanden. Anschließend gezielte Weltpakettests einschließlich Schemafingerabdrücken und zusätzlicher Legacy-/Referenzweltprüfung: 31 bestanden. Ein symbolischer Datei-Link-Test wurde wegen fehlender Windows-Berechtigung übersprungen; der tatsächliche Junction-Test bestand. Der Browserablauf prüft externen Eintritt, Editor, Speichern und Neustart.

Review: Unabhängige Vorprüfung und abschließende Reviews zu Standards und Ticket fanden und prüften nach Korrektur Schema- und PNG-Prüfung, Diagnose, Validierungsreihenfolge, SemVer-Schema, Bildauslieferung und gemeinsame YAML-Konvertierung; in den Nachprüfungen keine verbleibenden Befunde. Ausgangspunkt: `f1cffc70f989ef9c0057e45a3a5813d8cd46f8e1`. Menschliche Abnahme steht aus.

## Manuelle Abnahme
1. `worlds/reference` in ein Verzeichnis außerhalb des Repositorys kopieren. Nach README mit `--World:Root <Pfad> --authoring` starten; im Browser prüfen, dass der Startpunkt im Manifest verwendet und das Hintergrundbild geladen wird.
2. `/editor.html` lokal öffnen, einen Raum bearbeiten und speichern. Server neu starten und prüfen, dass die Änderung erhalten bleibt und der Eintritt wieder am Start-Spawn erfolgt. Eine zweite Browserinstanz kann den Raum gemeinsam betreten.
3. Server beenden; im kopierten Manifest `formatVersion` auf `9.0.0` ändern. Den erneuten Start versuchen und die verständliche Ablehnung prüfen. Danach `1.0.0` wiederherstellen.
4. Optional die unveränderten Dateien aus `content/rooms` in das Paket übernehmen und mit `roomFormat: legacy-1` starten. Die lokalen YAML-Schema-Zuordnungen nach README prüfen.

Erst eine ausdrückliche menschliche Abnahme setzt dieses Ticket auf `done`. Weltwahl und Mehrweltisolation folgen in Ticket 10.

## Erneuter Review · 02.11.2026
Auf Wunsch des Benutzers erneut selbst geprüft; keine menschliche Produktabnahme. TypeScript-Typcheck und vollständiger .NET-/Browserlauf erfolgreich (163 bestanden); der gezielte externe Browserablauf und 31 Weltpakettests bestanden. Der symbolische Datei-Link-Test bleibt wegen fehlender Berechtigung übersprungen; Junction-Prüfung ist abgedeckt. Der externe Browserablauf prüft einen Teilnehmer; die übrigen Multiplayerprüfungen decken den bisherigen Betrieb ab, keinen separaten Zwei-Browser-Ablauf für externe Welten.

Offene Reviewpunkte: Die Weltvalidierung ist auf Loader und Editor verteilt. `ValidateWorld()` lädt für jeden Raum erneut die gesamte Welt einschließlich PNG-Prüfung; das erzeugt quadratisch zunehmende Arbeit und birgt das Risiko auseinanderlaufender Regeln. Außerdem wurde der vorhandene lokale Bildupload für externe Pakete erweitert, während dieses Ticket Upload-Funktionen ausschließt: Ob sich der Ausschluss auf diese vorhandene Editorfunktion oder auf eine neue Upload-Funktion bezieht, ist eine offene Auslegung. Keine Umsetzung oder Fehlerkorrektur im Zuge dieses Reviews. Zum Zeitpunkt des Reviews wieder `in-progress`; Ticket 08 bleibt `done`.

## Abnahme · 02.11.2026
Der Benutzer hat Ticket 09 ausdrücklich mit „abgenommen“ akzeptiert und die KI-Prüfung als Grundlage toleriert. Die Prüfungen und Reviews wurden von der KI durchgeführt; der Benutzer hat nicht selbst getestet. Die oben genannten Reviewpunkte und Testgrenzen bleiben dokumentiert und wurden durch die Abnahme nicht als behoben erklärt.
