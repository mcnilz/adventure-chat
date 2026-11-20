# 26 – Figurenposen und Animationen an Objektaktionen binden

**What to build:** Autoren können einer Objektaktion einen Bedienpunkt, eine Blickrichtung und eine Figurenpose oder kurze Animation zuordnen. Eine Pose kann für eine fortdauernde Benutzung gehalten werden. Eine kurze Geste kann einer sichtbaren Objektwirkung vorausgehen. Dieselben Angaben stehen für unterschiedliche geeignete Interaktionen zur Verfügung.

**Blocked by:** [21](21-state-dependent-navigation.md), [22](22-verbs-and-interaction-editor.md).

**Status:** ready-for-human

## Acceptance criteria

- [x] Die Figur erreicht den angegebenen Bedienpunkt, richtet sich aus und zeigt die für diese Aktion konfigurierte Pose oder Animation. Der Autor kann einer anderen Aktion am selben oder einem anderen Objekt eine andere Darstellung zuordnen.
- [x] Eine gehaltene Pose endet über die vereinbarte Bedienung und die Figur kann anschließend wieder gehen. Eine kurze Geste kann vor einem definierten Objektzustandswechsel abgespielt werden. Andere Besucher im Raum sehen die Handlung und später Eintretende den erreichten Objektzustand.
- [x] Aktionsvoraussetzungen können vorhandene Objektzustände verwenden. Ein gesperrter oder unerreichbarer Bedienpunkt, wiederholte Eingabe und vorzeitig beendete Benutzung haben definierte Ergebnisse. Zwei Besucher erhalten bei derselben belegbaren Stelle ein konsistentes Ergebnis.
- [x] Erreichbare Beispiele in der Referenzwelt zeigen beide Fälle. Vorgeschlagen sind eine Sitzpose an einem freigegebenen Platz, etwa bei abgesenktem Schreibtisch, und ein kurzes Handheben, nach dem ein vorbereiteter Tafelinhalt erscheint. Beide verwenden dieselbe konfigurierbare Zuordnung.

## Bestätigte Entscheidungen und Testgrenzen

Am 20.11.2026 mit „ja“ zur Umsetzung freigegeben. Ein neuer Gehauftrag, Raumwechsel, Verbindungsende oder der Wechsel des vorausgesetzten Zustands beendet die Pose. Der Server vergibt den Platz nach seiner Ankunftsreihenfolge. Wiederholte identische Aufträge laufen unverändert weiter. Gesperrte oder unerreichbare Punkte melden den Grund lokal. Eine Geste wirkt nach Ablauf, wenn die Prüfung dann erfolgreich ist. Bei Abbruch vor dem Ablauf entfällt die Wirkung. Für Figuren ohne zugeordnetes Bild verwendet der Renderer das vorhandene Standbild in der konfigurierten Blickrichtung.

Geprüft werden Laden, Validieren, Speichern und Neuladen, gültige und ungültige Animationsdaten, der Formatvertrag älterer Welten und öffentliche Serverkommandos. Zwei Browser prüfen Anlauf, Ausrichtung, Canvas-Darstellung, konkurrierende Benutzung, Abbruch und späteren Eintritt. Die Lesbarkeit und das Bediengefühl bleiben menschliche Sichtabnahme.

## Umsetzung und Kompatibilität

Die Werkstatt verwendet die Zuordnung am vorhandenen Stuhl und an der neuen Tafel. Der Zustand `pulled` gibt **Hinsetzen** frei. Die Geste an der Tafel zeigt nach 1500 ms den vorbereiteten Werkstattplan. Am selben Objekt ist ein weiterer Klick mit **Benutzen** die Gegenaktion. Damit lassen sich beide Arten im selben Raum spielen und im Raumeditor unter **Aktionen → Figurenanimation** bearbeiten. Die Referenzwelt verwendet Weltformat **1.10.0** und Inhaltsversion **5.1.0**. Der Zusatz ist optional. Bestehende Aktionen ohne Figurenanimation behalten Bedienradius und unmittelbare Wirkungen. Die neuen PNGs sind eigens für die Werkstatt gezeichnet. [Formatvertrag](../../../CONTENT-FORMAT.md#figurenposen-und-gesten-ab-1100) · [Grafikherkunft](../../../worlds/reference/ART-PROVENANCE.md#sitzpose-und-tafel--ticket-26)

Die Grafik wird beim Raumaufbau vorgeladen. Der ausführende Browser wartet vor der Eingabe auf die Dekodierung der zugeordneten Bilder. Ein anderer Browser kann bei besonders langsamer Übertragung anfangs das Standbild sehen. Die serverseitige Dauer und Objektwirkung sind für alle dieselben.

## Prüfbelege am 20.11.2026

- TypeScript-Typcheck, Clientbuild und .NET-Build bestanden. Der .NET-Build meldete 0 Warnungen. Die finale Vollsuite bestand **362 Tests** (246 Server, 116 Browser in drei Gruppen). Die Ergebnisdateien liegen unter `.scratch/adventure-chat/evidence/26/final-tests/`.
- Die gezielten Serverprüfungen decken YAML-Formatgrenzen, Figurenkatalog und PNG-Raster, belegte oder gesperrte Punkte, geordnete Ankünfte, wechselnde Ausgangszustände und die Gestenfrist ab. Die Browser prüfen Sitzen und Handheben im Canvas, Anlauf und Abbruch, späteren Eintritt sowie Speichern, Neuladen und Vorschau. Der erste Vollsuite-Lauf deckte einen fehlerhaften Vorschau-Raumwechsel mit Figuren-ID auf. Der bestehende Test besteht nach Korrektur in der finalen Suite.
- Unabhängige Reviews zu Anforderungen und Standards prüften den Diff seit `ee2dfb4`. Befunde zur Reihenfolge, Frist, Wiederholung, Bildvorbereitung und Inhaltsvalidierung wurden behoben und nachgeprüft. Die 112 bereits versionierten Dateien der älteren Schemas, Fixtures und mitgelieferten Altwelten sind unverändert. Die Abhängigkeits- und Lockdateien sind unverändert.
- Lokaler Commit `Add actor animations to object actions (ticket 26)`. Die menschliche Sichtabnahme steht aus.

## Manuelle Abnahme

1. [Lokale Abnahmekopie](http://127.0.0.1:61395/) in zwei Browserfenstern öffnen und mit verschiedenen Namen eintreten. Von der Lounge durch die rechte Tür in den Garten gehen. Die Holztür im efeubewachsenen Steinbogen führt zur Werkstatt.
2. Mit **Ziehen** den Stuhl vom Tisch rücken. Mit **Benutzen** direkt Platz nehmen. Beide Fenster sollen dieselbe Sitzpose sehen. Im zweiten Fenster dieselbe Aktion probieren. Der Platz ist belegt. Die erste Figur mit **Gehe zu** auf freie Fliesen schicken und im zweiten Fenster erneut **Benutzen** wählen.
3. **Benutzen** auf der leeren Tafel links an der Rückwand wählen. Die Figur soll zur Tafel gehen, nach oben schauen und die Hand heben. Erst danach erscheint der Werkstattplan in beiden Fenstern. Ein weiterer Klick mit **Benutzen** leert die Tafel. Die Geste wiederholen und vor der Wirkung einen Gehauftrag geben. Die Tafel soll leer bleiben. Nach erfolgreicher Geste mit einem frischen Besuch die beschriftete Tafel prüfen.
4. Im [Editor der Abnahmekopie](http://127.0.0.1:61395/editor.html?worldId=reference) **Werkstatt → board → Aktionen → Figurenanimation** öffnen. Dauer und Blickrichtung prüfen. Optional eine Dauer ändern, speichern, neu laden und die Geste über den Vorschaumodus ausprobieren.

Spiel und Editor der getrennten Abnahmekopie wurden mit HTTP 200 geprüft. Die aktualisierte Kopie liegt unter `.scratch/adventure-chat/evidence/26/acceptance-direct-verbs` und läuft als Prozess 33928. Sitzpose, erhobene Hand und geschriebener Plan sind als Browseraufnahmen im Belegordner festgehalten.

## Nachtrag zur direkten Verbenbedienung

Am 20.11.2026 bestätigte der Benutzer die Funktion und bat darum, alle Aktionsuntermenüs aus der Verbenleiste zu entfernen. Spiel und Editorvorschau führen pro Verb und Objektklick direkt eine Aktion aus. Der zurückgeschobene Stuhl weist **Benutzen** dem Hinsetzen zu. Der Stuhl am Tisch lässt sich weiterhin mit **Ziehen** zurückschieben. Die Regel für vorhandene frei benannte Aktionen steht im [Formatvertrag](../../../CONTENT-FORMAT.md#interaktionen-ab-weltformat-160-ticket-20).

Referenzinhalt **5.1.1** enthält die angepasste Aktionsbezeichnung. Die Änderung betrifft die Auswahl im Browser. Format **1.10.0**, veröffentlichte Schemata, gespeicherte Aktions-IDs und serverseitige Ausführung bleiben unverändert. Menschliche Abnahme des Nachtrags steht aus.

Typcheck, Clientbuild und .NET-Build mit 0 Warnungen bestanden. Die vollständige Suite bestand erneut **362 Tests** (246 Server, 116 Browser). Die Browser prüfen unter anderem das direkte Hinsetzen, den Stuhl am Tisch, den Schalter mit mehreren gespeicherten Aktionen und Spiel sowie Editorvorschau. Belege stehen unter `.scratch/adventure-chat/evidence/26/direct-verbs-tests`. Lokaler Nachtragscommit `Execute object verbs directly (ticket 26)`.
