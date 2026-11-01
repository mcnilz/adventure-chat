# Spezifikation – begehbarer Adventure-Chat
MVP abgenommen; Erweiterungen 09–13 geplant.

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

## Out of Scope
Inventar, Gegenstandsaktionen, Dialogbäume, Skriptsprache, SCUMM-Dateiformate, Charaktereditor, Audio-/Videochat, öffentliche Registrierung, öffentliche Moderationsoberfläche, mobile Optimierung, Datenbank, persistente Chatarchive, verteilte Server, Hosting und GitHub-Veröffentlichung.

## Further Notes
Vorläufige Testzielgröße: zehn gleichzeitige Teilnehmer. Private Testumgebung ist keine Freigabe für öffentlichen Betrieb. Für öffentliche Nutzung später Zugang, Moderation, Datenschutz und Missbrauchsschutz konkret planen.

## Erweiterung: Weltpakete und Darstellung
Externe Weltpakete werden unabhängig von der Anwendung betrieben; eine mitgelieferte Referenzwelt demonstriert den Ablauf. Die Weltwahl und die Trennung von Teilnehmern, Ressourcen und Raumchat zwischen Welten folgen in Ticket 10. Laufender Zustand wird nicht persistiert. Tickets 11–13 ergänzen begehbare Draufsichtkarten, horizontales Scrollen und kontinuierliche Tiefenskalierung. Mitgelieferte Demo-Welten zeigen drei erreichbare Szenarien: Scrollen, Tiefe und die Kombination; mindestens ein scrollender Raum ist eine Karte. Details und Abnahme stehen in den Tickets 09–13.
