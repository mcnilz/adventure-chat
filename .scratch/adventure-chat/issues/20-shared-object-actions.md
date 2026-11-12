# 20: Gemeinsame Objektzustände, Aktionen und direkte Verknüpfungen

**What to build:** Zwei Spieler verändern dieselben interaktiven Raumobjekte und sehen unmittelbar denselben bestätigten Zustand. Autorendaten definieren endliche Anfangszustände, erlaubte Aktionen und deren direkte Auswirkungen. Eine Aktion kann Schalter und verbundenes Tor gemeinsam ändern. Ein späterer Eintritt erhält den aktuellen Zustand.

**Blocked by:** 09, 10, 15 (alle done). Keine implizite Abnahme von 18 oder 19; deren noch offene Sichtabnahmen sind keine Voraussetzung für den unabhängigen Zustandsvertrag.

**Status:** ready-for-human

## Auftrag und Freigabegrenze

Am 10.11.2026 im Sprachgespräch bestätigte Erweiterungsplanung: „Das passt erstmal so leg das mal an“. Das bestätigt die Richtung und das Anlegen der Tickets 20–23, keine Implementierung, Testgrenzenfreigabe oder Produktabnahme. Grundlage: [SPEC](../../../SPEC.md), [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md), [Kompatibilitätsvertrag](../../../docs/work-packages/world-format-compatibility.md).

## Umfang und Paketgrenze

- Bestehende grafische Raumobjekte um optionale Interaktionsdefinitionen ergänzen: stabile Zustands-/Aktions-IDs, Anzeigenamen, genau ein Anfangszustand und explizite Übergänge aus benannten Zuständen. Zustände können verschiedene vorhandene PNGs, feste Positionen und passende Darstellungsangaben wählen. Darstellung und Tiefenordnung nutzen den gemeinsamen Renderer.
- Tür: öffnen/schließen. Stuhl: exakt zwei Autorenpositionen „am Tisch“ und „zurückgeschoben“; Ziehen beziehungsweise Drücken wechselt zurück und vor. Jeder Spieler darf eine zulässige Gegenaktion ausführen; kein Besitz, kein persönliches Undo und keine freie Möbelphysik.
- Schalteraktion setzt den eigenen und den ausdrücklich benannten Zielobjektzustand zusammen oder gar nicht. Ziele bleiben innerhalb derselben Welt; keine automatische Ausführung weiterer Aktionen durch einen Zustandswechsel, keine Ereignisketten, Ausdrücke oder Skripte.
- Der Server prüft Sitzung, Welt, aktuellen Raum des Handelnden, Objekt, erlaubte Aktion und aktuellen Ausgangszustand. Clients verlangen eine Aktion, setzen keine Zustände oder fremden Identitäten. Zielreferenzen werden aus geprüften Autorendaten aufgelöst.
- Zustände leben pro Weltinstanz, unabhängig von Anwesenheit im Raum. Raum-/Weltwechsel, leerer Raum und Reconnect setzen sie nicht zurück. Neustart der Welt initialisiert die deklarierten Anfangszustände; keine zusätzliche Zustandspersistenz.
- Für einen vorführbaren vertikalen Ausschnitt eine kleine echte Objekt-/Aktionsauswahl über die vorhandene Aktionszeile vorsehen und später in Ticket 22 vervollständigen; kein ausschließliches Testpanel. Kleine neue synthetische Testwelten genügen. Keine Änderungen der Referenzwelt und keine Bilderzeugung in diesem Paket.
- Hindernisse, Sperren und laufende Routen folgen in [21](21-state-dependent-navigation.md); bis dahin haben diese Zustände ausschließlich deklarierte Darstellungswirkungen. Die Demo darf noch keine geschlossene Tür als tatsächliche Wegsperre ausgeben.

## Acceptance criteria

- [ ] Öffnen/Schließen und Drücken/Ziehen sind aus den jeweiligen Zuständen eindeutig definiert. Ein zweiter Spieler kann die Änderung des ersten rückgängig machen. Nicht erlaubte, unbekannte oder inzwischen unpassende Aktionen liefern eine verständliche Rückmeldung ohne Teiländerung.
- [ ] Zwei unabhängige Browser zeigen denselben bestätigten Zustand einschließlich Stuhlposition. Späterer Eintritt, Raumrückkehr und echter Reconnect übernehmen den aktuellen Zustand; Neustart liefert wieder Anfangszustände.
- [ ] Gleichzeitige Befehle werden serverseitig eindeutig geordnet und jeweils gegen den dann aktuellen Zustand geprüft. Eine dokumentierte Revision/Ordnungsregel verhindert, dass verspätete Ereignisse einen neueren Zustand überschreiben. Es wird keine bestimmte Reihenfolge zwischen gleichzeitig eintreffenden Clients versprochen.
- [ ] Schalter und Tor wechseln atomar: kein bestätigter Zwischenzustand, keine teilweise erfolgreiche Aktion. Auch eine im anderen betroffenen Raum anwesende Sitzung beziehungsweise ein späterer Snapshot sieht ein konsistentes Ergebnis. Keine rekursive Auslösung weiterer Objektaktionen.
- [ ] Gleichnamige IDs in anderen Welten bleiben isoliert; manipulierte Sitzungs-, Raum-, Objekt- und Aktionsangaben verändern keinen fremden Zustand.
- [ ] Öffentliche Lade-/Speichergrenze prüft eindeutige IDs, Anfangszustände, endliche Übergänge, vorhandene Ziele und Zielzustände, widersprüchliche Mehrfachzuweisungen sowie Grenzen für Datenmengen. Ungültige Definitionen überschreiben keine gültige Welt.
- [ ] Geplantes Format 1.6.0 als additive Minor-Erweiterung begründen und erst bei Umsetzung separat veröffentlichen. Bisherige Formate erhalten weder Interaktionen noch andere Defaults; neue Felder unter alten Versionen werden abgewiesen. Explizite Servermatrix erweitern, unbekannte Versionen weiter ablehnen. Alle veröffentlichten Schemas/Teilschemas und älteren Referenzfixtures bleiben unverändert und werden als Regression nachgewiesen.
- [ ] Tatsächliche Checks, Review, technische Grenzen und konkrete menschliche Abnahmeschritte dokumentieren; danach ready-for-human. Keine Abnahme aus Tests ableiten.

## Vorgeschlagene Testgrenzen – vor Implementierung bestätigen

Öffentliche Inhalte-Lade-/Speichergrenze, echte Server-/SignalR-Kommandos und Browser-/Canvasgrenze. Kleine unabhängige Weltkopien: gültige/ungültige Zustandsreferenzen, keine Script-Ausführung, Weltisolation mit identischen IDs, zwei konkurrierende Akteure mit dokumentierter Serverreihenfolge, abgewiesene Gegenaktion nach Zustandsänderung, atomare Mehrfachänderung, späterer Eintritt in zuvor leeren Raum, echter Reconnect und Weltneustart. Renderer zeigt beide Stuhlpositionen und Türzustände auch bei Kameraoffset und Skalierung. Ältere unveränderte Fixtures behalten Darstellung und Navigation. Keine Tests privater Sperren, Maps oder Nachrichtenwarteschlangen und keine zusätzliche Test-API.

Automatisierung belegt Reihenfolge und Zustand, menschliche Abnahme bewertet Verständlichkeit und sichtbare Rückmeldung. Typcheck, Clientbuild und einschlägige vollständige .NET-/Browsersuite am Paketende; keine neuen Abhängigkeiten vorausgesetzt.

## Offene Detailentscheidungen vor Paketfreigabe

1. Exakte Feldnamen, Limits, Zustandsdarstellung und Referenzadressierung (Vorschlag: explizite Raum-/Objekt-ID innerhalb der Welt, auch für direkte Verknüpfungen in andere Räume); keine implizite Namenssuche.
2. Interaktionsreichweite und Verhalten beim Anklicken aus der Ferne: Vorschlag definierter erreichbarer Bedienpunkt und servergeprüfte Nähe; automatisches Hinlaufen oder verständliche Ablehnung noch festlegen. Keine Interaktion durch Wände zusichern.
3. Revisions-/Konfliktantworten, wiederholte Befehle und bereits erfüllter Zielzustand festlegen; keine unbemerkte doppelte Umschaltung. Verhalten beim lokalen Neuladen geänderter Autorendaten festlegen: Vorschlag kontrollierter Weltreset statt stiller Zustandsmigration, ohne Zustandspersistenz.
4. 1.6.0 gegen den dann tatsächlichen Veröffentlichungsstand prüfen. Ticket 21 muss bei späterer Veröffentlichung eine weitere Minor-Version erhalten.

## Stand / Abnahme

12.11.2026: Implementierung abgeschlossen, menschliche Abnahme ausstehend. Format 1.6.0 ist eine optionale Minor-Erweiterung; ältere Schemas und Referenzfixtures im Git-Vergleich unverändert. Keine neuen Abhängigkeiten. Typcheck und Clientbuild erfolgreich; abschließende Gesamtsuite 289 Tests in zwei Projekten, 0 Warnungen (378,1 s). Inhaltstests prüfen Struktur, Referenzen, Bilder und Roundtrip; ein echter SignalR-/Browserablauf prüft Konkurrenz, direkte raumübergreifende Wirkung, Weltisolation, manipulierte und veraltete Befehle, zwei Ansichten, Raumrückkehr, echte Verbindungsunterbrechung, Entfernung, Kameraoffset, Skalierung, Editor-Speicherung und Weltneustart. Unabhängige Standards-/Spec-Reviews: gemeldete Modulaufteilung, UI-Kopplung, Kamerasprung und verspäteter Befehl nach Raumwechsel korrigiert und nachgeprüft; keine weiteren konkreten Befunde. Sichtprüfung der separaten Testwelt und Stuhlaktion erfolgt. Navigation bleibt unverändert und die Testgrafiken sind abstrakte Farbmuster. Kein automatischer Schluss auf menschliche Abnahme.

### Menschliche Abnahme

1. [Testwelt öffnen](http://127.0.0.1:54622/) und in zwei unabhängigen Browserfenstern betreten. In Fenster A „Stuhl“ und „Ziehen“ ausführen; Fenster B zeigt die neue Position. In B „Drücken“ wählen und die Gegenaktion in A beobachten.
2. In B den Ausgang am Startpunkt der Figur anklicken (beim Darüberfahren erscheint „Go to Nebenraum“). In A „Schalter“ / „Einschalten“ ausführen; das Tor im Nebenraum bewegt sich in B. „Ausschalten“ stellt es zurück. Die roten Muster sind Platzhalter; das Tor ist noch keine Wegsperre.
3. In einem Fenster von der Startposition weglaufen und eine Aktion versuchen: Die Entfernungsmeldung erscheint. Wieder nahe herangehen und eine erlaubte Aktion ausführen. Raum wechseln oder die Verbindung neu aufbauen: Der gemeinsame Zustand bleibt erhalten.

Die Abnahmeansicht verwendet die isolierte Kopie `.scratch/adventure-chat/evidence/20/acceptance-world`; PID des Startprozesses zuletzt 50192. Erst ausdrückliche Benutzerabnahme setzt das Ticket auf `done`.

Lokaler Commit: `Implement shared object actions (ticket 20)`. Nach rein mechanischer Zeilenendenkorrektur der neuen Schemas sechs Inhaltsfälle erneut bestanden; der gestufte Diff ist whitespace-sauber.

12.11.2026, laufende Umsetzung: Format 1.6.0, Inhaltsprüfung, gemeinsamer Objektzustand mit revisionsgeordneten Snapshots/atomaren direkten Wirkungen, Reset bei Autorenspeicherung und Aktionsauswahl implementiert. `tests/Fixtures/interactions` ist die synthetische Testwelt; eine getrennte Abnahmekopie läuft auf http://127.0.0.1:54622/. Gezielte Inhalts- und Browser-/SignalRprüfungen erfolgreich, einschließlich Konkurrenz, Weltisolation, Ablehnung manipulierter/veralteter Befehle, zwei Browsern, Raumrückkehr, Reconnect, Entfernung, Kameraoffset, Editor-Roundtrip und Neustart. Unabhängiger Standardsreview fand Bündelung und UI-Kopplung; beides korrigiert und nachgeprüft. Spec-Review fand Kamerasprung und verspäteten Befehl nach Raumwechsel; beide rot/grün nachgewiesen und nachgeprüft. Vollsuite läuft; abschließende Ergebnisse, Diffprüfung und Abnahme noch offen. Keine neuen Abhängigkeiten; Schemas/Fixtures bis 1.5.0 im Diff unverändert.

12.11.2026, Fortsetzung: Entwurf des 1.6.0-Schema-Bündels und DTOs für Interaktionen angelegt; alte Schemas unverändert. Neuer Schematest bestätigt Annahme der Interaktionsstruktur durch 1.6.0 und Ablehnung durch 1.5.0. Format 1.6.0 bleibt absichtlich im Server gesperrt, da semantische Validierung, Bildprüfung, Laufzeit und Bedienung noch fehlen. Typcheck und Clientbuild erfolgreich; gezielter Testlauf 1 bestanden, 1 erwartungsgemäß rot. Kein freigegebener Formatvertrag, kein Review oder Commit. Nächster Schritt: referenzielle und geometrische Validierung sowie Dateischutz testgetrieben ergänzen, bevor das Format freigeschaltet wird.

Zwischenzeitlicher vollständiger Lauf: 282 bestanden, ein erwarteter Fehler im Freischaltungstest, 0 Warnungen (389 s). Der später hinzugefügte Schematest wurde separat ausgeführt und war nicht Teil dieses Vollsuite-Laufs.

12.11.2026: Paketumfang, vorgeschlagene Detailentscheidungen und Testgrenzen vom Benutzer mit „ja“ bestätigt. Vorschlag: explizite Raum-/Objekt-IDs, Bedienpunkt mit servergeprüfter Nähe und Ablehnung aus der Ferne, monotone Revision und Ablehnung veralteter/wiederholter Befehle, bereits erreichter Zielzustand innerhalb einer sonst gültigen Aktion zulässig, kontrollierter Weltreset bei Änderungen der Autorendaten. Erster Test an der öffentlichen Inhaltsgrenze angelegt: `InteractiveObjectTests.Format160_IsExplicitlySupported` schlägt erwartungsgemäß fehl, weil 1.6.0 noch nicht unterstützt wird. Keine Produktimplementierung, vollständige Suite, Review oder Abnahme erfolgt. Nächster Schritt: neue Version mit vollständigem Interaktionsvertrag und Validierung implementieren, ohne alte Schemas zu ändern; anschließend Serverzustand und Browserbedienung ergänzen. Test ist derzeit absichtlich rot.

10.11.2026: ausschließlich angelegt und geplant; keine Implementierung oder Produktprüfung erfolgt. Erst Paketumfang und Testgrenzen bestätigen lassen.
