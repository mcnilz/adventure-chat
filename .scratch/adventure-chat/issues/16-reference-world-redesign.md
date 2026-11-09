# 16: Referenzwelt nach neuem Design Guide integrieren

**What to build:** Lounge und Tavernen-Garten bilden eine spielbare, visuell stimmige Referenzwelt nach dem neuen Design Guide. Mara und Bruno sind auswählbar; Tische verdecken dahinter stehende Figuren korrekt. Raumgeometrie und Grafik stimmen überein.

**Blocked by:** 14, 15; Ticket 12 ist done. Ticket 13 folgt erst nach diesem Ticket und ist kein Blocker.

**Status:** done

## Menschliche Abnahme · 09.11.2026

Benutzer: „Ticket 16 neues design wurde umgesetzt, ist abgenommen“. Damit ist die abschließende Native128-Fassung einschließlich der bislang offenen Sichtabnahme abgenommen. Die folgenden ready-for-human-Angaben dokumentieren den historischen Prüfstand vor dieser Abnahme.

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

- Bestätigter Umfang umgesetzt: Weltkatalog mit Mara/Bruno, Default Mara; gelieferte Hauptframes 64 × 96 erhalten. Nach Größenfeedback Spielraster 64 × 72 ohne Kopfraum, Raumhöhe 128 und sichtbarer Körper 128 Raumpixel. Abweichung vom bevorzugten Hauptquellraster ist bewusst: Der transparente 24-Pixel-Kopfbereich hatte zusammen mit Raumhöhe96 nur 72 sichtbare Pixel ergeben, zu klein im Verhältnis zu Möbeln. Stand und alle vier Laufphasen in vier Richtungen im öffentlichen CharacterRenderer geprüft; Fußpunkt und seitliche Ränder abgesichert. Übergroße erste Mara-Rückenpose durch einheitliche sichtbare Höhe korrigiert. Anatomische Feinheiten und zeitliches Bewegungsgefühl bleiben ausdrücklich menschliche Sichtabnahme.
- Beide Räume bleiben 640 × 360. Abweichung von der angenommenen direkten Nutzung der 1280 × 720-Bilder: Der erste neue Ladetest scheiterte am bestehenden exakten PNG-Größenvertrag. Deshalb unveränderte Originale erhalten und proportionale Spielkopien erzeugt; keine Änderung des Formats oder seiner Interpretation. Transparente Tischgrafiken mit Originalpixeln und durchsichtigen Zwischenräumen, passend zu denselben Abtastpunkten wie die Hintergründe. Generierter Freistellungsversuch nicht übernommen, weil die Silhouette abwich.
- Geometrie, Spawns, Tür-Hitboxen und Ziele angepasst. Fußabdruck und überstehende Tischplatte getrennt; keine Tiefenskalierung. Reviewkorrektur: hintere Gartenzone auf sichtbares Pflaster begrenzt, Mauerpunkt (330,230) wird abgelehnt. Hintere Ziele Lounge (330,258), Garten (330,278), vordere Ziele jeweils (330,330). Türziele Lounge (572,244), Garten (66,304).
- Format 1.4.0 verwendet nur bereits unterstützte Figuren-/Objektfelder. Inhaltsversion 2.0.0 wegen grundlegend ersetzter Gestaltung und Navigation, danach visuelle Patchkorrektur 2.0.1 für die Größenrelation ohne Navigationsänderung. Keine neuen Abhängigkeiten. Alle veröffentlichten Schemas/Teilschemas und Fixtures unverändert; alle fünf Git-Blob-IDs der reference-v1-Fixture identisch zur Referenzwelt in b3d0b83. Bestehende Hashprüfungen und alte Verhaltenstests erhalten.
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

## Größenfeedback · 05.11.2026

Benutzer: „nur die figur größe stimmt nicht zur relation im raum“, belegt mit Lounge-Screenshot. Feedback setzt Ticket16 auf in-progress; keine vorweggenommene Abnahme durch „das ist schon super“.

Öffentlicher Renderer-Regressionslauf zuerst rot: tatsächlich nur72 sichtbare Pixel (minY88 statt Soll32–34 bei Fußpunkt160). Derselbe Test verwendet nun die tatsächliche Raumkonfiguration und prüft128 sichtbare Pixel für beide Figuren in allen40 Richtungs-/Phasenansichten. Korrektur: Spielkopien ohne24 transparente Kopfzeilen, Katalograster64×72 und actorHeight128 in beiden Räumen. Fußpunkt unverändert; Namen und Sprechblasen liegen dadurch direkt über dem sichtbaren Körper. Keine generative Überarbeitung und keine Änderung des globalen Renderers. Originale und andere Welten unverändert. Reproduzierbare Neuerzeugung aller sechs Spielbilder bytegleich geprüft.

Typcheck und Clientbuild erfolgreich; drei gezielte Browserfälle grün (43,5s). Abschließende Vollsuite erneut234 Tests bestanden,0 Warnungen (190,5s). Erneutes Standards-/Spec-Review gegen8265a4d in zwei separaten Subagenten: keine offenen Befunde; größere Figurenrelation anhand frischer Spiel-/Editorscreenshots bestätigt. Gespeicherte Screenshotbelege oben aktualisiert; zusätzlich [Bruno vor Tisch und Bar](../evidence/16/reference-lounge-front-bruno.png). Status wieder ready-for-human. Neue Sichtabnahme nach Serverneustart und hartem Browser-Neuladen; danach dieselben Schritte oben für beide Figuren/Räume durchführen. Lokaler Korrekturcommit „Fix reference character scale relative to furniture“.

## Native128-Lieferung eingebunden · 05.11.2026

Nach erneutem Feedback zur groben Vergrößerung lieferte der Benutzer neue Open-Design-Grafiken unter `worlds/reference/design-guide/assets/characters/native128`. Die fertigen 512 × 384-Sheets wurden bytegleich in die Spielgrafiken übernommen. Katalograster nun 128 × 128; Raumhöhe weiterhin 128, somit keine Größenänderung beim Zeichnen. Sichtbarer Körper 125 Pixel, drei Pixel oberer Rand, Fußanker unverändert. Die Neuzeichnung und Rasteraufbereitung stammen aus der Lieferung; dieser Integrationsschritt hat keine Pixel neu erzeugt oder bearbeitet.

Der Bildbuild kopiert jetzt nur die fertigen nativen Figuren-Sheets. Originale, Lieferbeleg, Kontaktbogen, GIFs, acht Vorschauen und Generierungsquellen erhalten; der mitgelieferte Python/Pillow-Helfer bleibt bewusst lokal und unversioniert und wurde nicht ausgeführt oder als Projektwerkzeug aufgenommen. Ein sauberer Checkout benötigt ihn nicht. Keine neuen Projektabhängigkeiten. Design Guide und README zeigen/beschreiben die eingebundenen Grafiken; Herkunftshinweise unterscheiden Lieferung und Spieleinbindung.

Inhaltsversion 2.0.2: visuelle Patchkorrektur, keine Änderung von IDs, Navigation oder Formatinterpretation. Format 1.4.0, veröffentlichte Schemas/Teilschemas und ältere Fixtures unverändert. Alle sechs Raum-/Tisch-PNGs gegen ihre bisherigen Git-LFS-SHA256-Werte unverändert geprüft, beide Figuren-Spielkopien bytegleich zu den gelieferten Sheets.

TDD an bestehender öffentlicher Renderergrenze: erst rot (minY32 statt35, 30996 abweichende Farbkanäle beim ersten Frame), danach grün. Alle 40 Richtungs-/Phasenansichten zeigen 125 sichtbare Pixel mit identischem Fußpunkt; jedes gezeichnete RGBA-Pixel entspricht seinem nativen Quellframe, links exakt gespiegelt. Drei gezielte Browserfälle bestanden (42,6 s): Figurenwahl, Bewegung, Chat/Sprechblase, Hin-/Rückweg, echter Reconnect, Editor-Roundtrip und Vorschau sowie Tischverdeckung mit echten Grafiken. Typcheck/Clientbuild bestanden; abschließende Vollsuite 234 Tests bestanden, 0 Warnungen (193,4 s). Beide erneuten Subagent-Reviewachsen gegen 382a83f nach Präzisierung des lokalen Lieferwerkzeugs ohne offene Befunde. Screenshots unter `../evidence/16/` aktualisiert und im Spiel/Editor gesichtet.

Status ready-for-human; keine menschliche Abnahme. Die subtilen Profil-Gegenphasen, gespreizte Standpose und das Bewegungsgefühl bleiben ausdrücklich offene Sichtabnahmegrenzen aus dem Lieferbeleg. Zum Prüfen Server neu starten, Browser mit Strg+F5 neu laden und die Abnahmeschritte oben mit Mara/Bruno wiederholen. Lokaler Commit „Integrate native128 reference characters“.
