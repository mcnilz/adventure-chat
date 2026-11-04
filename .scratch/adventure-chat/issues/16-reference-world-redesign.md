# 16: Referenzwelt nach neuem Design Guide integrieren

**What to build:** Lounge und Tavernen-Garten bilden eine spielbare, visuell stimmige Referenzwelt nach dem neuen Design Guide. Mara und Bruno sind auswählbar; Tische verdecken dahinter stehende Figuren korrekt. Raumgeometrie und Grafik stimmen überein.

**Blocked by:** 14, 15; Ticket 12 ist done. Ticket 13 folgt erst nach diesem Ticket und ist kein Blocker.

**Status:** draft

Am 04.11.2026 nur als Ticketanlage beauftragt. Umsetzung erfolgt durch andere Agenten nach Freigabe von Umfang und Testgrenzen. Bestehende Benutzeränderungen erhalten, keine vorweggenommene Abnahme.

## Vorhandene offene Änderungen (04.11.2026)

- `worlds/reference/assets/images/lounge.png` und `garden.png` sind bereits durch die neuen 1280 × 720-Hintergründe ersetzt, jeweils bytegleich zu `worlds/reference/design-guide/assets/rooms/`.
- `worlds/reference/world.yaml` hat bislang nur die Inhaltsversion von 1.0.0 auf 1.0.1 erhöht; Format ist noch 1.0.0.
- Neu und bislang unversioniert: `worlds/reference/design-guide/index.html`, die beiden Raumvorlagen und Mara/Bruno einschließlich Legacy-Sheets in `worlds/reference/design-guide/assets/`.
- Haupt-Sheets: 256 × 288, vier Spalten à 64 × 96; rechts/hinten/vorne, links gespiegelt. Legacy-Sheets: 192 × 192 mit 48 × 64-Frames. Kontinuität und Zuschnitt sind visuell noch nicht abgenommen.
- Es gibt noch keine separaten Tischmasken. Laufpolygone, Hindernisse, Spawns und Ausgänge sind unverändert. Design-Guide-Koordinaten beschreiben den bisherigen Stand und sind kein Beleg für passende neue Geometrie.

## Acceptance criteria

- [ ] Design Guide und vorhandene Assets sichten und als Grundlage erhalten. Referenzwelt nutzt die neuen Lounge-/Gartenbilder und bietet Mara/Bruno über den Weltkatalog aus Ticket 14 an. Haupt-Sheets mit 64 × 96-Frames bevorzugen; abweichende Entscheidung begründen. Keine globale Ersetzung der Figur für andere Welten.
- [ ] Beide Figuren bei Stand, allen Laufphasen und Blickrichtungen im Spiel prüfen. Angeschnittene Frames, unstete Fußpunkte und störende Animationssprünge vor Abnahme korrigieren oder als konkrete Abnahmegrenze offen melden; keine ungeprüfte Behauptung fertiger Animationsqualität.
- [ ] Passgenaue transparente Tischgrafiken für beide Räume erstellen und über Ticket 15 einbinden. Silhouette, Deckkraft, Farben und Position stimmen mit dem Hintergrund überein; Lücken bleiben transparent. Keine doppelten Konturen, sichtbaren Nähte oder Abdeckung außerhalb des Tisches. Hintergrundänderungen nur soweit zur sauberen Integration erforderlich.
- [ ] Figur kann hinter und vor beiden Tischen stehen und die erwartete Teilverdeckung zeigen. Laufpolygone und Tischhindernisse an die sichtbaren Objekte anpassen, soweit nötig. Hintere Laufbereiche müssen erreichbar sein; bestehende Navigationsabstände berücksichtigen.
- [ ] Türbereiche, Beschriftungen/Interaktion, Spawnpunkte und Übergänge passen sichtbar zu den neuen Bildern. Lounge und Garten sind in beide Richtungen erreichbar; kein Spawn im Hindernis und kein unzugänglicher Ausgang.
- [ ] Logischer Raum bleibt grundsätzlich 640 × 360 bei proportional gezeichnetem 1280 × 720-Hintergrund. Zielhöhe der Figuren bewusst visuell wählen und über vorhandene Raumangaben ausdrücken. Neue Bilder allein vergrößern keine Weltkoordinaten.
- [ ] Raumeditor lädt, zeigt, speichert und öffnet beide Räume einschließlich Objekten ohne Informationsverlust. Vorschau und Spiel zeigen dieselbe Figurenposition, Größenwirkung und Verdeckung.
- [ ] Vor Umstellung der Referenzwelt ihre bisherige veröffentlichte Fassung aus dem Git-Basisstand (nicht aus den bereits ersetzten Arbeitsdateien) als eigenständige unveränderte Kompatibilitätsfixture sichern. Bestehende Tests auf die eingefrorene Fixture ausrichten, Zusicherungen und Hashprüfungen erhalten, keine alten Erwartungswerte nur zur Anpassung an das Redesign überschreiben. Alte Schemas/Teilschemas unverändert lassen.
- [ ] Manifest verwendet die tatsächlich erforderliche unterstützte Formatversion aus 14/15. Endgültige Inhaltsversion anhand der inhaltlichen Änderungen bewusst begründen; die bereits eingetragene 1.0.1 nicht ungeprüft als finale Version übernehmen. Ressourcen bleiben vollständig lokal im Weltpaket.
- [ ] Herkunft und Nutzungsrechte der gelieferten und neu bearbeiteten Bilder ehrlich dokumentieren; keine Lizenz erfinden. Design Guide mit tatsächlichen Dateipfaden, Rasterangaben und erreichtem Integrationsstand aktualisieren; Aussagen über ungetestete Vorlagen durch tatsächliche Belege ersetzen, soweit geprüft.
- [ ] README/Demoanleitung beschreibt Weltwahl, Figurenwahl, Weg zwischen den Räumen und Stellen zum Testen der Tischverdeckung. PLAN, Ticket und active-state enthalten tatsächliche Checks und ready-for-human; menschliche Sichtabnahme separat.

## Abgrenzung zu Ticket 13

Dieses Ticket integriert die zwei Räume der neuen Art Direction ohne Tiefenskalierung. Auf ausdrücklichen Benutzerwunsch vom 04.11.2026 folgt Ticket 13 erst nach 14, 15 und 16. Dessen drei Demo-Szenarien je mitgelieferter Welt werden erst dort ergänzt und blockieren die Abnahme dieses Tickets nicht. Ticket 13 setzt auf der hier fertiggestellten Referenzwelt auf; seine Kriterien werden dadurch nicht gestrichen. Tiefenskalierung ist für Tischverdeckung nicht erforderlich.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Weltlade-/Ressourcen- und Editor-Speichergrenzen sowie Browserablauf mit zwei Teilnehmern. Beide Figuren, beide Räume, Türwechsel, Chat und Reconnect; erreichbare Punkte hinter/vor beiden Tischen; passende Kollision; Editor speichern/neuladen/Vorschau. Alte veröffentlichte Welten über die eingefrorenen Fixtures weiter prüfen. Typcheck, Build und einschlägige vollständige Suite nach Integration; Bildprüfungen ergänzen, keine privaten Assetpfade als alleinigen Funktionsbeleg verwenden.

Menschliche Abnahme: beide Figuren auswählen und in allen Richtungen laufen, hinter/vor beiden Tischen stehen, Türen hin und zurück benutzen, Namen/Sprechblasen mit zweitem Browser vergleichen und dieselben Stellen im Editor prüfen. Belege mit konkreten Positionen/Screenshots ergänzen. Numerische Tests ersetzen die gestalterische Abnahme nicht.
