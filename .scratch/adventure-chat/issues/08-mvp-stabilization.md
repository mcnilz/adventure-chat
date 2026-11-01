# 08: Multiplayer-Zustand und Autorenbetrieb zuverlässig machen

**What to build:** Der vorhandene Adventure-Chat erhält eine serverseitig verlässliche Bewegung, konsistente Raumbeitritte, einen expliziten lokalen Autorenmodus und geprüfte YAML-Inhalte. Bestehende Bedienung und Darstellung bleiben erhalten.

**Blocked by:** 07 (laut vorhandenem Tracker done)

**Status:** done

**Revision:** 1 · 01.11.2026

**Reviewbasis:** `3d355bd65a43618264d1de4382cdc3c960b91cba`. Vor Implementierung aktuellen Stand und mögliche zwischenzeitliche Fehlerbehebungen prüfen.

## Problem und Ziel

Die MVP-Funktionen sind vorhanden. Die Codeprüfung zeigt jedoch Lücken gegenüber SPEC.md: veraltete Bewegungs-Snapshots, vom Client erzwingbare Positionen und Raumwechsel, unvollständige Bereinigung bei wiederholtem Join, standardmäßig aktiver Editor und ungeprüft geladene YAML-Dateien. Die bisher gemeldeten 106 grünen Tests belegen diese Eigenschaften nicht vollständig.

Das Paket stellt diese vorhandenen Zusagen her. Es führt keine neuen Spielfunktionen und keinen öffentlichen Dienst ein. Die frühere menschliche Abnahme der Tickets 01–07 wird nicht rückwirkend geändert.

## Verbindlicher Umfang und vorgeschlagene Detailentscheidungen

Die folgenden Entscheidungen bilden den Vorschlag für dieses Ticket. Paketumfang und Testgrenzen werden vor Implementierungsbeginn gemäß AGENTS.md bestätigt.

### A. Bewegung: eine serverseitige Wahrheit

- Der Client sendet ein Ziel, keine verbindliche Startposition. Eine optionale Clientposition darf die Serverposition nicht überschreiben.
- Der Server bestimmt Weg und Geschwindigkeit. Bevorzugte kleine Lösung: Weg plus serverseitige Startzeit speichern und die aktuelle Position anhand verstrichener Zeit berechnen; kein permanenter Simulationstakt erforderlich.
- Neue Bewegungsziele starten an der aktuellen Serverposition, auch während eines laufenden Weges. Ungültige Ziele verändern den bestehenden gültigen Weg nicht.
- Snapshots enthalten die aktuelle Position und nur noch relevante Restbewegung. Nach abgeschlossener Bewegung enthalten sie den Zielpunkt, Zustand idle und keinen veralteten Weg.
- Browser interpolieren diese Daten. Für Hintergrundtabs muss verstrichene Zeit berücksichtigt werden; gedeckelte Frame-Schritte dürfen keinen dauerhaften Positionsrückstand verursachen.
- Der Hub-Befehl UpdateProgress wird entfernt oder so begrenzt, dass er keine Positionen und Zustände erzwingen kann. Keine zweite ungesicherte Schreibmöglichkeit behalten.
- Für Zeitberechnungen bevorzugt .NET TimeProvider verwenden. Tests brauchen keine sekundenlangen Sleeps und keine zusätzliche Bibliothek.

### B. Beitritt, Raumwechsel und Wiederverbindung

- Pro Verbindung existiert höchstens eine Sitzung. Vorschlag: erneutes JoinRoom auf derselben bereits beigetretenen Verbindung mit verständlichem Fehler zurückweisen, ohne Zustand oder Gruppen zu ändern.
- Raumwechsel erfolgen über die ID eines Ausgangs im aktuellen Raum. Zielraum und Zielspawn leitet der Server aus der Raumdefinition ab.
- Der Server prüft die Nähe zum Ausgang anhand der aktuellen Serverposition. Toleranz passend zur bestehenden Geometrie zentral festlegen und dokumentieren. Bei knappem Zeitversatz darf der Client bei Ankunft kontrolliert erneut versuchen; keine clientseitige Teleport-Freigabe.
- Fehlerhafte Ausgangs-ID, zu große Entfernung oder ungültige Zielreferenz: Sitzung bleibt unverändert im alten Raum.
- SignalR-Gruppen, Teilnehmerlisten und Sitzung stimmen nach Wechsel und Disconnect überein. Der alte Raum erhält genau die passende Abmeldung.
- Ereignisse und Snapshots tragen ausreichenden Raum-/Bewegungsbezug, damit verspätete Ereignisse des alten Raums und überholte Bewegungen den neuen Zustand nicht verändern. Die kleinste passende Sequenz-/Revisionslösung wählen.
- Echte automatische Wiederverbindung übernimmt die vorhandene MVP-Regel: frische Sitzung am Spawn, keine Fortsetzung der alten Sitzung. Nach serverseitiger Bereinigung bleibt genau eine Figur sichtbar.
- Chatverhalten außerhalb dieser Zustellungsregeln nicht neu gestalten. Bestehende Raumhistorie und deren Produktentscheidung nicht beiläufig ändern.

### C. Expliziter lokaler Autorenmodus

- Ohne Schalter sind Editor-APIs deaktiviert, einschließlich Schreib- und Uploadzugriff. Vorgeschlagene Antwort für die API: 403. Keine Dateiveränderung.
- Der dokumentierte Schalter `--authoring` aktiviert den lokalen Autorenmodus tatsächlich. Schalter vor Übergabe an den allgemeinen Konfigurationsparser gezielt auswerten.
- Loopback-Beschränkung beibehalten und fehlende Remote-IP nicht als pauschale Freigabe behandeln.
- Host-/Origin-Prüfung und Schutz schreibender Aufrufe gegen fremde Webseiten gemäß ARCHITECTURE.md umsetzen. Vorschlag: Same-Origin-Prüfung plus sitzungsgebundenes Autorentoken für Editor-Schreibzugriffe; vorhandene ASP.NET-Core-Mittel bevorzugen. Kein öffentliches Login-System bauen.
- Im Spielmodus bietet editor.html keine nutzbare Autorenoberfläche; vorzugsweise 404 statt einer scheinbar funktionsfähigen Seite.
- Uploads auf das zugesagte PNG-Format begrenzen und Dateisignatur sowie 10-MiB-Grenze prüfen. Dateiendung allein genügt nicht. Keine SVG-Uploads; keine neue Bildbibliothek ohne begründeten Bedarf.

### D. YAML beim Laden prüfen

- Der reguläre Inhalte-Ladeweg verwendet die Schema-/Fachvalidierung wie der Speicherweg, bevor Raumdefinitionen registriert werden; raumübergreifende Referenzen nach dem Einlesen aller Kandidaten prüfen.
- Ungültiges YAML, doppelte Schlüssel, unbekannte Versionen, ungültige Geometrie sowie fehlende Zielräume oder Spawns liefern verständliche Diagnosen mit Datei und Ursache.
- Vorschlag für den Start: Bei ungültigen vorhandenen Inhaltsdateien Start abbrechen und Fehler melden; kein stiller Ersatz durch eingebaute Beispielräume. Fehlender Pflichtschema-Inhalt ist ebenfalls ein Konfigurationsfehler.
- Nicht vorhandene Referenzen während schrittweisen Editor-Anlegens bleiben speicherbar, soweit dies für zwei sich gegenseitig referenzierende neue Räume nötig ist; die vorhandene Weltprüfung meldet sie. Strenge Prüfung beim Weltstart verlangt vollständig gespeicherte Inhalte.
- Gültige Raumdaten nach Neustart unverändert nutzbar; atomare Speicherung und Dateipfadbegrenzung bleiben erhalten.

### E. Reproduzierbare Übergabe

- NuGet-Lockfiles für Server und beide Testprojekte einchecken; dokumentierter Restore im Locked Mode. npm-Lockfile beibehalten, dokumentierter Installationsweg mit npm ci.
- SignalR-Verbindungsobjekt und Bibliothekszugriff mit den vorhandenen Pakettypen typisieren; kein explizites any in der angefassten Netzwerkschnittstelle. Kein Bundler allein für diesen Zweck.
- README, ARCHITECTURE.md, TESTING.md und active-state.md an das tatsächlich implementierte Verhalten anpassen. Veraltete Aussagen wie „keine Implementierung vorhanden“ beseitigen, ohne frühere Testläufe als eigene neue Nachweise auszugeben.
- Voraussetzungen für Playwright-Browserinstallation und Start aus einem frischen Checkout dokumentieren.

## Acceptance criteria

- [ ] **AC1:** Später Eintritt während und nach Bewegung liefert aktuelle Position und korrekten Restweg; kein erneutes Abspielen eines abgeschlossenen Weges.
- [ ] **AC2:** Zieländerung während des Laufens bleibt kontinuierlich und innerhalb der Geometrie; Client-Startposition und UpdateProgress können keine Position erzwingen.
- [ ] **AC3:** Raumwechsel nur über einen gültigen, erreichbaren Ausgang in Servernähe; Ablehnung verändert weder Sitzung noch Gruppenzustellung.
- [ ] **AC4:** Wiederholter Join derselben Verbindung erzeugt weder Doppelgänger noch zusätzliche Raumabonnements.
- [ ] **AC5:** Nach Wechsel kommen keine neuen Nachrichten/Bewegungen aus dem alten Raum an; verzögerte alte Ereignisse überschreiben den aktuellen Zustand nicht.
- [ ] **AC6:** Echte automatische Wiederverbindung führt nach Bereinigung zu genau einer neuen Sitzung; Reload allein gilt nicht als Nachweis.
- [ ] **AC7:** Standardstart sperrt Editor und Autoren-APIs. --authoring ermöglicht lokale Bearbeitung; nicht lokale und fremde Webseiten erhalten keinen Schreibzugriff.
- [ ] **AC8:** PNG-Upload funktioniert, falsche Dateisignaturen, SVG und übergroße Dateien werden ohne Überschreiben vorhandener Dateien zurückgewiesen.
- [ ] **AC9:** Regulärer Start akzeptiert gültige YAML-Welt und meldet ungültige Dateien/Referenzen nachvollziehbar; kein stilles Verschlucken.
- [ ] **AC10:** Typcheck, Clientbuild, Locked Restore und gesamte NUnit-/Playwright-Suite erfolgreich; echte Ergebnisse und Grenzen im Abschluss dokumentiert.
- [ ] **AC11:** Anleitung funktioniert mit frischem Checkout; keine Node-Serverlaufzeit, keine neue Engine-/Pluginarchitektur und keine unbegründeten Abhängigkeiten.

## Testgrenzen – vor Beginn bestätigen

| Öffentliche Grenze | Nachweis | Grenze des Nachweises |
| --- | --- | --- |
| Echter Server mit SignalR-Clients | manipulierte Kommandos, Join/Wechsel, Gruppenisolation, aktueller Snapshot | kein Pixellayout und kein subjektives Laufgefühl |
| Öffentlicher Bewegungs-/Sitzungszugang mit kontrollierter Zeit | Zwischenposition, Restweg, Zieländerung, abgeschlossene Bewegung | kein Browsertransport; keine privaten Felder auslesen |
| Browser mit zwei isolierten Kontexten | sichtbare Kontinuität, Hintergrundtab-Aufholen, echter Reconnect, Türen und Chat | nicht alle numerischen Geometriefälle |
| Regulärer App-Start und Inhalte-/Editor-API mit temporärem Verzeichnis | Autorenmodus, Origin/Host/Token, Uploadgrenzen, YAML-Laden und Neustart | keine Produktionsinfrastruktur |

Für die kontrollierte Zeit die bestehende öffentliche Sitzungsgrenze nutzen; keine öffentliche Test-API einführen. Die ausdrückliche Bestätigung dieses Ticketumfangs umfasst diese Tabelle. Danach nur bei tatsächlicher Erweiterung erneut fragen.

## Gezielte Regressionen

1. Bekannter gerader Weg, bekannte Geschwindigkeit: Teilnehmer B tritt ein, nachdem A einen Teil zurückgelegt hat; Position und Restweg gegen unabhängig berechnete Erwartung prüfen. Zusätzlich Eintritt nach Ankunft: idle am Ziel.
2. Während Bewegung neues Ziel setzen; Start muss zeitlich aktuelle Serverposition sein. Manipulierte Start-/Fortschrittswerte dürfen nicht übernommen werden.
3. Ausgang von fern, unbekannter Ausgang und Zielraum-/Spawn-Manipulation ablehnen; gültigen Ausgang aus Nähe annehmen. Danach Zustellung in beiden Räumen prüfen.
4. Join zweimal auf derselben tatsächlichen Hub-Verbindung senden; Anzahl sichtbarer Figuren und Raumzustellung bleiben korrekt.
5. Transport gezielt unterbrechen, automatische Wiederverbindung tatsächlich abwarten, neue Sitzung und Bereinigung der alten beobachten. Test darf nicht nur ReloadAsync ausführen oder kurzzeitig die alte Figur zählen.
6. Verzögertes Bewegungsereignis nach Raumwechsel und Hintergrundtab-Wiederaufnahme prüfen. Tests beobachten aktuellen Raum und Position statt nur die irgendwann erreichte Zielposition.
7. Start ohne/mit --authoring; nicht lokale IP und fremde Origin/Host prüfen. Upload mit PNG-Endung und falschem Inhalt muss scheitern.
8. Regulärer Start mit ungültiger handbearbeiteter YAML-Datei und mit gültiger Zwei-Raum-Welt. E2E-Inhalte ausschließlich in isolierten temporären Verzeichnissen; keine Repository-Inhalte verändern.

Nicht alle Tests vorab schreiben. Jeweils einen Fehler reproduzieren, roten Nachweis festhalten und minimal korrigieren. Vorhandene zu schwache Tests gezielt verbessern statt nur Testanzahl erhöhen. NUnit, keine FluentAssertions.

## Reihenfolge und Review

A → B → C → D → E, seriell. Innerhalb jedes Schritts red → green; Strukturverbesserungen im Review. Für den festen Review-Ausgangspunkt, Skill-Kompatibilität und Startprompts siehe [Ausführungsleitfaden](../../../docs/work-packages/08-stabilization-workflow.md).

## Nicht enthalten

Inventar, Dialoge, Skripting, neue Grafiken, neue Räume, persistente Chatarchive, Registrierung, öffentliche Moderation, Hosting, horizontale Skalierung, neue CI-Plattform oder umfassende Refaktorierung. Kein Push, PR oder externer Tracker. Neue Dependency nur mit bestehender Projektprüfung.

## Fachkontext

[SPEC.md](../../../SPEC.md), [ARCHITECTURE.md](../../../ARCHITECTURE.md), [TESTING.md](../../../TESTING.md), [CONTENT-FORMAT.md](../../../CONTENT-FORMAT.md), [GLOSSARY.md](../../../GLOSSARY.md). AGENTS.md gilt.

## Abschlussbelege

- Basis `ef43f41`; Umsetzung `5e14aaf`, Reviewkorrekturen `07ae33b` und `a3e8de5`, Hintergrundtab-Test `e47f0ce`, Abschlussdokumentation und Uploadtest `49f2976`.
- Rot und anschließend grün nachgewiesen: gefälschte Startposition (X=200 statt 120), Snapshot während Bewegung (X=120 statt 185), zweiter Join, freier Zielraumwechsel, langer Browserframe (X=133 statt 220), Editor-API im Standardstart, fremder Host, fehlendes Autorentoken, falsche PNG-Signatur, ungültiges YAML und fehlende Pflichtversion. Weitere gezielte Tests prüfen unter anderem echte SignalR-Manipulation, verspätete Ereignisse, Reconnect über eine unterbrochene lokale TCP-Verbindung, eingefrorenen Browsertab, Ausgangsradius, fehlende Zielreferenz und unveränderte Datei bei ungültigem Upload. Für die Reviewkorrekturen wurden die Tests nach der Korrektur ergänzt; ein roter Vorlauf wurde für diese nicht behauptet.
- Checks: `npm ci --ignore-scripts --offline`, Client-Typcheck und Clientbuild erfolgreich; `dotnet restore --locked-mode` mit lokalem Paketcache und separatem NuGet-Scratch erfolgreich; .NET-Build ohne Warnungen; NUnit-Serversuite 87/87 und Playwright-Browsersuite 43/43 erfolgreich. Tests verwenden isolierte temporäre Inhalte. Ein vorheriger gemeinsamer Lauf scheiterte beim Browserstart mit `EPERM`; separate Läufe bestanden. Ein Offline-Reconnect-Test war instabil und wurde durch eine echte Verbindungsunterbrechung ersetzt.
- Review mit zwei parallelen Subagenten gegen `ef43f41`: Standards fand doppelte Zugriffskontrolle; Spec fand Snapshot-/Ereignisverluste beim Wechsel und zu große Client-Ausgangsschwelle. Die Nachprüfung fand denselben Snapshot-Fall beim Join/Reconnect. Alle Befunde wurden korrigiert und von den jeweiligen Reviewern nachgeprüft; keine weiteren konkreten Befunde gemeldet.
- Manuelle Abnahme: Zwei Browser öffnen; während und nach einer Bewegung beitreten, Ziel im Lauf ändern und einen Tab pausieren/fortsetzen. Durch einen Ausgang wechseln, Chat und Teilnehmer in beiden Räumen beobachten und eine Verbindung tatsächlich unterbrechen. Ohne `--authoring` Editor und API prüfen; mit Schalter lokal PNG-Raum bearbeiten, speichern und nach Neustart betreten. Eine Kopie der YAML-Datei absichtlich beschädigen, Fehlermeldung prüfen und Datei wiederherstellen.
- Grenzen: Browser- und Transporttests decken nicht jede mögliche Ereignisreihenfolge oder Produktionsinfrastruktur ab. Kein Test ersetzt die menschliche Prüfung von Bewegung, Darstellung und Editorbedienung.
- Menschliche Abnahme: Am 01.11.2026 bestätigte der Benutzer das vorgestellte Ergebnis mit „passt“. Einzelne manuelle Prüfschritte wurden dabei nicht gesondert dokumentiert.
