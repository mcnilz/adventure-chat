# Inhaltsfixtures

`reference-v1` wurde mit `git show b3d0b83:worlds/reference/...` bytegetreu gesichert; PNG-Dateien aus den zugehörigen lokalen LFS-Objekten. Diese Fixture bleibt unverändert und ersetzt in Regressionstests die derzeit für Ticket 16 überarbeitete Referenzwelt. Deren neue Bilder passen noch nicht zu den alten Raumabmessungen.

`characters-a` und `characters-b` sind neue synthetische Testwelten für 1.3.0. Ihre selbst erzeugten PNGs enthalten farbige Rasterzellen zur numerischen Prüfung von Richtung und Frame, keine fremden Grafiken. Die gleichen IDs und Bildpfade liefern je Welt unterschiedliche Pixel. Keine Beurteilung der Animation echter Figuren daraus ableiten.
