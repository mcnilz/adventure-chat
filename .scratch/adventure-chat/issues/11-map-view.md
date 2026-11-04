# 11: Begehbare Kartenansicht mit fester Figurengröße

**What to build:** Ein Autor erstellt einen Raum als begehbare Karte mit Draufsicht-Figuren. Teilnehmer können darin gemeinsam laufen und chatten sowie über Kartenorte normale Räume betreten. Perspektive und Figurengröße werden vom Autor pro Raum festgelegt; es gibt keinen zur Laufzeit veränderbaren Zoom.

**Blocked by:** 09 (versioniertes Autorenformat); für die Gesamtabnahme im Mehrweltbetrieb zusätzlich 10

**Status:** done

## Acceptance criteria

- [x] Räume behalten das gemeinsame Grundmodell aus Hintergrund, begehbaren Flächen, Startpunkten und Ausgängen. Eine optionale Darstellungsangabe unterscheidet bisherige Adventure-Ansicht und Draufsicht. Ein eigener Raumtyp wird nur bei nachgewiesenen abweichenden fachlichen Regeln eingeführt und begründet.
- [x] Der Autor legt die Figurengröße pro Raum als festen Maßstab fest. Die genaue Einheit, der Bezugspunkt und zulässige Werte werden vor Implementierung festgelegt. Keine Zoomsteuerung, keine dynamische Kamera und keine Größenänderung durch Teilnehmer.
- [x] Mindestens eine Referenzfigur besitzt Draufsichtgrafiken mit erkennbaren Bewegungsrichtungen und Stand-/Laufzuständen. Fehlende Grafiken werden im Autorenmodus verständlich gemeldet; kein unbemerkter Wechsel zur falschen Perspektive. Die Behandlung weiterer Figuren ohne Draufsichtgrafiken wird vor Implementierung festgelegt.
- [x] Spiel und Editorvorschau zeigen übereinstimmende Perspektive, Größe und Figurenposition. Skalierung verändert keine Weltkoordinaten und umgeht keine serverseitige Bewegungsprüfung. Namen, Sprechblasen und anklickbare Ausgänge bleiben benutzbar.
- [x] Die Laufgeschwindigkeit kann als separate Autoreneinstellung pro Raum festgelegt werden; diese Ergänzung ist ein Vorschlag, vor Implementierung bestätigen. Ohne neue Angabe gilt die bisherige Geschwindigkeit. Der Server bestimmt die Bewegung; alle Clients interpolieren denselben Verlauf.
- [x] Kartenorte nutzen bestehende Ausgänge mit Zielraum und Ziel-Spawn. Ein Kartenraum führt in mindestens zwei normale Räume, von denen eine Rückkehr auf die Karte möglich ist. Perspektive, Maßstab und gegebenenfalls Geschwindigkeit wechseln korrekt mit dem Raum.
- [x] Eine Referenzkarte enthält ein Labyrinth mit Verzweigungen, Sackgasse, Hindernissen und schmalen, für die konfigurierte Geometrie zulässigen Durchgängen. Zunächst vorhandene Wegfindung prüfen; Änderungen nur bei belegter Lücke, kein vorsorglicher Algorithmuswechsel.
- [x] Wege schneiden keine Wände oder gesperrten Flächen. Unerreichbare Ziele werden nach den bestehenden Regeln zurückgewiesen. Bewegungsziel während des Laufens ändern funktioniert auch im Labyrinth.
- [x] Die optische Figurengröße wird nicht still zum Kollisionsradius. Vor Umsetzung den vorhandenen Fußpunkt-/Abstandsvertrag prüfen und dokumentieren, wie Durchgangsbreite, Hindernisabstand und sichtbare Figur zusammenpassen. Keine neue Figurenkollision erforderlich.
- [x] Zwei Browser sehen konsistente Bewegung, Anwesenheit, Raumwechsel und Chat auf der Karte. Vorhandene Welt- und Raumisolation bleibt erhalten.
- [x] Bestehende Raumdateien behalten ohne neue Felder ihre bisherige Darstellung, Größe und Bewegung. Optionale Felder mit kompatiblen Defaults erhalten eine begründete Minor-Einstufung; veröffentlichte Schemas bleiben unverändert. Neue Formatversion und Unterstützungsmatrix ergänzen, alte Referenzwelten unverändert testen.
- [x] Raumeditor bietet Ansicht und feste Größe als Eigenschaften sowie Vorschau. Speichern verwendet eine Formatversion, die diese Angaben tatsächlich unterstützt.
- [x] Dokumentation und Glossar unterscheiden Draufsicht, Figurengröße, Bildschirmskalierung und Laufgeschwindigkeit. Tatsächliche Checks, Review und manuelle Abnahmeschritte sind dokumentiert; done erst nach menschlicher Abnahme.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Inhaltsvalidierung, serverseitige Bewegungs-/Raumwechselgrenze, vorhandene Renderer-/Browsergrenzen. Nicht die internen Schritte eines Wegfindungsalgorithmus nachbauen.

Gezielte Fälle: bestehender Raum ohne neue Angaben; Draufsicht mit zwei festen Größen; ungültiger Maßstab; fehlende Draufsichtgrafik; Editor speichern und neu laden; Kartenraum zu normalem Raum und zurück; Labyrinthroute um Hindernisse; schmaler zulässiger Durchgang; unerreichbares Ziel; Richtungswechsel während Bewegung; zwei Browser mit übereinstimmender Darstellung und Bewegung. Alte Raumfixtures bleiben unverändert. Visuelle Prüfung ergänzt Verhaltensprüfungen bei Fußpunkt, Sprite-Skalierung, Namen und Sprechblasen.

## Vorgehen

1. Bestehendes Raum-, Sprite- und Bewegungsmodell lesen. Konkrete Darstellungsfelder, Maßstab, Grafikzuordnung und Testgrenzen vorschlagen. Die noch vorgeschlagene Geschwindigkeitsoption bestätigen lassen.
2. Draufsichtfigur und Referenzkarte durchgehend darstellen, vorhandene Raumübergänge nutzen.
3. Labyrinthfälle an bestehender Wegfindung prüfen und nur notwendige Korrekturen umsetzen.
4. Editor, Formatversion, Kompatibilitätsfixtures, Dokumentation und Review abschließen.

## Nicht im Umfang

Inventar, Rätsel, Skripting, dynamischer Zoom, Kamerascrolling, automatisch erzeugte Labyrinthe und eine neue Weltzustandspersistenz.

## Manuelle Abnahme

Karte betreten, Draufsicht und feste Größe prüfen, durch das Labyrinth laufen, währenddessen ein neues Ziel anklicken, zwei Kartenorte betreten und zurückkehren. Mit zweitem Browser Bewegung und Chat prüfen. Danach eine unveränderte ältere Welt öffnen und deren bisheriges Verhalten bestätigen.

## Bestätigung 04.11.2026
Benutzer bestätigt Umfang und öffentliche Testgrenzen mit „ja“. Perspective adventure/top-down; actorHeight 16–128 logische Pixel (Default 64), Fußpunkt als Anker; walkSpeed separat (Default 130). Fehlende Draufsichtgrafiken sind Autorenfehler. Reviewbasis: 16b06d5854b405f583da3a15788f0e23494a45a1.


## Umsetzung und Nachweise · 04.11.2026

- Freigegebene Felder durch Lade-/Speichergrenze, Runtime-DTOs und gemeinsamen Renderer geführt; neue unveränderliche Schemas im Bündel 1.1.0, alte Schemas unverändert. Referenzwelt 1.0.0 bleibt unverändert. Keine neue direkte Abhängigkeit.
- Native Canvas-Pixelgrafik der einzigen Referenzfigur: vier Richtungen, Stand und Schrittanimation. Es gibt keine zusätzliche Grafikdatei oder weitere auswählbare Figuren; ein fehlender Draufsicht-Dateipfad ist damit nicht konfigurierbar. Unbekannte Perspektiven werden an der Inhaltsgrenze gemeldet. Diese Konkretisierung vermeidet einen unnötigen Figuren-/Grafikkatalog.
- Kartenwelt unter `worlds/map`: zwei Orte mit Rückwegen, Verzweigungen, begehbare U-Sackgasse, 15-Pixel-Durchgang unten und geschlossener unerreichbarer Innenbereich. PNG und Geometrie stimmen überein. Bestehende Wegfindung unverändert.
- Editor kann Perspektive, feste Höhe und Geschwindigkeit speichern/neu laden. Seine Vorschau nutzt serverseitige Wege für ungespeicherte Daten und friert während der Anfrage kurz ein; sie ist keine Multiplayersitzung.
- TDD-Nachweise: Format 1.1.0 zuerst als nicht unterstützt rot, danach grün; alte Editorvalidierung ließ neue Felder zunächst zu (rot), danach verständliche Verweigerung (grün); Sackgassenwand war zunächst begehbar (rot), nach Ergänzung blockiert (grün).
- Typcheck und Clientbuild bestanden. Öffentliche Paketvalidierung: 29 Tests grün zum Zwischenstand. Abschließende gezielte Kartenprüfung: 4 Tests grün (Serverbewegung/Umkehr/Raumwechsel, zwei Browser mit Bewegung und Chat, Editor speichern/laden/Vorschauweg, Canvas-Darstellung in vier Richtungen bei 32/48 Pixeln mit Stand-/Schrittunterscheidung und Fußpunkt).
- Screenshotprüfung von Spiel und Editor bei 32 und 48 Pixeln: Größe, Position, Namen und Sprechblase geprüft. Subjektives Bewegungsgefühl und Bedienqualität bleiben menschliche Abnahme.
- Code-Review als zwei unabhängige Subagenten: Standards ohne Befund; Spec fand fehlende erreichbare Sackgasse, korrigiert und durch denselben Reviewer nachgeprüft, keine verbleibenden Befunde. Reviewbasis `16b06d5854b405f583da3a15788f0e23494a45a1`; Arbeitsdiff einschließlich neuer Dateien geprüft.
- Umgebungsgrenze: Chromiumstart im eingeschränkten Prozess schlug mit EPERM fehl. Mit Ausführungsrechten bestanden die Browserprüfungen. Ein Editor-Test nahm nach Neuladen fälschlich erneute Weltwahl an; an bestehende automatische Wiederwahl angepasst.

## Konkrete Abnahmeschritte

1. Startbefehl aus README („Kartenwelt“) verwenden, Welt betreten. Linker brauner Ort: Taverne; rechter: Garten; in beiden „Karte“ zurück.
2. Durchs Labyrinth laufen, die U-Sackgasse links der Mitte betreten und verlassen. Im Lauf ein neues Ziel klicken; den geschlossenen Bereich rechts oben anklicken (kein Eintritt).
3. Zweiten Browser öffnen: Anwesenheit, Bewegung und Chat vergleichen; Karte ↔ beide Adventure-Räume prüfen.
4. Im Editor die Karte öffnen, feste Höhe 32 auf 48 ändern, speichern und neu laden; Vorschau, Fußpunkt, Namen und Sprechblasen prüfen.
5. Unveränderte ältere Referenzwelt starten: bisherige Darstellung und Bewegung bestätigen. Erst ausdrückliche Benutzerabnahme setzt done.

Abschluss: vollständige Suite nach Reviewkorrektur 183 Tests bestanden, keine Warnungen; Typcheck/Build und Diffprüfung sauber. Menschlich am 04.11.2026 abgenommen („funktioniert super“). Implementierungscommit: `a3497dc`.

## Menschliche Abnahme · 04.11.2026
Benutzer: „funktioniert super“. Ticket 11 ist damit done. Keine zusätzliche manuelle Einzelprüfung wird daraus behauptet.
