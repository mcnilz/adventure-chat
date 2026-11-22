# 24 – Epic: Interaktive Welten mit Figuren und NPCs

**What to build:** Autoren können sichtbare Objektübergänge, Figurenanimationen, NPC-Dialoge, NPC-Wege und freiwillige Führungen für ihre Welten konfigurieren. Spielerfiguren und NPCs bewegen beim Sprechen den Mund. NPCs können dabei gestikulieren. Jede umgesetzte Funktion lässt sich in der Referenzwelt ausprobieren, einschließlich einer eigens gestalteten NPC-Figur.

**Blocked by:** Keine für die Planung. Umsetzung und Abnahme erfolgen über die verlinkten Pakete mit ihren jeweiligen Blockern.

**Status:** draft

## Arbeitspakete

| Ticket | Vorführbares Ergebnis |
| --- | --- |
| [25 – Sichtbare Objektübergänge](25-object-state-transitions.md) | Eine Objektaktion spielt einen konfigurierten sichtbaren Übergang zwischen Zuständen ab. |
| [26 – Figurenanimationen bei Objektaktionen](26-character-animations-for-object-actions.md) | Je nach Aktion zeigt die Figur am Bedienpunkt eine kurze Animation oder eine gehaltene Pose. |
| [27 – NPCs ansprechen](27-npc-dialogue.md) | „Spreche mit“ öffnet bei einer benannten computergesteuerten Figur Antwortmöglichkeiten mit vorbereiteten Repliken. |
| [28 – Zustandsabhängige Dialoge](28-state-dependent-dialogue.md) | Ein vorhandener Objektzustand oder ein vorgesehener Fortschritt ändert Fragen und Antworten. |
| [29 – NPC-Wege und Aufenthaltsbereiche](29-npc-movement.md) | Eine Figur geht definierte Wegpunkte ab. Ein optionaler Aufenthaltsbereich ermöglicht abwechslungsreiche Wege. |
| [30 – Freiwillig einem NPC folgen](30-follow-npc.md) | Nach Zustimmung begleitet der Spieler einen NPC durch mehrere Räume, erhält Erläuterungen und kann jederzeit selbst weitergehen. |
| [31 – Kombinierte Referenzszene](31-combined-reference-scenario.md) | Ein zusammenhängender Ablauf verbindet Objektaktionen, NPC-Dialog und Führung. |
| [32 – Gezielte NPC-Reaktion auf Objektaktionen](32-object-triggered-npc-reactions.md) | Optional kann eine konfigurierte Objektaktion einen beliebigen dafür definierten NPC zu einem angegebenen Ziel rufen. |
| [33 – Sprechende Figuren, NPC-Gesten und eine eigene NPC-Grafik](33-speaking-animations-and-npc-art.md) | Spielerfiguren und NPCs sprechen sichtbar. Ein NPC mit eigener Grafik begleitet eine Antwort mit einer Handgeste. |

## Acceptance criteria

- [ ] Die Funktionstickets spezifizieren konfigurierbare Fähigkeiten für verschiedene Objekte und Welten. Schreibtisch, Sitzplatz, Tafel und Klingel sind Beispiele für die Abnahme. Die Führung beginnt freiwillig und lässt sich unterwegs jederzeit beenden.
- [ ] Mindestens ein Wechsel eines schon vorhandenen Objektzustands beeinflusst ein Gespräch sichtbar. Der Umfang eigenen Dialogfortschritts wird separat entschieden.
- [ ] Bei sichtbarer Sprechblase bewegen Spielerfiguren und NPCs den Mund, soweit das Gesicht zu sehen ist. Eine NPC-Antwort zeigt eine Geste. Die neue NPC-Figur der Referenzwelt ist im Stil des Design Guides gestaltet und von den Spielerfiguren klar zu unterscheiden.
- [ ] Autoren können Beispiele über den vorhandenen Inhaltsweg anlegen und in einer von der Live-Welt isolierten Vorschau prüfen. Die Reihenfolge für YAML und Browsereditor wird pro Paket festgelegt.
- [ ] Jede umgesetzte Funktion hat im selben Paket ein auffindbares Beispiel in der Referenzwelt. Bestehende Räume und kurze Wege ab Einstieg haben Vorrang. Falls separate Räume sinnvoll sind, kommt ein zentraler Startraum mit direkten Zugängen infrage. Der kombinierte Ablauf ist separat abnehmbar.
- [ ] Eigene Checks und menschliche Abnahmen sind je Paket nachvollziehbar. Ältere Weltformate werden gegen unveränderte Referenzfixtures geprüft, sobald der Autorenvertrag erweitert wird.

## Umfang und Reihenfolge

25–27 sind getrennte Funktionen auf Basis der Oberfläche und des Autorenwerkzeugs aus 22. 28 und 29 vertiefen die NPC-Grundfunktion. 31 bündelt die Beispiele zu einem Ablauf. 32 ist eine optionale Vertiefung und kann in die kombinierte Szene aufgenommen werden. 33 ergänzt die sichtbare Darstellung von Raumchat und Dialog vor der Gesamtabnahme des Epics. Umsetzung und Testgrenzen werden pro Paket bestätigt.

Vorschlag für den Einstieg sind sichtbare Übergänge zwischen festen Zuständen, einfache Figurenposen, kurze Gesten und Autorendialoge mit Auswahl. Unterbrechungen, Mehrspieler-Sichtbarkeit und der Geltungsbereich von Dialogfortschritt werden je Paket entschieden. [SPEC](../../../SPEC.md) hält den Produktumfang fest. [ADR 0002](../../../docs/adr/0002-declarative-object-interactions.md) beschreibt den bestehenden Objektvertrag.
