# ADR 0002 – Deklarative gemeinsame Objektaktionen ohne Ereignisketten

Datum: 10.11.2026. Status: fachliche Richtung zur Planung bestätigt; Datenfelder, Sicherheitsdefinition und Testgrenzen vor den einzelnen Implementierungspaketen bestätigen. Keine Implementierungsfreigabe.

Nachtrag 12.11.2026: Paket 20 einschließlich Datenvertrag, Bedienpunkt, Revisionen und Testgrenzen bestätigt; Umsetzung und tatsächlicher Prüfstand stehen in Ticket 20. Die Sicherheitsdefinition für Paket 21 bleibt separat zu bestätigen.

## Entscheidung und Grund

Interaktive Raumobjekte erhalten endliche deklarierte Zustände und explizit erlaubte Aktionen. Eine Aktion setzt ihren eigenen und gegebenenfalls direkt benannte andere Objektzustände gemeinsam oder gar nicht. Zustandsänderungen lösen keine weiteren Aktionen aus. Dies ermöglicht reversible Tür-/Stuhlaktionen und Schalter-/Torverknüpfungen, ohne einen Skriptinterpreter, eine VM oder einen generischen Ereignisbus einzuführen. Die Grenze verhindert, dass ein scheinbar einfacher Schalter unbegrenzte automatische Ketten und deren Reihenfolge-/Abbruchprobleme in das Autorenformat einführt.

Der Server bestimmt den flüchtigen Zustand pro Weltinstanz und ordnet konkurrierende Aktionen eindeutig. Eintritt und Resynchronisierung erhalten den aktuellen Zustand, auch nach leerem Raum; Weltneustart lädt wieder die deklarierten Anfangszustände. Jeder Spieler kann erlaubte Gegenaktionen ausführen. Dauerhafte Speicherung und Besitz-/Undo-Historien sind hierfür nicht erforderlich und nicht beauftragt.

## Folgen und Grenzen

- Zustandsdarstellung, feste Positionen und Navigation bleiben ausdrücklich konfigurierte Angaben. Der Stuhl hat zwei Endpositionen, kein kontinuierliches Physikmodell. PNG-Alpha erzeugt keine Kollision. Gemeinsamer Renderer und gemeinsame autoritative Vorschau bleiben erhalten.
- Ein Zustandswechsel einschließlich direkter Verknüpfungen wird auf die resultierende Geometrie aller betroffenen Räume geprüft. Die bestätigte Richtung ist vollständige Ablehnung mit verständlicher Meldung bei Überdecken/Einschließen einer Figur. Die begrenzte Fußpunkt-/Sicherheitsankerregel in [Ticket 21](../../.scratch/adventure-chat/issues/21-state-dependent-navigation.md) ist noch ein Vorschlag; globale Erreichbarkeit der Welt und Lösbarkeit zukünftiger Aktionsfolgen werden nicht zugesichert. Keine zweite abweichende Browser-Wegfindung.
- Keine Inventargegenstände, kein Aufheben, Dialogsystem, frei formulierte Bedingungen oder automatische Aktionsketten. Klassische Verbenleiste und Objektsatz sind Bedienung für die erlaubten Aktionen, keine allgemeine Adventure-Engine.
- Formaterweiterungen sind optional: ältere grafische Objekte bleiben grafisch und alte Navigation bleibt unverändert. Auf Basis 1.5.0 ist für [20](../../.scratch/adventure-chat/issues/20-shared-object-actions.md) 1.6.0 geplant; die separat veröffentlichte Navigation aus 21 benötigt anschließend 1.7.0. Minor gilt nur bei erhaltenen Defaults und vollständiger alter Semantik. Kein bereits veröffentlichtes Schema/Teilschema wird ergänzt. Exakte Servermatrix und unveränderte ältere Fixtures weisen Unterstützung nach.

## Abgewogen

Skripte oder automatische Ereignisketten würden mehr Ausdruckskraft bieten, bringen aber Ausführungsgrenzen, Fehlerbehandlung und wesentlich komplexere Mehrspielerreihenfolge mit. Freie Möbelphysik erfordert kontinuierliche Kollisionsregeln statt zwei klarer Endzustände. Persistenz würde zusätzliche Lebenszyklus-/Migrationspflichten erzeugen; der ausdrücklich akzeptierte Reset beim Weltneustart vermeidet sie. Diese Optionen sind kein Teil des bestätigten Einstiegs.

Produktumfang und Auftrag stehen in [SPEC](../../SPEC.md), Reihenfolge und Status in [PLAN](../../PLAN.md); Einzelentscheidungen und Testgrenzen in Tickets 20–23. Es wird weder die Abnahme von 18/19 noch die Umsetzung der Erweiterung behauptet.
