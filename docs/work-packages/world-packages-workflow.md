# Integration und Startprompts für Tickets 09 bis 13

## Dokumentationsänderungen im Zielrepository

Diese neuen Dateien ergänzen den bestehenden Stand; Stammdokumente gezielt bearbeiten:

- ARCHITECTURE.md: Architekturziel aus world-format-compatibility.md übernehmen/verlinken; Welt-ID als Geltungsbereich für Räume, Ressourcen, Sitzungen und Chat festlegen.
- CONTENT-FORMAT.md: Manifestvertrag, Format-/Inhaltsversion, versionierte Offline-Schemas, Legacy-Zuordnung und Leser-/Schreiberregeln dokumentieren. Bestehendes version: 1 nicht still ersetzen. Nicht mehr aktuelle Entwurfsbehauptungen bei Umsetzung bereinigen.
- SPEC.md: bestätigte Erweiterung um externe Weltpakete, Referenzwelt und Weltwahl; keine Persistenz des laufenden Zustands.
- AGENTS.md: folgenden verbindlichen Absatz unter Technik/Qualität ergänzen.

> Abwärtskompatibilität bestehender Welten ist ein Architekturziel. Bei jeder Änderung am Autorenformat oder dessen Interpretation die Auswirkungen auf veröffentlichte Versionen prüfen, die SemVer-Einstufung begründen und Kompatibilität durch unveränderte ältere Referenzfixtures nachweisen. Veröffentlichte Schemas einschließlich Teilschemas bleiben unverändert. Server akzeptieren nur ausdrücklich unterstützte Formatversionen. Ohne Formatbezug genügt eine kurze begründete Feststellung im Review.

- TESTING.md: öffentliche Loader-, Dateisystem-, Editor- und Sitzungsgrenzen aus den Tickets nach Bestätigung aufnehmen; ältere Formatfixtures und Isolation bei identischen lokalen IDs testen.
- GLOSSARY.md: Welt, Weltpaket, Welt-ID, Start-Spawn, Formatversion, Inhaltsversion, Schema-Bündel, Abwärtskompatibilität und Legacy-Raumformat definieren.
- PLAN.md: 09 nach 08/07, 10 nach 09, 11 nach 09 und zur Gesamtabnahme 10; 12 nach 09 mit 11/10 für die Gesamtabnahme; 13 nach 09/11 und 12 für die Gesamtabnahme eintragen. 08 ist das separat vorbereitete Stabilisierungspaket, keine zweite Variante von 09.
- active-state.md: neue Planung nennen, tatsächlichen Stand von 08 klären, 09–13 als draft führen. Historische Testergebnisse nicht als Prüfung dieser Erweiterung darstellen. Als nächsten Schritt Paket 09 und Testgrenzen vorschlagen, sobald Blocker erledigt sind.

## Startprompt zur Vorbereitung

```text
Lies AGENTS.md und active-state.md. Integriere die bestätigten Anforderungen
aus docs/work-packages/world-format-compatibility.md gemäß dieser Anleitung
in die bestehenden Stammdokumente, ohne neuere Änderungen zu überschreiben.
Prüfe die tatsächliche Verfügbarkeit und den Status von Ticket 08.
Lies Ticket 09 und seine Blocker. Stelle Manifest, Legacy-Übernahme,
Versionsmatrix und öffentliche Testgrenzen konkret vor. Kläre ausschließlich
noch offene Details und warte gemäß Projektworkflow vor der Implementierung
auf Bestätigung. Keine Skills installieren, keine Remote-Schreibzugriffe.
```

## Nach Bestätigung: implement → tdd → code-review

```text
Nutze den tatsächlich installierten Matt-Pocock-Skill implement für das
bestätigte Ticket 09-world-packages.md im lokalen Tracker.
Lies seine aktuelle SKILL.md und die tatsächlich installierten Anweisungen
für tdd und code-review. Projektregeln und Benutzerauftrag haben Vorrang.
Arbeite nur am bestätigten Paket und den bestätigten öffentlichen Testgrenzen.
Halte die Architektur proportional und weise insbesondere Formatkompatibilität,
unveränderliche Schemas und den Legacy-Übernahmeweg nach.
Halte den Git-Ausgangspunkt vor Änderungen fest; führe lokale Checks aus
und prüfe den Diff gegen Ticket, Architekturziel und AGENTS.md.
Falls der installierte Review-Skill committete Änderungen voraussetzt,
beachte dessen tatsächlichen Ablauf und die lokale Commit-Autorisierung
in AGENTS.md. Kein Push, keine PR, kein Deployment.
Dokumentiere wirklich ausgeführte Checks, Reviewbefunde und manuelle Abnahme.
Setze erst ready-for-human; done verlangt ausdrückliche menschliche Abnahme.
Fehlende Skills offen nennen und ohne erfundene Skill- oder Subagentaufrufe
nach dem gleichwertigen lokalen Ablauf vorgehen.
```

Für Tickets 10 bis 13 Nummer und Pfad ersetzen und die im jeweiligen Ticket genannten Blocker nachweisen.
Die Skill-Zuordnung übernimmt SKILLS.md des geprüften Repositorystands.
Für dieses Dokument wurden keine Skills installiert oder ausgeführt und deren
heutige Remote-Versionen nicht neu verifiziert. Vor Umsetzung gelten die
konkret installierten Anweisungen; keine Parallel-Agenten automatisch starten.

## Manuelle Abnahme

09: Referenzwelt extern kopieren, konfigurieren, starten, Raum bearbeiten,
neustarten und wieder betreten. Unterstützte ältere Fixture erfolgreich laden;
nicht unterstützte Version mit verständlichem Fehler ablehnen.
10: Zwei Welten mit identischen lokalen IDs anbieten. Zwei Browser erst getrennt,
dann gemeinsam betreten lassen; Bilder, Bewegung, Chat, Raumwechsel und Rückkehr
zur Auswahl prüfen. Neustart führt erneut zum Start-Spawn.

## Stand dieser Lieferung

Nur Planung und Übergabe wurden erstellt und auf lokale Markdown-Verweise,
Ticketnummern sowie Archivinhalt geprüft. Keine Implementierung, Builds,
Produkttests, Produktabnahme, Commits oder Remote-Schreibzugriffe durchgeführt.

## Darstellung und Demo-Anforderungen aus Tickets 11 bis 13

- Ticket 11: Draufsicht und feste Figurengröße pro Raum, ohne Laufzeit-Zoom; Labyrinth zur Prüfung vorhandener Wegfindung. Separate Laufgeschwindigkeit bleibt ein ausdrücklich zu bestätigender Vorschlag.
- Ticket 12: horizontale, lokale Kamera mit Ruhebereich und korrekter Umrechnung zwischen Bildschirm- und Raumkoordinaten. Funktioniert auch in Kartenansichten.
- Ticket 13: zwei horizontale Bezugslinien mit kontinuierlich interpolierten, begrenzten Größenfaktoren. Fußpunkt bleibt verankert, Raumkoordinaten und Kameraoffset sind unabhängig.
- Jede mitgelieferte Demo-Welt enthält drei erreichbare Beispiele: Scrollen ohne Tiefe, Tiefe ohne Scrollen und beides kombiniert. Mindestens ein scrollender Raum ist eine Kartenansicht; vorzugsweise der reine Scrollraum. Die Tiefengestaltung muss optisch schlüssig sein, etwa ein nach hinten ansteigender Hügelweg. Keine neue Klettermechanik.
- ARCHITECTURE.md und CONTENT-FORMAT.md um diese optionalen Einstellungen, kompatible Defaults und den lokalen Charakter der Kamera ergänzen. GLOSSARY.md und TESTING.md entsprechend aktualisieren. Ältere externe Welten und eingefrorene Referenzfixtures müssen keine neuen Demo-Räume erhalten.

## Abnahme der Darstellungsfunktionen

11: Karte mit Draufsichtfigur und zwei Maßstäben prüfen, Labyrinth durchlaufen und normale Räume betreten.
12: Breiten Raum in beide Richtungen durchlaufen, Ausgänge bei verschobenem Bild anklicken und zwei unabhängige Kameras im Multiplayer prüfen.
13: Alle drei Demo-Beispiele besuchen, kontinuierliche Größenänderung und festen Fußpunkt prüfen; Kombination mit Scrollen und Kartenansicht nachweisen.

