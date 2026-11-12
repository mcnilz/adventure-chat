# Inhaltsfixtures

`reference-v1` wurde mit `git show b3d0b83:worlds/reference/...` bytegetreu gesichert; PNG-Dateien aus den zugehörigen lokalen LFS-Objekten. Diese Fixture bleibt unverändert und ersetzt in Regressionstests die derzeit für Ticket 16 überarbeitete Referenzwelt. Deren neue Bilder passen noch nicht zu den alten Raumabmessungen.

`characters-a` und `characters-b` sind neue synthetische Testwelten für 1.3.0. Ihre selbst erzeugten PNGs enthalten farbige Rasterzellen zur numerischen Prüfung von Richtung und Frame, keine fremden Grafiken. Die gleichen IDs und Bildpfade liefern je Welt unterschiedliche Pixel. Keine Beurteilung der Animation echter Figuren daraus ableiten.

## foreground (Format 1.4.0)
Synthetische Testwelt für Ticket 15, auf characters-a aufgebaut. table.png ist ein selbst erzeugtes 12×12-RGBA-Testmuster (rot, zwei transparente Spalten), keine Fremdressource. Es wird absichtlich auf 120×120 Raumpixel skaliert; depthY=280. Dient Pixelprüfungen, Navigation ohne automatische Kollision und Editor-Roundtrip. Ältere Fixtures werden dafür nicht geändert.

## Eingefrorene Karten-/Scrollwelten vor Ticket 13
map-v1 (Format 1.1.0) und scroll-v1 (1.2.0) entsprechen bytegetreu dem Stand 7733562 unter worlds/map bzw. worlds/scroll. Alle zwölf Dateien wurden unabhängig gegen Git-Blobs bzw. Git-LFS-SHA256 verglichen. Bestehende Karten-/Scroll-Browserregressionen verwenden diese unveränderten Fixtures; neue Tiefentests prüfen die weiterentwickelten mitgelieferten Welten separat.

## interactions (Format 1.6.0)
Kleine synthetische Testwelt für Ticket 20: Stuhl und Schalter im Testraum, verknüpftes Tor im Nebenraum. Die farbigen PNG-Testmuster sind Kopien aus `foreground`; sie illustrieren Zustandspositionen und sind keine endgültige Gestaltung. Die Objekte beeinflussen noch keine Navigation. Die eingefrorenen älteren Fixtures bleiben unverändert.
