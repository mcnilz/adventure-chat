# 18: Gesamte Spieloberfläche an die Fenstergröße anpassen

**What to build:** Spielfeld, Aktionszeile, Chatverlauf, Eingabe und Spielbedienung passen gemeinsam vollständig in das Browserfenster. Kleine Fenster verkleinern die gesamte Oberfläche proportional. Ein umschaltbarer Button nutzt auf Wunsch die verfügbare Fensterfläche auch zum Vergrößern.

**Blocked by:** keine; Tickets 01–17 sind done.

**Status:** ready-for-human

## Auftrag und vorgeschlagene Detailentscheidungen

Benutzerauftrag vom 09.11.2026: Bei geringer Fensterhöhe wird unten Chat abgeschnitten; alles soll passend skalieren. Bei deutlich größeren Fenstern soll ein Button das Hochskalieren erlauben.

- Standardmodus: bisherige Maximalbreite von 960 CSS-Pixeln beibehalten, bei Platzmangel anhand von Breite UND Höhe proportional verkleinern.
- Alternativmodus: „Auf Fenstergröße“ vergrößert die gesamte Oberfläche proportional bis zur verfügbaren Breite oder Höhe; „Normalgröße“ schaltet zurück. Gemeint ist die Browser-Innenfläche, kein Betriebssystem-Vollbild.
- Zentrierung, Seitenverhältnis und Pixelartdarstellung erhalten; Chat und Bedienelemente skalieren mit.
- Beide Modi passen sich nach Fensteränderungen automatisch an. Modus gilt für die aktuelle Seite; keine neue dauerhafte Einstellung.
- Nur Spielansicht einschließlich „Zur Weltwahl“; kein Editorumbau oder Umbau der Welt-/Figurenwahl.
- Keine Änderung an Raumkoordinaten, Bewegung, Kamera, Figurenhöhe im Raum, Autorenformat oder Serververtrag. Keine neuen Abhängigkeiten vorgesehen.

## Acceptance criteria

- [x] Bei niedrigen und schmalen Fenstern bleiben Spielfeld, Aktionszeile, Chatverlauf, Eingabe, Senden und Spielbedienung innerhalb der sichtbaren Browserfläche.
- [x] Alle Bestandteile werden mit demselben Faktor skaliert; das Spielfeld bleibt unverzerrt.
- [x] Standardmodus vergrößert nicht über die bisherige Maximalbreite hinaus.
- [x] Der beschriftete, per Tastatur bedienbare Button schaltet zwischen Standard und maximal passender Fenstergröße um; Zustand und Rückweg sind erkennbar.
- [x] Beide Modi reagieren korrekt auf Fensterverkleinerung und -vergrößerung, auch nach Raumwechsel.
- [x] Klickbewegung trifft nach Skalierung weiterhin die logischen Raumkoordinaten; Chat bleibt nutzbar und löst keine Bewegung aus.
- [x] Bestehende Tests bleiben grün; Ticket und active-state.md enthalten tatsächliche Belege und manuelle Abnahmeschritte.

## Testgrenzen – vor Implementierung bestätigen

Bestehende öffentliche Browser-/Canvasgrenze. Zuerst fehlschlagender Browsernachweis für abgeschnittene Chat-Eingabe bei geringer Höhe, danach Korrektur. Vorgeschlagene Fenster: 1280×480, 640×360, 800×900 und 1920×1080. Prüfen: sichtbare Außenkanten aller Bereiche, proportionaler Maßstab, Umschalten in beide Richtungen, Resize in beiden Modi, Bewegung an bekanntem Raumziel und Chatversand. Bestehende Raumwechsel-/Scrollingtests als Regression. Typcheck, Clientbuild und vollständige .NET-/Browsersuite gemäß Projektregeln.

Automatisierung sichert Geometrie und Bedienabläufe, ersetzt keine menschliche Beurteilung von Lesbarkeit und Pixelartwirkung. Bei extrem kleinen Fenstern wird entsprechend dem Auftrag alles kleiner; keine Mindestgröße mit Abschneiden einführen.

## Stand

09.11.2026: Umfang, vorgeschlagene Detailentscheidungen und Testgrenzen vom Benutzer mit „ja genau“ bestätigt. Umsetzung begonnen; zunächst Browsernachweis für das Abschneiden bei niedriger Fensterhöhe.

## Umsetzung und Nachweise (09.11.2026)

- Fehler vor der Korrektur reproduziert: `dotnet test tests/AdventureChat.E2ETests --no-restore --filter FullyQualifiedName~ResponsiveLayoutTests` scheitert bei 1280×480. Unterkante der Chat-Eingabe 574,875 statt höchstens 480,5 CSS-Pixel. Ein vorheriger Lauf wies zusätzlich die oben abgeschnittene Oberfläche nach (Oberkante −104,875).
- Ursache: Die bisherige Breitenbegrenzung berücksichtigt die gesamte Höhe aus Canvas, Aktionszeile und Chat nicht; Zentrierung und versteckter Seitenüberlauf schneiden die übergroße Oberfläche ab.
- `GameViewport` misst die untransformierte Gesamtoberfläche und die verfügbare Fensterfläche. Ein gemeinsamer Transform skaliert alle Elemente proportional; ResizeObserver reagiert auf Fensteränderungen und andere Raumseitenverhältnisse. Unveränderte Koordinatenumrechnung liest das tatsächliche Canvas-Rechteck.
- Kompakte Spielleiste mit „Zur Weltwahl“ und dem umschaltbaren Größenbutton; explizite Beschriftung, sichtbarer Tastaturfokus und `aria-pressed`. Beobachter und Buttonlistener werden bei fehlgeschlagenem Spielstart entfernt. Keine Import-Seiteneffekte.
- Typcheck und Clientbuild erfolgreich. Sieben neue Browserfälle erfolgreich: vier anfängliche Fenstergrößen, proportionale Anpassung inklusive echter Klickbewegung und Chat, Vergrößern/Resize/Rückkehr per Enter/Leertaste sowie echte Raumwechsel zwischen 640×360 und 640×540 in beiden Modi samt Rückkehr zur Weltwahl. Der höhere Testraum und sein gültiges PNG werden ausschließlich in einer temporären Weltkopie erzeugt.
- Screenshots `scaling-small.png`, `scaling-normal.png`, `scaling-expanded.png` im Browser-Testausgabeverzeichnis. Kleine und vergrößerte Darstellung visuell geprüft: vollständig, zentriert, Seitenverhältnis erhalten.
- Korrigierte Testaufbaufehler: Button nach Beschriftungswechsel über stabile ID abfragen; bei Mausereignissen die Rundung auf CSS-Pixel berücksichtigen (kleinste Testdarstellung: bis 1,5 Raumpixel je CSS-Pixel, Toleranz 2); zum höheren Testraum ein PNG passender Größe erzeugen. Keine Änderung an Eingabe-/Spiel-/Inhaltslogik dafür.
- Ein eingeschränkter Teststart hing vor Testausführung; Wiederholung mit nötigen Prozessrechten erfolgreich. Abschließende Vollsuite: `dotnet test --no-restore --logger "console;verbosity=minimal"`, 275 Tests in zwei Projekten bestanden, 0 Warnungen (294,2 Sekunden). `git diff --check` erfolgreich.

## Review und Kompatibilität

Eigener Funktions- und Standardsreview gegen Ticket, SPEC.md, Architektur und Coding Standards: gekapselte UI-Verantwortung, strikte Typisierung, keine neue Test-API, keine Änderung an Renderer, Kamera, Simulation, Server oder Editor. Keine offenen Befunde im überprüften Diff. Keine Subagents eingesetzt.

Kein Formatbezug: Autorenformat und dessen Interpretation bleiben unverändert; die Anpassung betrifft ausschließlich die lokale Bildschirmdarstellung. Keine neue Format-/Inhaltsversion und keine neuen direkten oder transitiven Abhängigkeiten. Veröffentlichte Schemas, ältere Referenzfixtures und Benutzerbilder unverändert.

## Manuelle Abnahme

Lokale Spielansicht: http://127.0.0.1:54236/ (separater Spielprozess ohne Autorenmodus; Welt `D:/adventure-chat/worlds/reference`). HTML und neues Skalierungsmodul per HTTP geprüft. Bestehender Editor auf Port 54235 bleibt unverändert. Die neue Spielansicht verwendet die gebaute Anwendung und die aktuellen Clientdateien; Start ohne Build, daher kein Austausch der Editorkopie.

1. Spiel öffnen und Welt betreten. Fenster bei ausreichend Breite auf ungefähr 480 Pixel Höhe verkleinern; anschließend schmaler machen. Canvas, Aktionszeile, Chatverlauf, Eingabe, Senden und obere Spielleiste müssen komplett sichtbar bleiben und gemeinsam schrumpfen.
2. Fenster deutlich vergrößern. „Auf Fenstergröße“ klicken: die gesamte Oberfläche wächst bis zur verfügbaren Höhe oder Breite. Fenster in diesem Modus erneut verkleinern und vergrößern. „Normalgröße“ begrenzt wieder auf die bisherige Größe.
3. In beiden Modi auf den Boden klicken, eine Nachricht senden und einen Raum wechseln. Zielklicks müssen passen, Chat und Weltwahl bleiben erreichbar. Lesbarkeit und Pixelartwirkung beurteilen.

Menschliche Abnahme ausstehend. Keine Abnahme aus automatisierten Tests abgeleitet.

Lokaler Implementierungscommit: `Fit complete game surface to browser window (ticket 18)`; kein Push.
