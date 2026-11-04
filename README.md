# Adventure Chat
Arbeitsname, endgültiger Name offen.

## Zweck
Ein lokaler Multiplayer-Chat mit begehbaren Räumen und einem Browser-Raumeditor. Die aktuelle Projektübergabe steht in [active-state.md](active-state.md).

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
Die lokalen Tickets unter [.scratch/adventure-chat/issues](.scratch/adventure-chat/issues/) dokumentieren Pakete und Abnahmen.

## Lokaler Start und Entwicklung (PowerShell 7)

### Voraussetzungen
- .NET 10 SDK
- Node.js (>= 20) und npm

### 1. Client kompilieren
```powershell
cd src\AdventureChat.Client
npm ci --ignore-scripts
npm run build
npm run typecheck
cd ..\..
```

### 2. Projekt bauen und automatisierte Tests ausführen
```powershell
# Abhängigkeiten anhand der Lockfiles wiederherstellen und bauen
dotnet restore --locked-mode
dotnet build --no-restore

# Einmalig den zur Playwright-Version passenden Chromium installieren
pwsh tests\AdventureChat.E2ETests\bin\Debug\net10.0\playwright.ps1 install chromium

# Browser-Verhaltenstests (Playwright / NUnit) ausführen
dotnet test --no-build --no-restore
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


## Externes Weltpaket (Ticket 09)

Die Referenzwelt kann unabhängig vom Anwendungsverzeichnis kopiert werden:

```powershell
Copy-Item -Recurse worlds\reference C:\AdventureWorld
# Als Spielserver; für den lokalen Editor zusätzlich --authoring angeben
dotnet run --project src\AdventureChat.Server -- --World:Root C:\AdventureWorld --authoring
```

`world.yaml` bestimmt Welt-ID, Namen, Format- und Inhaltsversion sowie Start-Raum und Start-Spawn. `rooms/*.yaml` enthält das ausdrücklich zugeordnete Legacy-Raumformat `version: 1`; Bilder liegen unter `assets/images`. Der Server prüft alle Dateien beim Start und bricht bei Fehlern ab. Im Spiel erfolgt der Eintritt stets am festgelegten Startpunkt, auch nach Neustart oder Reconnect. Der Editor ist nur im lokalen Autorenmodus verfügbar und speichert im Weltordner. Laufender Spielzustand bleibt flüchtig.

Alternativ lassen sich mehrere Pfade als `--World:Roots:0 <Pfad> --World:Roots:1 <Pfad>` konfigurieren. Alle werden geprüft, doppelte Welt-IDs abgewiesen. `World:Root` steht, falls zusätzlich angegeben, an erster Stelle. Ohne Weltkonfiguration bleibt der bisherige lokale Betrieb mit URL-Raumwahl erhalten.

Bei konfigurierten Weltpaketen erscheint im Spiel und im lokalen Editor zuerst die Weltwahl, auch bei nur einer Welt. Im Spiel führt „Zur Weltwahl“ aus der Sitzung zurück; anschließend lässt sich eine andere Welt betreten. Nach einem Verbindungsabbruch erfolgt der frische Eintritt in die zuletzt gewählte Welt am Start-Spawn. Ist diese Welt nicht mehr verfügbar, erscheint die Auswahl erneut. Bei Verbindungsverlust werden in diesem Modus keine lokalen Ersatzbewegungen berechnet.

Im Editor merkt die Seitenadresse die gewählte Welt als `?worldId=...`. Laden, Speichern, Validierung, Bilder und Vorschau beziehen sich auf diese Welt. „Zur Weltwahl“ wechselt die Welt über einen Seitenwechsel; bei ungespeicherten Änderungen warnt der Browser. Weltpakete werden weiterhin ausschließlich über Serverkonfiguration eingebunden; die Browseradresse enthält keine Verzeichnispfade.

Die öffentlichen Raumdaten liegen unter `/api/worlds/{worldId}/rooms/{roomId}`, Bilder unter `/world-assets/{worldId}/images/...`. Die bisherigen `/api/rooms/...`-Routen bleiben Zugänge zur ersten Welt beziehungsweise zum Betrieb ohne Weltpaket; der Mehrweltclient verwendet für Bewegung ausschließlich die an seine Sitzung gebundenen Hubbefehle. Autorenabfragen über `/api/editor/...` tragen `worldId` als Abfrageparameter; unbekannte Welt-IDs liefern 404. Die Autoren-Zugriffsbeschränkungen gelten unverändert.

Für YAML-Editoren das mit der Anwendung ausgelieferte `schemas/world/1.0.0/room.schema.json` lokal den Dateien `rooms/*.yaml` und `world.schema.json` dem Manifest zuordnen. Der Browsereditor schreibt beim Speichern eine lokale relative Schema-Zuordnung; Schema-Kommentare sind kein Laufzeitvertrauen. Bei übernommenen Dateien mit älteren Schema-Kommentaren kann die Zuordnung im Texteditor angepasst werden, ohne Raumdaten zu ändern. Die unveränderten Dateien aus `content/rooms` lassen sich durch Kopieren nach `rooms/` im Paket mit `roomFormat: legacy-1` verwenden, sofern die zugehörigen Bilder ebenfalls im Paket liegen.

Beispiel für VS Code mit installierter YAML-Erweiterung: Den externen Weltordner als Arbeitsordner öffnen und in dessen lokalen Einstellungen die beiden absoluten Schema-Dateipfade auf die eigene Installation anpassen:

```json
{
  "yaml.schemas": {
    "file:///D:/adventure-chat/src/AdventureChat.Server/bin/Debug/net10.0/schemas/world/1.0.0/world.schema.json": "world.yaml",
    "file:///D:/adventure-chat/src/AdventureChat.Server/bin/Debug/net10.0/schemas/world/1.0.0/room.schema.json": "rooms/*.yaml"
  }
}
```

Die Pfade sind Beispiele und müssen auf das lokal vorhandene Schema-Bündel zeigen. Die Referenzdateien enthalten bewusst keine vorrangigen Schema-Kommentare. Unveränderte alte Raumdateien können einen früheren Kommentar enthalten; für eine eindeutige Zuordnung in einem Texteditor gegebenenfalls dessen Schema-Pfad im Kommentar korrigieren. Das ändert weder Raumdaten noch die beim Server maßgebliche Legacy-Zuordnung.
