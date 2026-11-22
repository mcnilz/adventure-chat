# 33 – Sprechende Figuren, NPC-Gesten und eine eigene NPC-Grafik

**What to build:** Solange die Sprechblase einer Spielerfigur oder eines NPCs sichtbar ist, wechseln bei sichtbarem Gesicht die Mundphasen, sodass die Figur erkennbar spricht. NPCs können eine Sprechblase mit einer optionalen Geste begleiten, etwa einer kurzen Handbewegung. Die Referenzwelt erhält eine neu gezeichnete NPC-Figur im Stil des [Design Guides](../../../worlds/reference/design-guide/index.html#figuren).

**Blocked by:** [26 – Figurenanimationen](26-character-animations-for-object-actions.md), [27 – NPC-Dialog](27-npc-dialogue.md), [30 – NPC-Führung](30-follow-npc.md) (alle `done`).

**Status:** done

## Acceptance criteria

- [x] Beim Raumchat und bei Dialogblasen von Spielerfiguren und NPCs wechselt der Mund während der sichtbaren Blase wiederholt zwischen erkennbaren Phasen. In der Rückenansicht bleibt das Gesicht verdeckt. Ablauf, Überspringen, Ersetzen oder Abbruch einer Blase beenden die zugehörige Animation. Dieselbe Regel gilt für NPC-Erläuterungen während einer Führung.
- [x] Die Animation ist in denselben Browsern sichtbar wie die zugehörige Blase. Raumchat bleibt raumgebunden. Bei privaten Gesprächen sieht ein zweiter Besucher die Mundbewegung und die Geste nur während seiner eigenen sichtbaren NPC-Antwort. Lokale Systemhinweise lösen keine Sprechbewegung aus.
- [x] Eine NPC-Antwort in der Referenzwelt zeigt eine kurze Handbewegung, während der NPC spricht. Eine weitere Antwort zeigt die Mundbewegung ohne Geste. Bewegung, Blickrichtung und Fußpunkt der Figur bleiben stimmig. Beim Ende der Blase kehrt die Figur zur passenden Stand-, Lauf- oder Aktionspose zurück.
- [x] Eine neu gestaltete NPC-Figur ist schon an Silhouette, Frisur und Kleidung von den auswählbaren Spielerfiguren Mara und Bruno zu unterscheiden. Die Figur steht und läuft in den benötigten Blickrichtungen und hat passende Sprech- und Gestenphasen. Proportionen, Pixelgruppen, Lichtführung, 128er-Grundraster und gemeinsamer Fußanker orientieren sich am [Design Guide](../../../worlds/reference/design-guide/index.html#figuren). Die Grafik wird in der Referenzwelt einem bestehenden NPC zugeordnet und ist dort NPCs vorbehalten.
- [x] Ältere Welten lassen sich mit ihren bisherigen Grafiken und Sprechblasen weiter nutzen. Fehlen passende Animationsgrafiken, bleibt die Blase wie gewohnt sichtbar. Erweitert das Paket den Autorenvertrag, sind SemVer, explizite Versionsunterstützung und Kompatibilität durch unveränderte ältere Schemata und Referenzfixtures nachzuweisen.

## Bestätigter Umfang und Testgrenzen

Am 22.11.2026 bestätigte der Benutzer Nela als einzige neu gestaltete Figur, die Gestenregeln und die öffentlichen Testgrenzen. Nelas erstes „Wer bist du?“ zeigt ein kurzes Winken, „Wo ist der Garten?“ zeigt reine Mundphasen. In der Referenzwelt 5.8.0 bleibt Nela NPCs vorbehalten.

- Pro Antwortoption ist eine Geste optional. Sie gehört zum ersten Abschnitt der Antwort. Mundphasen folgen der Laufrichtung und der jeweiligen Laufphase. Objektposen behalten ihre Grafik. Bei fehlenden Animationsblättern bleibt der Text sichtbar. Lokale Systemhinweise verwenden die normale Pose.
- Das additive Autorenformat 1.16.0 legt die Angaben in YAML fest. Die isolierte Editorvorschau spielt dieselben Dialoge ab. [Formatvertrag](../../../CONTENT-FORMAT.md#sprechphasen-und-npc-gesten-ab-1160)
- Browser prüfen die sichtbaren Phasen im Raumchat, auf getrennten Dialogansichten sowie in der privaten Führung. Weitere Prüfungen betreffen Lauf- und Rückansichten, Blasenwechsel, Ablauf und Raumwechsel, die isolierte Vorschau sowie die Inhaltsvalidierung und Beibehaltung älterer Welten.

## Manuelle Abnahme

1. Die [lokale Abnahmekopie](http://127.0.0.1:54655/) in zwei Browsern öffnen und die Referenzwelt betreten. In der Lounge eine Chatnachricht senden und beobachten, wie die eigene Figur in beiden Browsern bis zum Ende der Blase spricht.
2. Den NPC in der Lounge ansprechen. „Wer bist du?“ mit Handgeste und „Wo ist der Garten?“ ohne Handgeste prüfen. Eine Blase überspringen oder das Gespräch abbrechen und beobachten, wie Mund und Hand zur passenden Pose zurückkehren. Im zweiten Browser währenddessen eine eigene Frage stellen und die getrennten Antworten vergleichen.
3. Die neue Figur neben Mara und Bruno im Stand und beim Laufen ansehen. Auf Silhouette, Gesicht, Kleidungsflächen und Fußpunkt achten.

4. „Führung starten“ wählen. Bei der ersten Erklärung an der Klingel spricht Nela nur in der Ansicht des Gastes. Dieselbe Gesprächsgeste lässt sich im [Raumeditor](http://127.0.0.1:54655/editor.html?worldId=reference) in der Lounge unter **Vorschau → Spreche mit** prüfen.

## Umsetzung und Prüfbelege

- Typcheck und Clientbuild bestanden. Die gezielte Nachprüfung umfasste 13 Browser- und Editorfälle. Die zwei angepassten Grafiktests prüfen separat das Draufsichtraster und Nelas sichtbaren Fußpunkt im selben 128er-Raster wie die Spielerfiguren.
- Die Abnahmekopie wurde im Browser geprüft. Die Figurenauswahl zeigte Mara und Bruno, Nela war in der Lounge ansprechbar. Die isolierte Editorvorschau zeigte dieselbe Geste. [Geste in der Lounge](../evidence/33/lounge-nela-wave.png) · [Vorschau](../evidence/33/editor-nela-wave.png)
- Für die beiden Draufsichtblätter wurden 32 Kombinationen aus Figur, Richtung und Laufphase im Canvas geprüft. Die Rückenreihen der transparenten Mundblätter sind leer. Eine Objektpose verwendet ihre vorhandene Grafik. Lässt sich eine Führungsblase in einem sehr kleinen Canvas nicht darstellen, steht die Erklärung weiter in der Führungsanzeige.
- Standards- und Spezifikationsreview wurden getrennt ausgeführt. Die Nachprüfung bestätigte die Korrekturen zur Lebensdauer der Blase, den sichtbaren privaten Fallback, die Typen an der Schnittstelle, die robustere Konvertierung der Testwelten und die zusätzlichen Canvas-Fälle zu Fuß und beim Raumwechsel.
- Das additive Schema-Bündel 1.16.0 und die Referenzwelt 5.8.0 wurden geprüft. Der Dateivergleich ergab keine Änderung an veröffentlichten älteren Schemas oder Referenzfixtures. Die Grundgrafiken von Mara und Bruno behalten ihre Bilddaten. Keine neuen Projektabhängigkeiten.
- Abschließende Vollsuite in Release vollständig bestanden. 410 Tests, davon 268 Server- und 142 Browsertests. Der Browserlauf dauerte 15 Minuten und 26 Sekunden. [Testprotokoll](../evidence/33/final.log)

## Menschliche Abnahme

Am 22.11.2026 ausdrücklich mit „abgenommen“ bestätigt. Umsetzungscommit `490711b`.
