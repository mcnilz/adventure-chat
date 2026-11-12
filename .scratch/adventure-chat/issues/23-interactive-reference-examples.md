# 23: Interaktive Referenzbeispiele für Tür, Stuhl und Schalter/Tor

**What to build:** Eine kleine erreichbare Referenzszene demonstriert die Erweiterung vollständig: Tür öffnen/schließen, Stuhl zwischen „am Tisch“ und „zurückgeschoben“ bewegen und ein Tor über einen direkt verbundenen Schalter bedienen. Zwei Spieler können Änderungen gegenseitig rückgängig machen und die geänderten Wege nutzen.

**Blocked by:** [20](20-shared-object-actions.md), [21](21-state-dependent-navigation.md), [22](22-verbs-and-interaction-editor.md) sowie 19 müssen done sein. 19 ist aktuell ready-for-human; dieser Plan nimmt seine Gestaltung nicht ab und ändert sie nicht.

**Status:** draft

## Auftrag und Freigabegrenze

Am 10.11.2026 ausschließlich Planung beauftragt. Jetzt keine Bilder erzeugen, Weltdateien anpassen, Server starten oder bestehende Abnahmeumgebungen verändern. Die spätere Inhaltsintegration benötigt eine separate Paketfreigabe. [SPEC](../../../SPEC.md), [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md).

## Umfang und vorgeschlagener Zuschnitt

- Eine Handvoll nutzbarer Raumobjekte in der mitgelieferten Referenzwelt: Tür, Stuhl, Schalter und verbundenes Tor. Exakte Szene/Einbindung vor Umsetzung anhand des abgenommenen Stands von 19 auswählen; vorzugsweise eine ergänzende kleine Interaktionsszene, die die vorhandene Tour erhält.
- Türzustand ändert Darstellung und Durchgang. Stuhl blockiert in beiden festen Positionen an der passenden Stelle. Drücken/Ziehen ist reversibel; keine freie Positionierung im Spiel.
- Schalter und Tor ändern sich mit einer expliziten Aktion konsistent. Keine automatische Kette, kein Inventar, kein Aufheben und keine Dialoge. Szene erlaubt Gegenaktionen durch den jeweils anderen Spieler.
- Geeignete freie Flächen und Sicherheitsanker machen erfolgreiche Aktionen möglich. Ein dokumentierter sicherer Prüfplatz demonstriert eine wegen Figur/Sicherheitsregel abgelehnte Änderung ohne jemanden festzusetzen.
- Vorhandene freigegebene Bilder bevorzugen. Fehlende geeignete Tür-/Stuhl-/Tormotive und deren Beschaffung vor Paketfreigabe klären; dieser Plan autorisiert keine neue Bilderzeugung. Herkunft/Lizenz/Originalerhalt auch bei späterer Bearbeitung dokumentieren.

## Acceptance criteria

- [ ] Szene ist über regulären Eintritt und dokumentierten Rundweg erreichbar. Zwei unabhängige Browser können alle vier Objekte über die Verbenbedienung nutzen, gegenseitig zurückstellen und weiter laufen/chatten.
- [ ] Geöffnete Tür/Tor geben die konfigurierten Wege frei; geschlossene sperren. Stuhlposition und Hindernisfläche stimmen visuell überein. Laufende Route reagiert wie in 21 abgenommen; keine optisch offene, geometrisch unerreichbare Passage.
- [ ] Direkte Schalter-/Torwirkung und Sicherheitsablehnung sind an der Szene nachvollziehbar. Sichere Gegenaktion funktioniert anschließend. Keine Rätselvoraussetzung verhindert die Demonstration.
- [ ] Später eintretender Spieler, Raumrückkehr und Reconnect sehen den aktuellen Zustand. Neustart der Welt zeigt deklarierte Anfangszustände; keine Zustandspersistenz hinzufügen.
- [ ] Dieselben Definitionen sind im Browsereditor vollständig sichtbar und bearbeitbar. Speichern/Neuladen und isolierte gemeinsame Vorschau demonstrieren dieselben Aktionen, Hindernisse und sicheren Ablehnungen. Für manuelle Bearbeitung eine Arbeitskopie verwenden, keine eingefrorene Fixture.
- [ ] Bestehende Referenzräume/Tour, Figuren, Scrollen/Tiefe und Weltisolation bleiben prüfbar. Autorenoriginale, veröffentlichte Schemas und sämtliche älteren Referenzfixtures bleiben unverändert. Keine bestehende Benutzer-Weltkopie überschreiben.
- [ ] Verwendete Formatversion entspricht dem dann veröffentlichten Vertrag aus 20/21, für 21 nun 1.8.0 geplant. Kein neues Format allein für Beispiele. Inhalts-SemVer vor Integration begründen: aktuell reference 3.0.0; rein zusätzliche erreichbare Szene mit erhaltenen Zusagen kann Minor sein, geänderte bisherige Laufbereiche/Spawns/Übergangssemantik verlangen nach Projektvertrag Major. Konkrete Inhaltsversion erst mit Szenenentscheidung festlegen.
- [ ] README/Herkunftshinweise, Editor-Demo, tatsächliche Testbelege, Grenzen und verständliche manuelle Abnahmeschritte ergänzen; ready-for-human, erst ausdrückliche Abnahme done.

## Vorgeschlagene Testgrenzen – vor Implementierung bestätigen

Öffentliche Inhalts-/Navigationsgrenze und echte Browser-/SignalR-Abläufe mit zwei Teilnehmern sowie einem späteren Eintritt. Alle reversiblen Aktionen, konsistente Verknüpfung, Tür-/Stuhlwege, abgelehnte Überdeckung/Einschließung, Änderung während Bewegung, Raumrückkehr, echter Reconnect und Neustart in isolierter Weltkopie. Editor laden/speichern/neuladen und dieselbe Szene in isolierter Vorschau bedienen. Bestehende Tour und ältere unveränderte Fixtures als Regression; Typcheck/Build und vollständige einschlägige Suite.

Screenshots in beiden Stuhl-/Türzuständen und mit zwei Figuren ergänzen den Funktionsnachweis. Grafische Glaubwürdigkeit, Bediengefühl und sichtbare Verbindung zwischen Schalter und Tor bleiben menschliche Abnahme. Keine Aussage über beliebige vom Autor gebaute Rätsel oder global einschließungsfreie Welten.

## Offene Detailentscheidungen vor Paketfreigabe

Ort/Anbindung der Szene, geeignete vorhandene oder separat freizugebende Grafiken, Namen/Bedienpunkte und konkrete Sicherheitsanker anhand der in 21 bestätigten Regel. Inhaltsversion begründen. Kein neuer Featureumfang oder neues Bildverfahren aus diesem Planungsticket ableiten.

## Stand / Abnahme

10.11.2026: draft, nur Planung; keine Welten oder Bilder verändert, keine Implementierung oder Testausführung behauptet.
