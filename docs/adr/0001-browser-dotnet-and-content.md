# ADR 0001 – Browserclient, .NET-Server und datenbasierte Räume
Datum: 28.10.2026.
Status: Produktrichtung bestätigt; konkrete technische Ausgestaltung vorgeschlagen.

## Kontext
Gewünscht ist ein begehbarer Chat mit Adventure-Anmutung im Browser und geringer npm-Abhängigkeitslast. Räume sollen einfach erstellt werden können.

## Entscheidung
TypeScript im Browser und .NET im Backend. Neue Implementierung statt SCUMM-Kompatibilität. YAML mit Schema für Autorendaten; PNG für Bilder. Raumeditor läuft ebenfalls im Browser und nutzt die Darstellung des Clients.
Canvas 2D, SignalR und serverseitiges Laden/Prüfen von YAML sind die technischen Startvorschläge. Einzelner Serverprozess, keine Datenbank im MVP.

## Folgen
Client und Editor teilen Koordinaten und Darstellung. Kein YAML-Parser im Browser nötig. Netzwerk-, Raum- und Inhaltsregeln bleiben in eigener Verantwortung. Build-/Testwerkzeuge können Node benötigen; der ausgelieferte Anwendungsserver benötigt es nicht.
Keine Vorbereitung eines allgemeinen Skript- oder Pluginsystems. Spätere Funktionen sollen auf stabilen IDs und getrennter Definition/Laufzeit aufbauen.

## Abgewogen
SCUMM-Kompatibilität bringt Aufwand für fremde Datenformate, ohne Multiplayer zu lösen. Desktopeditor benötigt eine weitere UI-Plattform. Vollständige Spieleengine wird erst bei nachgewiesenem Bedarf geprüft.

