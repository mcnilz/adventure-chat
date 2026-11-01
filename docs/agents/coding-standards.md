# Coding Standards & Clean Code für AI-Agenten und Entwickler

## 1. Grundprinzipien
Dieses Projekt wird kooperativ mit autonomen AI-Agenten (Codex / Junie) und menschlichen Entwicklern gepflegt. Um maximale Wartbarkeit, Stabilität und Kontext-Effizienz für LLMs zu gewährleisten, gelten folgende Clean-Code-Regeln.

### 1.1 Modulare Gliederung & Single Responsibility (SRP)
- Jedes Modul / jede Datei hat **genau eine fachliche oder technische Verantwortung**.
- **Dateigrößen begrenzen:** Dateien sollten idealerweise unter **200–300 Zeilen** bleiben. Große "God Objects" erschweren agentengestützte Reviews und führen zu Kontextüberlastung.
- Klare Trennung der Schichten:
  - `types.ts` / `constants.ts`: Reine Typdefinitionen und Konfigurationswerte.
  - `math.ts`: Reine Geometrie- und Rechenfunktionen ohne Seiteneffekte und ohne DOM/Netzwerk-Abhängigkeit.
  - `simulation/`: Bewegung, Interpolation und Zustandsautomaten (testbar ohne Canvas).
  - `state/`: Entitätenverwaltung (`RoomState`, Spieler-Maps, Snapshot-Anwendung).
  - `network/`: SignalR-Verbindungsmanagement, RPC-Invokes und Fallback-HTTP-Endpunkte.
  - `input/`: Koordinaten-Transformationen (`screenToLogical`), Hit-Testing und Mausevents.
  - `rendering/`: Isolierte Render-Ebenen (Hintergrund, Spielfiguren, Sprechblasen, Overlays/Marker).
  - `ui/`: HTML/DOM-Steuerung (Chatleiste, Aktionszeile, XSS-Schutz).
  - `game.ts`: Orchestrator / Fassade (`AdventureGame`), die Bausteine verbindet und den Gameloop steuert.

### 1.2 Strikte Typisierung & Schnittstellentreue
- TypeScript `strict: true` ist zwingend.
- Keine unsicheren `any`-Typen im Anwendungs- und Domänencode.
- DTO-Typen im Client (`types.ts`) müssen exakt mit den C#-Records des Servers übereinstimmen.
- Unveränderliche Daten (`readonly` Properties auf DTOs und Points) bevorzugen.

### 1.3 Entkopplung & Testbarkeit
- **Reine Funktionen bevorzugen:** Berechnungen (z. B. Kollisionen, Wegpunkte, Richtungsbestimmung) als seiteneffektfreie Funktionen implementieren.
- **Renderer sind passiv:** Render-Klassen zeichnen den übergebenen Zustand; sie modifizieren weder Spielerpositionen noch Game-State.
- **Sichere DOM-Manipulation:** Benutzereingaben und Chatnachrichten werden ausschließlich über `textContent` oder sichere DOM-Knoten eingefügt (kein `innerHTML` mit Fremddaten).

---

## 2. Richtlinien für AI-Agenten

### 2.1 Kontextschonendes und zielgerichtetes Arbeiten
- Vor Änderungen die relevanten Teilmodule öffnen, nicht das gesamte Projekt auf einmal laden.
- Neue Features nicht in bestehende Orchestratoren oder Fassaden hineinstopfen, sondern zuerst das passende Submodul identifizieren oder neu anlegen.
- Öffentliche Schnittstellen und E2E-Hooks (wie `window.__ADVENTURE_GAME__` und Canvas-Events) stabil halten, damit bestehende Playwright- und Integrationstests intakt bleiben.

### 2.2 Keine Seiteneffekte beim Import
- Module dürfen beim bloßen `import` keine DOM-Zugriffe, Netzwerkverbindungen oder Timer starten.
- Bootstrap und Initialisierung erfolgen explizit in `main.ts` oder über Konstruktoren.

### 2.3 Verifikations-Kette
Nach jeder Code-Anpassung ist die Verifikationskette zwingend auszuführen:
1. `npm --prefix src/AdventureChat.Client run typecheck` (tsc ohne Fehler)
2. `npm --prefix src/AdventureChat.Client run build` (Build & Asset-Kopie)
3. `dotnet test` (Alle NUnit- und Playwright-Tests müssen grün sein)
