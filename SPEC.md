# Spezifikation – begehbarer Adventure-Chat
MVP abgenommen; tatsächlicher Umsetzungsstand in active-state.md. Interaktive Raumobjekte als Erweiterung in Tickets 20–23 geplant (10.11.2026); keine Implementierungsfreigabe.

## Problem Statement
Ein gewöhnliches Chatfenster vermittelt keinen gemeinsamen Ort. Der Benutzer möchte sich mit anderen Personen in einer Welt im Stil klassischer Lucasfilm-Adventures treffen, mit Figuren bewegen und schreiben können.

## Solution
Ein Browser zeigt einen zweidimensionalen Raum mit Hintergrundbild und animierten Figuren. Ein Klick bewegt die eigene Figur innerhalb begehbarer Flächen. Andere Teilnehmer sehen diese Bewegung. Ein Raumchat mit dauerhaft sichtbarem Verlauf innerhalb der Sitzung und kurzlebigen Sprechblasen unterstützt Gespräche. Türen verbinden Räume. Ein lokaler Browsereditor erzeugt die Raumdaten.

## User Stories
1. Als Besucher möchte ich mit einem Anzeigenamen eintreten, damit andere mich erkennen.
2. Als Besucher möchte ich meine Figur eindeutig erkennen.
3. Als Besucher möchte ich durch Klicken ein Ziel setzen.
4. Als Besucher möchte ich Hindernisse umgehen, damit die Figur im begehbaren Bereich bleibt.
5. Als Besucher möchte ich während des Laufens ein neues Ziel wählen.
6. Als Besucher möchte ich andere Teilnehmer und deren Namen sehen.
7. Als Besucher möchte ich Raumnachrichten schreiben und lesen.
8. Als Besucher möchte ich erkennen, welche Figur etwas sagt.
9. Als Besucher möchte ich eine Tür benutzen und im Zielraum erscheinen.
10. Als Besucher möchte ich Verbindungsverlust erkennen und anschließend einen konsistenten Raum sehen.
11. Als Autor möchte ich ein PNG-Hintergrundbild importieren.
12. Als Autor möchte ich begehbare Flächen mit der Maus zeichnen und ändern.
13. Als Autor möchte ich Startpunkte und Raumübergänge platzieren.
14. Als Autor möchte ich Änderungen speichern und nach Neustart wieder öffnen.
15. Als Autor möchte ich einen Raum direkt ausprobieren.
16. Als Autor möchte ich Raumdateien lesbar bearbeiten und mit Schema-Unterstützung prüfen.
17. Als Entwickler möchte ich mit dokumentierten lokalen Befehlen bauen und testen können.

## Implementation Decisions
Verbindliche Richtung: TypeScript, .NET-Backend, Browsereditor, YAML-Authoring, einfache PNG-Ressourcen, minimale Abhängigkeiten. SCUMM dient als konzeptionelles Vorbild.
Vorgeschlagen: Canvas 2D und SignalR, ein Serverprozess, lokales Dateisystem für Inhalte, keine Datenbank.
Stabile Raum-, Spawn-, Ausgangs- und Sitzungs-IDs sowie Trennung von Raumdefinition und Laufzeitstatus gehören bereits in den MVP. Keine gemeinsame C#-/TypeScript-Domänenbibliothek erzwingen.

## Testing Decisions
Beobachtbares Verhalten statt interner Methoden prüfen. TESTING.md schlägt drei öffentliche Grenzen vor, die vor Umsetzung des betreffenden Pakets bestätigt werden müssen. Bestehende Tests und tatsächliche Nachweise stehen in den Tickets und active-state.md.

## MVP-Abnahme
- [ ] Autor erstellt zwei Räume im Editor, speichert und öffnet sie nach Neustart.
- [ ] Zwei voneinander unabhängige Browserkontexte betreten denselben Raum.
- [ ] Eigene und fremde Figuren sind unterscheidbar; Bewegung ist sichtbar animiert.
- [ ] Wegfindung bleibt in erlaubten Flächen; unerreichbare Ziele teleportieren niemanden.
- [ ] Beide Teilnehmer sehen Raumnachrichten im Verlauf und kurz als Sprechblase.
- [ ] Raumwechsel aktualisiert Teilnehmerlisten und Nachrichtenempfänger korrekt.
- [ ] Verbindungsverlust wird angezeigt; Wiederverbindung erzeugt keine verwaisten Doppelgänger.
- [ ] Client kann keine Positionen, Absender oder fremden Raumzugehörigkeiten erzwingen.
- [ ] Projekt lässt sich lokal mit dokumentierten Voraussetzungen starten.
- [ ] Keine Node-Laufzeit auf dem Anwendungsserver erforderlich.

## Out of Scope des ursprünglichen MVP
Inventar, Gegenstandsaktionen, Dialogbäume, Skriptsprache, SCUMM-Dateiformate, Charaktereditor, Audio-/Videochat, öffentliche Registrierung, öffentliche Moderationsoberfläche, mobile Optimierung, Datenbank, persistente Chatarchive, verteilte Server, Hosting und GitHub-Veröffentlichung. Die unten bestätigte Erweiterung nimmt ausschließlich die dort beschriebenen Gegenstandsaktionen hinzu; Inventar, Aufheben, Dialogsystem und Skriptsprache bleiben ausgeschlossen.

## Further Notes
Vorläufige Testzielgröße: zehn gleichzeitige Teilnehmer. Private Testumgebung ist keine Freigabe für öffentlichen Betrieb. Für öffentliche Nutzung später Zugang, Moderation, Datenschutz und Missbrauchsschutz konkret planen.

## Erweiterung: Weltpakete und Darstellung
Externe Weltpakete werden unabhängig von der Anwendung betrieben; eine mitgelieferte Referenzwelt demonstriert den Ablauf. Die Weltwahl und die Trennung von Teilnehmern, Ressourcen und Raumchat zwischen Welten folgen in Ticket 10. Laufender Zustand wird nicht persistiert. Tickets 11–13 ergänzen begehbare Draufsichtkarten, horizontales Scrollen und kontinuierliche Tiefenskalierung. Mitgelieferte Demo-Welten zeigen drei erreichbare Szenarien: Scrollen, Tiefe und die Kombination; mindestens ein scrollender Raum ist eine Karte. Details und Abnahme stehen in den Tickets 09–13.

## Bestätigte Erweiterung: interaktive Raumobjekte (20–23)

Planungsauftrag vom 10.11.2026 aus dem Sprachgespräch: „Ich will das Projekt weiter planen. Ich möchte jetzt neue Features und dazu die Tasks“, nach Konkretisierung „Das passt erstmal so leg das mal an“. Bestätigt sind der folgende Funktionsumfang und dessen Ticketplanung, keine Implementierung und keine Abnahme bestehender Pakete. Tickets 18/19 bleiben ready-for-human.

Nachtrag 12.11.2026: Paket 20 und seine Detailentscheidungen/Testgrenzen wurden separat bestätigt; Implementierungs- und Abnahmestand stehen im Ticket. Die Freigabe umfasst keine Abnahme von 18 oder 19 und keine Implementierung der folgenden Pakete.

Als Spieler möchte ich Raumobjekte gemeinsam mit anderen verändern und deren Änderungen mit einer erlaubten Gegenaktion rückgängig machen. Autorendaten definieren endliche Anfangszustände, zulässige Aktionen und direkte Auswirkungen; keine Skriptsprache oder VM. Alle Spieler derselben Welt erhalten denselben serverautoritativen Zustand, auch bei späterem Eintritt. Gleichzeitige Aktionen haben eine eindeutige serverseitige Reihenfolge. Der Zustand bleibt bis zum Neustart der Welt erhalten; Neustart auf Anfangszustände ist ausdrücklich akzeptiert, zusätzliche Zustandspersistenz nicht erforderlich.

Zum Einstieg gehören Öffnen/Schließen und Ziehen/Drücken. Ein Stuhl hat genau zwei konfigurierte Positionen „am Tisch“ und „zurückgeschoben“. Direkte ausdrücklich konfigurierte Verknüpfungen erlauben etwa, dass eine Schalteraktion den Schalter und ein anderes Objekt wie ein Tor zusammen und konsistent ändert. Keine automatischen Ereignisketten, freie Möbelphysik, Inventargegenstände, Aufheben oder Dialoge.

Hindernisflächen und Ausgangssperren folgen dem Zustand: Der Stuhl kann ständig blockieren und seine Fläche mitbewegen, die offene Tür gibt den Durchgang frei. Wegfindung berücksichtigt Änderungen einschließlich laufender Routen. Bestätigte konservative Richtung: Eine Änderung verständlich ablehnen, wenn sie eine Figur überdeckt oder einschließt. Die endliche technische Definition, Randtoleranzen und Testgrenzen werden vor Ticket 21 bestätigt; dessen lokaler Sicherheitsanker-Vorschlag ist keine Zusage globaler Rätsellösbarkeit.

Eine klassische LucasArts-artige Verbenleiste mit Vorbild links unten ergänzt die bestehende Aktions-/Statuszeile. Verb und Objekt bilden einen Satz, beispielsweise „Öffne Tür“. Autoren konfigurieren Zustände, Aktionen, direkte Objektverknüpfungen und Hindernisse im Browsereditor; die gemeinsame Vorschau verwendet dieselben Regeln und dieselbe Darstellung. Eine Handvoll nutzbarer Referenzobjekte demonstriert Tür, Stuhl und Schalter mit verbundenem Tor.

Die abnehmbaren Pakete und ihre noch zu bestätigenden Detailentscheidungen/Testgrenzen sind:

1. [20: Gemeinsame Zustände, Aktionen und direkte Verknüpfungen](.scratch/adventure-chat/issues/20-shared-object-actions.md).
2. [21: Zustandsabhängige Hindernisse und sichere Durchgänge](.scratch/adventure-chat/issues/21-state-dependent-navigation.md).
3. [22: Verbenbedienung und Browsereditor](.scratch/adventure-chat/issues/22-verbs-and-interaction-editor.md).
4. [23: Interaktive Referenzbeispiele](.scratch/adventure-chat/issues/23-interactive-reference-examples.md).

Der aktuelle veröffentlichte Formatstand ist 1.5.0. Geplant sind optionale Minor-Erweiterungen 1.6.0 in 20 und bei nachfolgender separater Veröffentlichung 1.7.0 in 21; genaue Verträge/Versionsnummern vor Umsetzung gegen den Iststand prüfen. Alte grafische Objekte bleiben ohne neue Angaben rein grafisch, alte Navigation unverändert. Veröffentlichte Schemas einschließlich Teilschemas sowie ältere Referenzfixtures bleiben unverändert; Server akzeptieren nur ausdrücklich unterstützte Versionen. Details: [ADR 0002](docs/adr/0002-declarative-object-interactions.md) und [Kompatibilitätsvertrag](docs/work-packages/world-format-compatibility.md).

Dieser Planungsauftrag verändert keine Produktdateien, Weltinhalte oder Bilder, führt keine neuen Abhängigkeiten ein und autorisiert keine Bilderzeugung, Serveränderung oder Implementierungscommits. Umsetzung erst nach separater Freigabe des jeweiligen Pakets gemäß PLAN.md.
