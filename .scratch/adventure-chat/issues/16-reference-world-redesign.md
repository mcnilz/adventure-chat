# 16: Referenzwelt nach neuem Design Guide integrieren

**What to build:** Lounge und Tavernen-Garten bilden eine spielbare, visuell stimmige Referenzwelt nach dem neuen Design Guide. Mara und Bruno sind auswählbar; Tische verdecken dahinter stehende Figuren korrekt. Raumgeometrie und Grafik stimmen überein.

**Blocked by:** 14, 15; Ticket 12 ist done. Ticket 13 folgt erst nach diesem Ticket und ist kein Blocker.

**Status:** ready-for-human

Am 04.11.2026 nur als Ticketanlage beauftragt. Am 05.11.2026 hat der Benutzer Paketumfang, Detailentscheidungen und Testgrenzen mit „ja“ bestätigt. Implementiert; menschliche Sichtabnahme bleibt offen.

## Vorhandene offene Änderungen (04.11.2026)

- `worlds/reference/assets/images/lounge.png` und `garden.png` sind bereits durch die neuen 1280 × 720-Hintergründe ersetzt, jeweils bytegleich zu `worlds/reference/design-guide/assets/rooms/`.
- `worlds/reference/world.yaml` hat bislang nur die Inhaltsversion von 1.0.0 auf 1.0.1 erhöht; Format ist noch 1.0.0.
- Neu und bislang unversioniert: `worlds/reference/design-guide/index.html`, die beiden Raumvorlagen und Mara/Bruno einschließlich Legacy-Sheets in `worlds/reference/design-guide/assets/`.
- Haupt-Sheets: 256 × 288, vier Spalten à 64 × 96; rechts/hinten/vorne, links gespiegelt. Legacy-Sheets: 192 × 192 mit 48 × 64-Frames. Kontinuität und Zuschnitt sind visuell noch nicht abgenommen.
- Es gibt noch keine separaten Tischmasken. Laufpolygone, Hindernisse, Spawns und Ausgänge sind unverändert. Design-Guide-Koordinaten beschreiben den bisherigen Stand und sind kein Beleg für passende neue Geometrie.

## Acceptance criteria

- [x] Design Guide und vorhandene Assets sichten und als Grundlage erhalten. Referenzwelt nutzt die neuen Lounge-/Gartenbilder und bietet Mara/Bruno über den Weltkatalog aus Ticket 14 an. Haupt-Sheets mit 64 × 96-Frames bevorzugen; abweichende Entscheidung begründen. Keine globale Ersetzung der Figur für andere Welten.
- [x] Beide Figuren bei Stand, allen Laufphasen und Blickrichtungen im Spiel prüfen. Angeschnittene Frames, unstete Fußpunkte und störende Animationssprünge vor Abnahme korrigieren oder als konkrete Abnahmegrenze offen melden; keine ungeprüfte Behauptung fertiger Animationsqualität.
- [x] Passgenaue transparente Tischgrafiken für beide Räume erstellen und über Ticket 15 einbinden. Silhouette, Deckkraft, Farben und Position stimmen mit dem Hintergrund überein; Lücken bleiben transparent. Keine doppelten Konturen, sichtbaren Nähte oder Abdeckung außerhalb des Tisches. Hintergrundänderungen nur soweit zur sauberen Integration erforderlich.
- [x] Figur kann hinter und vor beiden Tischen stehen und die erwartete Teilverdeckung zeigen. Laufpolygone und Tischhindernisse an die sichtbaren Objekte anpassen, soweit nötig. Hintere Laufbereiche müssen erreichbar sein; bestehende Navigationsabstände berücksichtigen.
- [x] Türbereiche, Beschriftungen/Interaktion, Spawnpunkte und Übergänge passen sichtbar zu den neuen Bildern. Lounge und Garten sind in beide Richtungen erreichbar; kein Spawn im Hindernis und kein unzugänglicher Ausgang.
- [x] Logischer Raum bleibt grundsätzlich 640 × 360 bei proportional gezeichnetem 1280 × 720-Hintergrund. Zielhöhe der Figuren bewusst visuell wählen und über vorhandene Raumangaben ausdrücken. Neue Bilder allein vergrößern keine Weltkoordinaten.
- [x] Raumeditor lädt, zeigt, speichert und öffnet beide Räume einschließlich Objekten ohne Informationsverlust. Vorschau und Spiel zeigen dieselbe Figurenposition, Größenwirkung und Verdeckung.
- [x] Vor Umstellung der Referenzwelt ihre bisherige veröffentlichte Fassung aus dem Git-Basisstand (nicht aus den bereits ersetzten Arbeitsdateien) als eigenständige unveränderte Kompatibilitätsfixture sichern. Bestehende Tests auf die eingefrorene Fixture ausrichten, Zusicherungen und Hashprüfungen erhalten, keine alten Erwartungswerte nur zur Anpassung an das Redesign überschreiben. Alte Schemas/Teilschemas unverändert lassen.
- [x] Manifest verwendet die tatsächlich erforderliche unterstützte Formatversion aus 14/15. Endgültige Inhaltsversion anhand der inhaltlichen Änderungen bewusst begründen; die bereits eingetragene 1.0.1 nicht ungeprüft als finale Version übernehmen. Ressourcen bleiben vollständig lokal im Weltpaket.
- [x] Herkunft und Nutzungsrechte der gelieferten und neu bearbeiteten Bilder ehrlich dokumentieren; keine Lizenz erfinden. Design Guide mit tatsächlichen Dateipfaden, Rasterangaben und erreichtem Integrationsstand aktualisieren; Aussagen über ungetestete Vorlagen durch tatsächliche Belege ersetzen, soweit geprüft.
- [x] README/Demoanleitung beschreibt Weltwahl, Figurenwahl, Weg zwischen den Räumen und Stellen zum Testen der Tischverdeckung. PLAN, Ticket und active-state enthalten tatsächliche Checks und ready-for-human; menschliche Sichtabnahme separat.

## Abgrenzung zu Ticket 13

Dieses Ticket integriert die zwei Räume der neuen Art Direction ohne Tiefenskalierung. Auf ausdrücklichen Benutzerwunsch vom 04.11.2026 folgt Ticket 13 erst nach 14, 15 und 16. Dessen drei Demo-Szenarien je mitgelieferter Welt werden erst dort ergänzt und blockieren die Abnahme dieses Tickets nicht. Ticket 13 setzt auf der hier fertiggestellten Referenzwelt auf; seine Kriterien werden dadurch nicht gestrichen. Tiefenskalierung ist für Tischverdeckung nicht erforderlich.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Weltlade-/Ressourcen- und Editor-Speichergrenzen sowie Browserablauf mit zwei Teilnehmern. Beide Figuren, beide Räume, Türwechsel, Chat und Reconnect; erreichbare Punkte hinter/vor beiden Tischen; passende Kollision; Editor speichern/neuladen/Vorschau. Alte veröffentlichte Welten über die eingefrorenen Fixtures weiter prüfen. Typcheck, Build und einschlägige vollständige Suite nach Integration; Bildprüfungen ergänzen, keine privaten Assetpfade als alleinigen Funktionsbeleg verwenden.

Menschliche Abnahme: beide Figuren auswählen und in allen Richtungen laufen, hinter/vor beiden Tischen stehen, Türen hin und zurück benutzen, Namen/Sprechblasen mit zweitem Browser vergleichen und dieselben Stellen im Editor prüfen. Belege mit konkreten Positionen/Screenshots ergänzen. Numerische Tests ersetzen die gestalterische Abnahme nicht.

## Umsetzung und Belege · 05.11.2026

- Bestätigter Umfang umgesetzt: Weltkatalog mit Mara/Bruno, Default Mara; Hauptquellraster 64 × 96, Raumhöhe 96, sichtbarer Körper 72 Pixel. Stand und alle vier Laufphasen in vier Richtungen im öffentlichen CharacterRenderer geprüft; Fußpunkt und seitliche Ränder abgesichert. Übergroße erste Mara-Rückenpose durch einheitliche sichtbare Höhe korrigiert. Anatomische Feinheiten und zeitliches Bewegungsgefühl bleiben ausdrücklich menschliche Sichtabnahme.
- Beide Räume bleiben 640 × 360. Abweichung von der angenommenen direkten Nutzung der 1280 × 720-Bilder: Der erste neue Ladetest scheiterte am bestehenden exakten PNG-Größenvertrag. Deshalb unveränderte Originale erhalten und proportionale Spielkopien erzeugt; keine Änderung des Formats oder seiner Interpretation. Transparente Tischgrafiken mit Originalpixeln und durchsichtigen Zwischenräumen, passend zu denselben Abtastpunkten wie die Hintergründe. Generierter Freistellungsversuch nicht übernommen, weil die Silhouette abwich.
- Geometrie, Spawns, Tür-Hitboxen und Ziele angepasst. Fußabdruck und überstehende Tischplatte getrennt; keine Tiefenskalierung. Reviewkorrektur: hintere Gartenzone auf sichtbares Pflaster begrenzt, Mauerpunkt (330,230) wird abgelehnt. Hintere Ziele Lounge (330,258), Garten (330,278), vordere Ziele jeweils (330,330). Türziele Lounge (572,244), Garten (66,304).
- Format 1.4.0 verwendet nur bereits unterstützte Figuren-/Objektfelder. Inhaltsversion 2.0.0 wegen grundlegend ersetzter Gestaltung und Navigation. Keine neuen Abhängigkeiten. Alle veröffentlichten Schemas/Teilschemas und Fixtures unverändert; alle fünf Git-Blob-IDs der reference-v1-Fixture identisch zur Referenzwelt in b3d0b83. Bestehende Hashprüfungen und alte Verhaltenstests erhalten.
- Rechteherkunft ehrlich in worlds/reference/ART-PROVENANCE.md dokumentiert; keine Lizenzbehauptung. README und Design Guide aktualisiert. Sechs Spielbilder durch scripts/build-reference-art.ps1 bytegleich reproduziert. Gelieferte Originale unverändert; .scratch/foreground-demo unangetastet.
- TDD an bestätigter öffentlicher Weltlade-/Navigationsgrenze: neuer Test zuerst rot (Bildabmessungen), danach grün. Neue Browserfälle: zwei Teilnehmer mit unterschiedlichen Figuren, Hin-/Rückwege, Chat/Sprechblase, echter Reconnect, Editor-Roundtrip beider Räume und Vorschauziel hinter dem Tisch; tatsächliche Grafiken in 40 Richtungs-/Phasenansichten; Pixelvergleich beider Figuren in beiden Räumen (hinten sichtbar verdeckt, vorne identischer Bildinhalt mit/ohne Masken und somit keine sichtbaren Nähte).
- Typcheck und Clientbuild bestanden. Abschließende Vollsuite: 234 Tests bestanden, 0 Warnungen. Drei gezielte Browserfälle zusätzlich nach Anpassung der Bildlade-Wartebedingungen erfolgreich (42,0 s, 0 Warnungen). Diffprüfung sauber.
- Skill code-review auf Basis c3eaa294a0b397512db49043778b6f17acb81a40, zwei separate Subagenten: Standards und Spec nach Korrektur ohne offene Befunde. Korrigiert: Garten-Mauerzone, gemeinsame Bildabtastung, explizites Dekodieren der Testgrafiken, Textkodierung im Design Guide. Keine technische Prüfung als menschliche Abnahme gewertet.

Belege (lokal versioniert): [40 Figurenansichten](../evidence/16/reference-poses.png), [Lounge hinter dem Tisch](../evidence/16/reference-lounge-behind-mara.png), [Garten hinter dem Tisch](../evidence/16/reference-garden-behind-bruno.png), [Garten-Editorvorschau](../evidence/16/reference-editor-garden.png).

### Konkrete Abnahme

1. Aus dem Projekt starten: `dotnet run --project src/AdventureChat.Server -- --World:Root D:\adventure-chat\worlds\reference --authoring`.
2. Zwei Browser öffnen, Referenzwelt wählen, unterschiedliche Namen und Mara/Bruno auswählen. Jeweils in vier Richtungen laufen und anhalten; Fußpunkt, Größe und Animationswechsel bewerten.
3. Hinter den Lounge-Tisch (330,258) und davor (330,330) laufen. Tischkonturen, Glas/Aschenbecher/Buch sowie transparente Beinzwischenräume prüfen. D zeigt die Geometrie.
4. Rechte Tür zum Garten benutzen. Hinter den Tisch (330,278), danach davor (330,330) laufen; Laterne/Krug/Vase und Maskenkanten prüfen. Linke Tür zurück benutzen.
5. Chat senden: Name und Sprechblase im zweiten Browser vergleichen. Verbindung unterbrechen/wiederherstellen und auf genau eine fremde Figur achten.
6. Editor mit worldId=reference öffnen, beide Räume speichern/neuladen und die gleichen Tischziele in der Vorschau anklicken. Größenwirkung und Verdeckung mit dem Spiel vergleichen.

Status ready-for-human; noch keine menschliche Abnahme. Lokaler Implementierungscommit in der Git-Historie unter „Integrate redesigned reference world (ticket 16)“.
