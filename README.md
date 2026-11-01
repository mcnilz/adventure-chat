# Adventure Chat – Planungspaket
Revision 1 · 28.10.2026 · Arbeitsname, endgültiger Name offen.

## Zweck
Dieses Paket übergibt die besprochene Produktidee an eine frische Codex-Sitzung. Es enthält keine Implementierung und keine installierten Skills. Anforderungen sind aus dem Gespräch abgeleitet; neue Detailentscheidungen sind ausdrücklich Vorschläge.

## Einstieg
1. [AGENTS.md](AGENTS.md): Arbeitsregeln und Lesereihenfolge.
2. [SPEC.md](SPEC.md): Umfang, User Stories und MVP-Abnahme.
3. [ARCHITECTURE.md](ARCHITECTURE.md): Technik und Zuständigkeiten.
4. [CONTENT-FORMAT.md](CONTENT-FORMAT.md): YAML und Bildressourcen.
5. [TESTING.md](TESTING.md): vorgeschlagene Testgrenzen.
6. [PLAN.md](PLAN.md): Abhängigkeiten und prüfbare Ergebnisse.
7. [SKILLS.md](SKILLS.md): Matt-Pocock-Workflow und Startprompts.
8. [active-state.md](active-state.md): aktueller Stand.
9. [GLOSSARY.md](GLOSSARY.md): gemeinsame Begriffe.

## Verwendung
Paket in einen eigenen Projektordner entpacken. Noch kein Projektname, Remote oder Repository vorgegeben. Existierende AGENTS.md beim Übernehmen zusammenführen, nicht überschreiben. Die lokalen Tickets unter [.scratch/adventure-chat/issues](.scratch/adventure-chat/issues/) gehören zu diesem dauerhaften Plan: nicht pauschal ignorieren oder löschen.

Alle Tickets sind Vorschläge, keine bereits abgenommenen Implementierungsaufträge. Zunächst Paket 01 mit vorgeschlagenen Testgrenzen bestätigen lassen. Danach jeweils ein Paket implementieren, prüfen und vorführen. Für spätere Sitzungen reichen AGENTS.md, active-state.md, das konkrete Ticket und dessen verlinkter Fachkontext.

## Lokaler Start und Entwicklung (PowerShell 7)

### Voraussetzungen
- .NET 10 SDK
- Node.js (>= 20) und npm

### 1. Client kompilieren
```powershell
cd src\AdventureChat.Client
npm install
npm run build
npm run typecheck
cd ..\..
```

### 2. Projekt bauen und automatisierte Tests ausführen
```powershell
# Vollständigen Build durchführen
dotnet build

# Browser-Verhaltenstests (Playwright / NUnit) ausführen
dotnet test
```

### 3. Server starten

#### A. Regulärer Spielmodus (Multiplayer-Chat)
Im regulären Spielmodus sind die Editor- und Autoren-APIs deaktiviert. Spieler können sich mit eigenem Namen in beliebige erstellte Räume einwählen, herumlaufen, chatten und Ausgänge benutzen.
```powershell
dotnet run --project src\AdventureChat.Server\AdventureChat.Server.csproj
```
Browser öffnen: `http://localhost:5000` (oder angezeigte Portadresse).
Mit URL-Parametern direkt einsteigen: `http://localhost:5000?name=Guybrush&room=lounge`

#### B. Autorenmodus (Integrierter Raum- & Welteditor)
Im Autorenmodus ist der Editor unter `/editor.html` aktiv. Räume können visuell modelliert, begehbare Flächen und Hindernisse gezeichnet, Spawns und Ausgänge platziert sowie Hintergrundbilder verwaltet und hochgeladen werden.
```powershell
dotnet run --project src\AdventureChat.Server\AdventureChat.Server.csproj -- --authoring
```
Editor im Browser aufrufen: `http://localhost:5000/editor.html`

### 4. Kurzanleitung: Räume verbinden und Welt nutzen
1. **Ersten Raum erstellen:** Im Editor `➕ Neu` anklicken, ID vergeben (z. B. `hub`), Bodenfläche aufziehen, Startpunkt (`📍 Spawn setzen`) mit ID `entry` platzieren.
2. **Zweiten Raum erstellen:** `➕ Neu` anklicken, ID vergeben (z. B. `dungeon`), Bodenfläche aufziehen, Startpunkt mit ID `entry` und einen Spawn mit ID `from-hub` platzieren.
3. **Ausgang verknüpfen:** Im ersten Raum `hub` das Werkzeug `🚪 Ausgang setzen` wählen, Zielraum `dungeon` und Ziel-Spawn `from-hub` im Dialog auswählen und auf der begehbaren Fläche platzieren.
4. **Welt validieren:** Button `🌐 Welt prüfen` anklicken, um alle raumübergreifenden Referenzen und Erreichbarkeiten automatisch zu verifizieren.
5. **Gemeinsam betreten:** Server im Spielmodus starten und mit mehreren Browserfenstern unter `http://localhost:5000?room=hub` gemeinsam den Chat-Raum betreten und durch den Ausgang wechseln.

## Festgelegt und noch offen

Planungsannahmen: private Tests statt öffentlichem Dienst, ungefähr zehn gleichzeitige Besucher, Desktopbrowser mit Maus, Punktfigur für Wegfindung ohne Spielerkollision, flüchtige Sitzungen und Nachrichten. Diese Annahmen werden vor dem jeweiligen betroffenen Paket bestätigt. GitHub-Veröffentlichung ist eine spätere Option.

