# Ticket 17 – Bearbeitungen und Ungespeichert-Status im Raumeditor bündeln

## What to build
Das bestehende EditorState-Modul verwaltet Raumänderungen, Auswahl und Ungespeichert-Status zusammen. Inspektor, Seitenformular, Hintergrundwahl, Canvas und Objektverwaltung verwenden dessen Bearbeitungswege; DOM-Module lesen Eingaben und stellen Ergebnisse dar. Außerhalb des Zustandsmoduls werden dessen lebende Raumdaten nicht mehr direkt verändert. Kein generisches Befehlssystem und kein Editor-Neubau.

## Blocked by
- 06, 07, 11, 12, 13, 14, 15 (alle done).
- Keine offenen Blocker; Umfang und Testgrenzen am 09.11.2026 mit „alles ja“ bestätigt.

## Status
ready-for-human. Am 09.11.2026 Umfang und Testgrenzen einschließlich Speicherantworten und internem Wechselschutz mit „alles ja“ bestätigt, implementiert und abschließend geprüft. Keine menschliche Abnahme; done bleibt ausdrücklich ausstehend.

## Ausgangsbefund
Basis: 784b47ac4b0abdeac053999edb7a5a4e40987f64; Arbeitsverzeichnis vor Prüfung sauber. Eigene statische Begehung und gesonderte read-only Prüfung der Testverträge erfolgt. Keine Anwendungstests ausgeführt.

- EditorInspector.applyVertexChanges/applySpawnChanges/applyExitChanges ändern Raumdaten und teilweise Auswahl direkt. EditorApp.handleInspectorInput synchronisiert Ansichten, setzt dirty aber nicht.
- Ein normaler Canvas-Auswahlklick führt bereits move* aus und setzt dirty auch ohne Inhaltsänderung. Das maskiert den Inspektorfehler im bestehenden Test Editor_Inspector_ShowsAndEditsActiveElement.
- Aussagekräftiger Reproduktionsablauf: Raum laden, Element auswählen, speichern und Erfolg abwarten, ausschließlich Inspektoreingabe ändern, Anzeige und Verlassenswarnung prüfen. Dieser Ablauf ist noch nicht im Browser nachgewiesen. In diesem Chat meldete die Browsersteuerung keine verfügbaren Browser; temporärer Testserver und Weltkopie wurden wieder entfernt.
- Hintergrundwahl setzt dirty, aktualisiert den Badge aber nicht. Seitenformular mutiert den Raum direkt und verwendet updateMetadata({}) als gesonderte Dirty-Markierung.
- Eine erfolgreiche Speicherantwort ruft markClean() ohne Bezug auf den gesendeten Bearbeitungsstand auf. Neuere Änderungen oder ein inzwischen anderer Raum können so fälschlich als gespeichert gelten.
- loadRoom/newRoom ersetzen einen ungespeicherten Entwurf ohne Nachfrage. ARCHITECTURE.md verlangt bereits eine Meldung beim Wechsel.

## Bestätigter Umfang
1. Bestehende Bearbeitungsregeln im Zustandsmodul bündeln: Metadaten, Darstellungseinstellungen, Geometrie, Spawns, Ausgänge und Objekte; bestehende Grenzen, Defaults und Auswahl bei ID-Änderungen erhalten. UI-Eingabegrenzen folgen der tatsächlichen Raumgröße.
2. Inhaltsänderungen setzen den Ungespeichert-Status konsistent. Reine Auswahl, Vorschau, Prüfen und wirkungslose Eingaben machen einen sauberen Raum nicht ungespeichert. Kein Undo/Redo oder automatischer Inhaltsvergleich mit allen früheren Speicherständen; manuelles Zurückändern muss den Status nicht automatisch löschen.
3. Erfolg bestätigt ausschließlich den tatsächlich gesendeten Entwurf. Bearbeitung bleibt während des Speicherns möglich; spätere Änderungen bleiben ungespeichert, veraltete Antworten verändern keinen inzwischen anderen Entwurf. Fehler erhalten Entwurf und Status.
4. Bei Raumwahl und „Neu“ mit ungespeicherten Änderungen: Verwerfen ausdrücklich bestätigen oder abbrechen; Abbruch erhält Entwurf und passende Raumauswahl. Saubere Wechsel bleiben ohne Nachfrage. Keine zusätzliche automatische Speicherung.

## Acceptance criteria (bestätigt)
- Eine ausschließliche Inspektoränderung aus sauberem Zustand zeigt „Ungespeichert“ und löst beim Neuladen/Verlassen die Browserwarnung aus; Abbrechen erhält die Eingabe.
- Erfolgreiches Speichern und erneutes Laden erhalten die Änderung und stellen den sauberen Status her, wenn es keine neueren Änderungen gibt.
- Abgelehntes oder unterbrochenes Speichern verliert keine Änderungen und hebt den Schutz nicht auf.
- Verzögerte Speicherantworten löschen den Status neuerer Änderungen nicht; ein anderer inzwischen geladener Entwurf wird nicht beeinflusst.
- Reine Auswahl bleibt sauber. Canvas-, Formular-, Hintergrund-, Inspektor- und Objektbearbeitung zeigen denselben verlässlichen Änderungsstatus; Auswahl nach Umbenennen bleibt konsistent.
- Bestehende Koordinaten- und Radiusregeln sowie breite Räume, Vorschau und öffentliche window.__ROOM_EDITOR__-Hooks bleiben funktionsfähig.
- Interner Raumwechsel und „Neu“ schützen ungespeicherte Änderungen entsprechend dem bestätigten Verhalten.

## Bestätigte Testgrenzen
Vorhandene öffentliche Browserbedienung und Inhalte-/Speicherschnittstellen nach TESTING.md. Keine privaten Feld-/Methodentests, keine neue Testplattform oder private Test-API.

Zuerst einen scheiternden Browsernachweis für den Inspektorablauf ergänzen und ausführen. Danach gezielt sichtbaren Status, echte Verlassenswarnung samt Abbruch, Speicher-Roundtrip, Speicherablehnung/Verbindungsfehler, verzögerte Antwort und internen Wechsel prüfen. Vorhandene Tests für Canvas, Objekte, breite Räume, Darstellungseinstellungen und Vorschau nutzen; nur tatsächliche Verhaltenslücken ergänzen. Tests arbeiten ausschließlich mit isolierten Weltkopien.

Am Paketende: Client-Typcheck, Clientbuild, vollständiges dotnet test; eigener Funktions-/Standardsreview und Diffprüfung. Bisherige 257 bestandene Tests sind historische Nachweise aus active-state.md, kein neuer Lauf.

## Grenzen und Kompatibilität
Nur Raumeditor; Weltpaketvalidierung aus dem zweiten Architekturvorschlag bleibt außerhalb. Gemeinsamen Renderer, serverseitige Wegfindung und Autorenformatsemantik erhalten. Keine neuen Abhängigkeiten vorgesehen. Keine Formatänderung oder SemVer-Anhebung vorgesehen: Regeln und Defaults sollen unverändert bleiben; im Abschlussreview ausdrücklich prüfen und begründen. Veröffentlichte Schemas, alte Referenzfixtures, gelieferte Originale und .scratch/foreground-demo bleiben unverändert. Nur lokale Arbeit.

## Offene Entscheidungen
Keine offenen Umfangsentscheidungen. Gesamter vorgeschlagener Umfang und Testgrenzen am 09.11.2026 ausdrücklich mit „alles ja“ bestätigt.

## Abschluss und menschliche Abnahme
Implementierung und Abschlussprüfung am 09.11.2026 abgeschlossen. Client-Typcheck und Clientbuild erfolgreich. Abschließende Vollsuite: 267 Tests bestanden, 0 Warnungen (183 bestehende Serverfälle und 84 Browserfälle einschließlich zehn neuer Fälle). Eigener Funktions-/Standardsreview und Diffprüfung abgeschlossen; keine offenen Befunde. Kein unabhängiger Implementierungsreview durch Subagenten behauptet; die gesonderte read-only Prüfung vor Umsetzung betraf Testverträge und Ausgangsbefund. Lokaler Implementierungscommit mit Titel `Consolidate room editor edits and unsaved state (ticket 17)`. Keine Remote-Aktion. Menschliche Abnahme und damit done stehen aus.

## Implementierungsstand und Nachweise (09.11.2026)
- EditorState besitzt alle übernommenen Bearbeitungswege und eine schreibgeschützte Raum-/Auswahlansicht. Zwei kleine interne Kernmodule halten Eigenschafts- und Entitätsregeln ohne DOM oder Netzwerk; keine zusätzliche Klasse, kein generisches Befehlssystem. EditorState wurde trotz zusätzlicher Verantwortung von 350 auf 298 Zeilen verkürzt. EditorElements setzt vorhandene UI-Elementtypen zusammen, statt deren Felder im Orchestrator zu duplizieren.
- Tatsächliche Inhaltsänderungen erhöhen die Bearbeitungsrevision; reine Auswahl und wirkungslose Eingaben nicht. Der Vergleich erfolgt nur bei übernommenen Bearbeitungen, nicht beim Zeichnen oder Hover. Manuelles Zurückändern setzt nicht automatisch auf sauber zurück.
- Speichern verwendet eine Kopie des gesendeten Entwurfs und bestätigt nur dessen Revision. Neuere Änderungen bleiben ungespeichert; Antworten eines inzwischen anderen Entwurfs verändern dessen Status nicht. Ein weiterer Speicherversuch ist bis zum Abschluss gesperrt, Bearbeitung bleibt möglich.
- Raumwahl und „Neu“ schützen ungespeicherte Änderungen mit Bestätigen/Abbrechen. Raumauswahl wird beim Abbruch wiederhergestellt. Verspätete Ladevorgänge berücksichtigen inzwischen erfolgte Bearbeitungen und weitere Raumwahl.
- Bestehende Objektregeln und Standardanlage liegen im Kern. Der Inspektor erhält reale Koordinatengrenzen; Auswahldaten folgen ID-Änderungen. Noch ladende Ausgangs-Zielfelder überschreiben bei anderer Eingabe kein vorhandenes Ziel.
- Unverändert angezeigte optionale Standardwerte bleiben im geladenen Entwurf erhalten. Gleiche Bedeutung und Defaults des Autorenformats; keine SemVer-Anhebung erforderlich. Bisherige Schemas/Fixtures/Welten/Bilder unverändert im Diff, keine neuen Abhängigkeiten.
- Rot/grün: ausschließliche Inspektoränderung nach erfolgreichem Speichern ließ zuerst den Badge verborgen; nach Korrektur samt echter beforeunload-Warnung/Abbruch und Speicher-Roundtrip grün. Der Test zeigte anschließend auch die unbeabsichtigte Änderung durch Auswahl und ist nach Korrektur vollständig grün.
- Weitere Rot/grün-Nachweise: zurückgehaltene Speicherantwort bei neuerer Bearbeitung in demselben beziehungsweise anderem Raum; fehlende Bestätigung bei Raumwahl/„Neu“; Auswahl nahe am Punkt verschob ihn; Hintergrundwahl ließ den Badge verborgen; breiter Spawn zeigte weiterhin max=640. Alle gezielt nach Korrektur grün.
- Weitere gezielte erfolgreiche Browserfälle: Validierungsablehnung und Verbindungsfehler erhalten Entwurf/Badge/echte Verlassenswarnung und gespeicherten Altstand; Umbenennen von Spawn/Ausgang erhält weitere Bearbeitung und Löschbarkeit, Radius 5–200. Bestehende Objekt- und breite Raum-/Vorschautests erfolgreich. Zehn neue Browserfälle insgesamt; bestehende Hintergrund- und Scrolltests erweitert.
- Client-Typcheck und Clientbuild nach Strukturreview erfolgreich. Eigener Funktions-/Standardsreview abgeschlossen. Die abschließende reine Zusammenstellung vorhandener UI-Typen lässt das erzeugte editor-app.js bytegleich (SHA256 vorher/nachher: 622e8fc4c301457ca9a324d67511175159f96bd519291b5df43ed0c3647851c1); Typcheck/Build erneut erfolgreich.
- Erste Vollsuite: 266 bestanden, ein bestehender Bildvergleich fehlgeschlagen, 0 Warnungen. ActualArt_OverlaysWithoutSeamsAndOccludesOnlyBehindTables meldete für lounge/mara/y=330 6859 abweichende Pixel. Isolierte Wiederholung ohne Codeänderung erfolgreich (1 Test, 0 Warnungen). Zweite Vollsuite ebenfalls 266/267, diesmal lounge/bruno/y=330 mit 6526 abweichenden Pixeln; alle Editorfälle bestanden.
- Unterstützende Testkorrektur zur verlässlichen Abschlussprüfung: Der bestehende Bildtest wartete nur zwei Animationsframes, obwohl die wirklichen Renderer neue Bildinstanzen laden; das Vorladen anderer Bildinstanzen gewährleistet deren Bereitschaft nicht. Mit vorübergehend um 150 ms verzögerten Bildantworten scheiterte derselbe Pixeltest reproduzierbar (Verdeckung hinter dem Tisch blieb bei 0 geänderten Pixeln). Ausschließlich der Test wurde auf Abschluss der tatsächlichen Bildanfragen vor erneuter öffentlicher render()-Messung umgestellt; die Pixelassertionen bleiben unverändert. Unter derselben Verzögerung danach grün (1 Test, 0 Warnungen). Diagnoseverzögerung entfernt. Gemeinsamer Spielrenderer und Quelldaten bleiben unverändert; keine zusätzliche Test-API oder neue Testgrenze. Abschließende Vollsuite nach dieser Korrektur erfolgreich: 267 bestanden, 0 Warnungen, 257,8 Sekunden.

## Konkrete manuelle Abnahme
Am 09.11.2026 separate Kopie von worlds/reference unter `C:\Users\MARTIN~1\AppData\Local\Temp\adventure-ticket17-acceptance-01a1c085` gestartet. Lokaler Server PID 58980, Editor: http://127.0.0.1:54235/editor.html?worldId=reference. Seite, Editorprogramm und Raumliste mit HTTP 200 geprüft. Dieser Start ist nur zur lokalen Abnahme, keine Bereitstellung; Änderungen betreffen ausschließlich die Kopie. Falls der Prozess beendet ist, Anwendung im Autorenmodus mit einer neuen Kopie starten. Die Bildschirmaufnahme reference-editor-lounge.png aus dem Browserlauf wurde visuell geprüft; gemeinsamer Vorschaurenderer und Editorbedienung sichtbar, keine Layoutänderung vorgenommen.

1. Lounge wählen und den Spawn „entry“ links unten bei (180,320) anklicken. Reine Auswahl darf „Ungespeichert“ nicht anzeigen. Im Inspektor X auf 185 setzen: Anzeige muss erscheinen.
2. Anderen Raum oder „Neu“ wählen und Verwerfen abbrechen: Lounge und X=185 bleiben erhalten. Browser neu laden und die echte Verlassenswarnung ebenfalls abbrechen: Bearbeitung bleibt erhalten.
3. Speichern, Erfolg abwarten, neu laden, Lounge und denselben Spawn wählen: X=185 bleibt gespeichert, Anzeige ist verborgen; Auswahl allein macht den Raum nicht ungespeichert.
4. Spawn auf (5,5) setzen und speichern: sichtbarer Validierungsfehler, Eingaben und Ungespeichert-Status bleiben erhalten. Auf (185,320) zurücksetzen und erfolgreich speichern.
5. Ergänzend Bodenpunkt ziehen, Hintergrund aus Galerie wählen oder Tischobjekt/Tiefenlinie bearbeiten: jede tatsächliche Änderung zeigt den Status. Vorschau betreten/verlassen und gespeicherte Daten erneut laden. Breite Räume behalten passende Koordinatengrenzen. Verzögerte Speicherantworten und weitere Bearbeitung während Speicherung sind automatisiert geprüft; künstliche Netzwerkbedienung ist für die menschliche Abnahme nicht erforderlich.
