# 03: Zwei Browser sehen einander laufen

**What to build:** Zwei voneinander isolierte Browser betreten denselben Raum mit Anzeigenamen und sehen Eintritt, Bewegung und Verlassen der anderen Figur.

**Blocked by:** 02

**Status:** done

## Acceptance criteria

- [x] Zwei getrennte Browserkontexte sehen jeweils genau zwei eindeutig benannte Figuren.
- [x] Bewegung eines Teilnehmers erscheint flüssig im anderen Browser.
- [x] Ein später eintretender Teilnehmer erhält auch bereits laufende Bewegungen korrekt.
- [x] Verlassen oder Verbindungsabbruch entfernt die betreffende Figur.
- [x] Verbindungsverlust ist sichtbar; nach erneutem Eintritt bleibt kein alter Doppelgänger zurück.
- [x] Ein Client kann weder fremde Figur-ID noch fremde Sitzung übernehmen.

## Testgrenzen – vor Beginn bestätigen
Browserbedienung und öffentliche Echtzeitkommandos.

## Hinweise
Vor Beginn Reconnect als frischen Eintritt und private Testzielgröße bestätigen; keine persistente Identität einführen.
Gemeinsamer Fachkontext: [Spezifikation](../../../SPEC.md), [Architektur](../../../ARCHITECTURE.md), [Tests](../../../TESTING.md), [Autorenformat](../../../CONTENT-FORMAT.md). Root-AGENTS.md gilt.

## Abschluss
- **Umsetzung:**
  - ASP.NET Core SignalR-Hub `RoomHub` unter `/hub/room` implementiert.
  - `IRoomSessionManager` und `RoomSessionManager` für die Verwaltung von Spielersitzungen, Verbindungsaufbau, Reconnect, Snapshot-Erstellung und servervalidierte Echtzeit-Wegfindung umgesetzt.
  - `@microsoft/signalr@9.0.6` (MIT-Lizenz) im Client eingebunden und über Build-Skript bereitgestellt.
  - `RoomRenderer` und `AdventureGame` um Multi-User-Rendering mit Y-Tiefensortierung, individuelle Namensschilder über den Charakteren und synchrone Wegpunkt-Animation erweitert.
  - Optimierung der Wegfindung: Corner-Clearance (14px Ecksicherheitsabstand) und dynamische Übergabe der aktuellen Spielerposition bei Folgeklicks im Lauf für unterbrechungsfreie Bewegungskorrekturen.
- **Checks:**
  - `npm run typecheck` (`tsc --noEmit`): 0 Fehler.
  - `npm run build`: 0 Fehler.
  - `dotnet build`: 0 Fehler, 0 Warnungen.
  - `AdventureChat.ServerTests`: 27 Tests erfolgreich (Sitzungsmanagement, Reconnect, Namensbereinigung, Wegfindung mit Ecksicherheit, Snapshot).
  - `AdventureChat.E2ETests`: 14 Playwright-E2E-Tests erfolgreich (Multi-Browser-Sichtbarkeit, Echtzeit-Bewegungssynchronisation, Late-Join mit laufender Bewegung, Disconnect und Reconnect ohne Geisterfiguren).
- **Abhängigkeitsbegründung `@microsoft/signalr`:**
  - *Nutzen:* Offizieller WebSocket/SignalR-Client für ASP.NET Core zur Echtzeitsynchronisation.
  - *Alternative:* Manuelle WebSocket-Implementierung (erheblich fehleranfälliger für Reconnect- und Hub-Protokoll).
  - *Lizenz:* MIT.
  - *Wartung:* Microsoft ASP.NET Core Team.
  - *Transitive Abhängigkeiten:* Im Browser 0 Laufzeit-Abhängigkeiten (nutzt native `WebSocket`- und `fetch`-APIs).
- **Menschliche Abnahme:** Erfolgreich bestätigt.

