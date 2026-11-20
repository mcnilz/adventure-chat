# 23: Interaktive Referenzbeispiele für Tür, Stuhl und Schalter/Tor

**What to build:** Eine kleine erreichbare Referenzszene demonstriert die Erweiterung vollständig: Tür öffnen/schließen, Stuhl zwischen „am Tisch“ und „zurückgeschoben“ bewegen und ein Tor über einen direkt verbundenen Schalter bedienen. Zwei Spieler können Änderungen gegenseitig rückgängig machen und die geänderten Wege nutzen.

**Blocked by:** [20](20-shared-object-actions.md), [21](21-state-dependent-navigation.md), [22](22-verbs-and-interaction-editor.md) und [19](19-reference-world-art-polish.md) sind done.

**Status:** ready-for-human

## Auftrag und Freigabegrenze

Am 10.11.2026 ausschließlich Planung beauftragt. Jetzt keine Bilder erzeugen, Weltdateien anpassen, Server starten oder bestehende Abnahmeumgebungen verändern. Die spätere Inhaltsintegration benötigt eine separate Paketfreigabe. [SPEC](../../../SPEC.md), [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md).

## Umfang und vorgeschlagener Zuschnitt

- Eine Handvoll nutzbarer Raumobjekte in der mitgelieferten Referenzwelt: Tür, Stuhl, Schalter und verbundenes Tor. Exakte Szene/Einbindung vor Umsetzung anhand des abgenommenen Stands von 19 auswählen; vorzugsweise eine ergänzende kleine Interaktionsszene, die die vorhandene Tour erhält.
- Türzustand ändert Darstellung und Durchgang. Stuhl blockiert in beiden festen Positionen an der passenden Stelle. Drücken/Ziehen ist reversibel; keine freie Positionierung im Spiel.
- Schalter und Tor ändern sich mit einer expliziten Aktion konsistent. Keine automatische Kette, kein Inventar, kein Aufheben und keine Dialoge. Szene erlaubt Gegenaktionen durch den jeweils anderen Spieler.
- Geeignete freie Flächen und Sicherheitsanker machen erfolgreiche Aktionen möglich. Ein dokumentierter sicherer Prüfplatz demonstriert eine wegen Figur/Sicherheitsregel abgelehnte Änderung ohne jemanden festzusetzen.
- Vorhandene freigegebene Bilder bevorzugen. Fehlende geeignete Tür-/Stuhl-/Tormotive und deren Beschaffung vor Paketfreigabe klären; dieser Plan autorisiert keine neue Bilderzeugung. Herkunft/Lizenz/Originalerhalt auch bei späterer Bearbeitung dokumentieren.

## Acceptance criteria

- [x] Szene ist über regulären Eintritt und dokumentierten Rundweg erreichbar. Zwei unabhängige Browser können alle vier Objekte über die Verbenbedienung nutzen, gegenseitig zurückstellen und weiter laufen/chatten.
- [x] Geöffnete Tür/Tor geben die konfigurierten Wege frei; geschlossene sperren. Stuhlposition und Hindernisfläche stimmen visuell überein. Laufende Route reagiert wie in 21 abgenommen; keine optisch offene, geometrisch unerreichbare Passage.
- [x] Direkte Schalter-/Torwirkung und Sicherheitsablehnung sind an der Szene nachvollziehbar. Sichere Gegenaktion funktioniert anschließend. Keine Rätselvoraussetzung verhindert die Demonstration.
- [x] Später eintretender Spieler, Raumrückkehr und Reconnect sehen den aktuellen Zustand. Neustart der Welt zeigt deklarierte Anfangszustände; keine Zustandspersistenz hinzufügen.
- [x] Dieselben Definitionen sind im Browsereditor vollständig sichtbar und bearbeitbar. Speichern/Neuladen und isolierte gemeinsame Vorschau demonstrieren dieselben Aktionen, Hindernisse und sicheren Ablehnungen. Für manuelle Bearbeitung eine Arbeitskopie verwenden, keine eingefrorene Fixture.
- [x] Bestehende Referenzräume/Tour, Figuren, Scrollen/Tiefe und Weltisolation bleiben prüfbar. Autorenoriginale, veröffentlichte Schemas und sämtliche älteren Referenzfixtures bleiben unverändert. Keine bestehende Benutzer-Weltkopie überschreiben.
- [x] Verwendete Formatversion entspricht dem dann veröffentlichten Vertrag aus 20/21, für 21 nun 1.8.0 geplant. Kein neues Format allein für Beispiele. Inhalts-SemVer vor Integration begründen: aktuell reference 3.0.0; rein zusätzliche erreichbare Szene mit erhaltenen Zusagen kann Minor sein, geänderte bisherige Laufbereiche/Spawns/Übergangssemantik verlangen nach Projektvertrag Major. Konkrete Inhaltsversion erst mit Szenenentscheidung festlegen.
- [x] README/Herkunftshinweise, Editor-Demo, tatsächliche Testbelege, Grenzen und verständliche manuelle Abnahmeschritte ergänzen; ready-for-human, erst ausdrückliche Abnahme done.

## Vorgeschlagene Testgrenzen – vor Implementierung bestätigen

Öffentliche Inhalts-/Navigationsgrenze und echte Browser-/SignalR-Abläufe mit zwei Teilnehmern sowie einem späteren Eintritt. Alle reversiblen Aktionen, konsistente Verknüpfung, Tür-/Stuhlwege, abgelehnte Überdeckung/Einschließung, Änderung während Bewegung, Raumrückkehr, echter Reconnect und Neustart in isolierter Weltkopie. Editor laden/speichern/neuladen und dieselbe Szene in isolierter Vorschau bedienen. Bestehende Tour und ältere unveränderte Fixtures als Regression; Typcheck/Build und vollständige einschlägige Suite.

Screenshots in beiden Stuhl-/Türzuständen und mit zwei Figuren ergänzen den Funktionsnachweis. Grafische Glaubwürdigkeit, Bediengefühl und sichtbare Verbindung zwischen Schalter und Tor bleiben menschliche Abnahme. Keine Aussage über beliebige vom Autor gebaute Rätsel oder global einschließungsfreie Welten.

## Offene Detailentscheidungen vor Paketfreigabe

Ort/Anbindung der Szene, geeignete vorhandene oder separat freizugebende Grafiken, Namen/Bedienpunkte und konkrete Sicherheitsanker anhand der in 21 bestätigten Regel. Inhaltsversion begründen. Kein neuer Featureumfang oder neues Bildverfahren aus diesem Planungsticket ableiten.

## Stand / Abnahme

10.11.2026: draft, nur Planung; keine Welten oder Bilder verändert, keine Implementierung oder Testausführung behauptet.

20.11.2026: Alle Vorgänger sind abgenommen. Zur Freigabe vorgeschlagen ist ein zusätzlicher Nebenraum mit Zugang und Rückweg am Garten. Der reguläre Start in der Lounge und die bisherige Tour bleiben erhalten. Tür, Stuhl, Schalter und Tor aus der abgenommenen Testwelt von Ticket 22 dienen als Ausgangspunkt. Platzierung und Darstellung an die Referenzwelt anpassen. Neue Motive bei Bedarf separat zur Freigabe vorlegen. Die genaue Position der Bedienpunkte und der sichere Prüfplatz werden an der Laufgeometrie geprüft. Vorgesehen sind das veröffentlichte Format 1.8.0 und die Inhaltsversion 3.1.0, sofern alle bisherigen Übergänge, Laufbereiche und Spawns ihre Bedeutung behalten. Die oben beschriebenen öffentlichen Testgrenzen stehen zur Paketfreigabe.

20.11.2026: Umfang und Testgrenzen mit „ja“ bestätigt. Die Werkstatt ergänzt den Garten mit einem Eingang rechts vom Tisch und einem Rückweg. Eine Holztür trennt beide Raumhälften. Der Schalter am Gitter öffnet und schließt das Gartentor. Das Schließen direkt am Gitter stellt den Schalter zurück. Der Raumeingang und ein benannter Spawn in der rechten Hälfte dienen als lokale Sicherheitsanker. Auf der Schwelle bei `(500,124)` lehnt die Navigation das Schließen ab. Bei `(500,160)` kann eine Figur sicher warten. In der Editor-Vorschau lässt sich die gleiche Fußpunktregel am Stuhl prüfen, wenn die Vorschaufigur vor dem Ziehen auf `(222,230)` steht.

Die erste Abnahmefassung verwendete Format **1.8.0** aus Ticket 21 und Inhalt **3.1.0**. Sie ergänzte die Werkstatt mit neuen Übergängen und einem zusätzlichen Gartenspawn. Die Schwelle der Holztür saß am Rand des Pflasters. Acht Werkstattgrafiken waren bytegleiche Kopien aus Ticket 22. [Kompatibilitätsvertrag](../../../docs/work-packages/world-format-compatibility.md) · [Grafikherkunft](../../../worlds/reference/ART-PROVENANCE.md).

20.11.2026: Die erste Platzierung ragte optisch aus der niedrigen Mauer. Die anschließende Fassung mit 77 Pixeln war für die 128 Pixel hohen Figuren zu klein. Die Holztür erhielt deshalb eine eigene hohe Steineinfassung im Stil des Gartens. Das Türblatt ist rund 140 Raumpixel hoch, der Bogen insgesamt 174. Die Klickfläche entspricht dem Bildrechteck und der Laufpunkt liegt vor dem Eingang auf dem Pflaster.

## Prüfbelege

- Der neue Inhaltstest schlug zunächst ohne erreichbare Werkstatt fehl. Die ergänzte Größenprüfung erkannte später die 77 Pixel kleine Tür. Beide Prüfungen bestehen mit der neuen Szene.
- Drei neue Browserfälle prüfen Gegenaktionen, gesperrte und freie Wege, eine beim Laufen geschlossene Tür, den tatsächlichen Klick zum Sicherheitsprüfplatz, spätere Teilnahme, Rückkehr, Wiederverbindung, Neustart sowie Speichern, Neuladen und sichtbare Sicherheitsmeldung im Editor. Zwei-Spieler- und Editorbilder liegen in `evidence/23/browser/`.
- TypeScript-Typcheck und Clientbuild bestanden. Die abschließende Vollsuite bestand mit **334 Tests** (222 Server, 112 Browser), ohne Compilerwarnungen. Protokoll und Detailergebnisse liegen in `evidence/23/full-entrance.log` und `evidence/23/full-entrance/`.
- Zwei unabhängige Prüfungen gegen `c8be2e8` bewerteten Projektstandards und Spezifikation. Die sichere Schwelle und die Editor-Ablehnung wurden anschließend über echte Mausklicks geprüft. Nachprüfung der Türgröße, Geometrie und Grafikherkunft ohne offene Befunde. Der Einsatz ähnlicher Testaufbauten für die Referenztour und Werkstatt bleibt ein optionaler Wartbarkeitshinweis.
- Die Vergleichsprüfung umfasste 202 unveränderte versionierte Schema-, Fixture-, Welt- und Design-Dateien. Acht Testweltgrafiken wurden per SHA-256 als bytegleiche Spielkopien bestätigt. Die Werkstattszene hat keine neuen Projektabhängigkeiten. Der lokale Implementierungscommit heißt `Add interactive workshop to reference world (ticket 23)`.

## Manuelle Abnahme

1. Das [Spiel der Abnahmekopie](http://127.0.0.1:50081/) in zwei Fenstern mit Mara und Bruno betreten. Von der Lounge durch die rechte Tür in den Garten. Rechts vom Tisch führt der Steinbogen mit Holztür in die Werkstatt. Direkt daneben lässt sich der Maßstab der Tür mit der eigenen Figur vergleichen. In der Werkstatt mit **Ziehen** und **Drücken** abwechselnd den Stuhl bewegen. Mit **Öffnen** die linke Tür in der Rückwand öffnen. Mit **Gehe zu** darauf klicken, um in den Garten zurückzukehren. Die andere Figur kann die Tür während der Annäherung schließen. Die laufende Figur hält dann in der Werkstatt an.
2. **Benutzen** am Torschalter öffnet das Gitter. Hinter dem Gitter ist der Pflasterweg aus der Werkstatt in den Garten zu sehen. Eine Figur klickt mit **Gehe zu** auf den Boden am rechten Rand der offenen Schwelle bei `(389,115)`. **Benutzen** am Schalter durch die andere Figur zeigt nun die Sicherheitsmeldung. Nach einem Schritt zurück Richtung `(389,159)` lässt sich das Tor wieder schließen. Zum direkten Schließen mit dem passenden Verb den oberen Gitterrahmen anklicken. Nach erneutem Öffnen führt das Gartentor als zweiter Rückweg in den Garten. Mit `D` lässt sich die Laufgeometrie prüfen.
3. Im [Editor der Abnahmekopie](http://127.0.0.1:50081/editor.html?worldId=reference) **Werkstatt** wählen. Stuhlpositionen und die Verknüpfung am Torschalter ansehen, speichern und neu laden. In der Vorschau mit **Gehe zu** auf `(222,230)` rechts unterhalb des Stuhls klicken. Nach der Ankunft **Ziehen** wählen und auf den Stuhl klicken. Die Vorschau meldet die drohende Überdeckung. Nach dem Wegtreten sind Ziehen und die weiteren Aktionen möglich. Das Spiel in einem anderen Fenster behält seinen eigenen Zustand.

Die Abnahmeinstanz verwendet eine eigene Weltkopie unter `.scratch/adventure-chat/evidence/23/acceptance-20261120-094637/`. Eintritt durch die Gartentür, beide Rückwege in den Garten und der Aufruf des Editors sind geprüft. `plausibility-garden.png` zeigt die Gartentür neben Mara. `plausibility-workshop-open.png` und `plausibility-workshop-closed.png` zeigen beide Werkstattausgänge. Bilder, lokales Testprotokoll und Startdaten liegen daneben in `.scratch/adventure-chat/evidence/23/`. Grafik und Bediengefühl bleiben menschliche Abnahme.

## Nacharbeit zur Laufgeometrie · 20.11.2026

Sichtfeedback zeigte, dass eine Figur hinter die Gartentür laufen konnte und die Innentür mitten im Raum aufgesetzt wirkte. Die Gartenfläche endet jetzt vor der Tür und ihrem seitlichen Maueranschluss. Der Auslösepunkt liegt davor auf dem Pflaster. In der Werkstatt sitzt die Tür links in der durchgehenden Rückwand und öffnet den bereits vorhandenen Ausgang zum Garten. Die Anordnung in einer Querwand wurde nach Sichtfeedback verworfen. Das Gartentor sitzt bündig in einer zweiten Rückwandöffnung. Hinter dem angehobenen Gitter ist der Weg in den Garten zu sehen.

Die Referenzwelt verwendet deshalb Inhaltsversion **4.0.0**. Einige vorher gültige Fußpunkte im Garten sind durch die angepasste Laufkante gesperrt. Das Format bleibt **1.8.0**. Die Anschlüsse der bisherigen Tour und ihre Spawns liegen weiterhin auf begehbaren Flächen.

Die Sichtprüfung im Browser verglich Figurenhöhe und Gartentür, die Stellung von Innentür und Gartentor in der Rückwand sowie das Pflaster hinter dem Gitter. Beide Rückwege wurden geöffnet, zu Fuß angeklickt und tatsächlich bis in den Garten benutzt. Im geschlossenen Zustand fehlen beide Ausgänge und der Boden vor den Schwellen ist gesperrt. Die Perspektive von Tisch und Stuhl wurde in beiden Stuhlpositionen gesichtet.

Der reproduzierende Inhaltstest zeigte drei zuvor fälschlich begehbare Punkte. Im endgültigen Stand prüfen fünf Inhaltsfälle die Schwellen und drei Browserfälle die Bewegungsstopps, beide Rückwege, Mehrspielerzustände sowie Speichern und Vorschau. Typcheck, Build und Vollsuite bestanden mit **339 Tests** (227 Server, 112 Browser). Die Schemata, älteren Referenzfixtures und ursprünglichen Weltgrafiken der Testwelten stimmen mit `484bfad` überein. Protokolle liegen in `evidence/23/full-plausibility/`, Browserbelege in `evidence/23/browser-plausibility/`. Der eigene Vergleich gegen Ticket und Projektstandards ergab keine weiteren offenen technischen Befunde. Die menschliche Abnahme steht aus.
