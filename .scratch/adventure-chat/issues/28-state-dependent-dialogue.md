# 28 – Gesprächsoptionen und Antworten an Zustände binden

**What to build:** Autoren können die Verfügbarkeit von Gesprächsoptionen und Antworten an vorhandene Objektzustände binden. Eigener Gesprächsfortschritt bleibt als separat zu entscheidende Vertiefung vorgesehen.

**Blocked by:** [20](20-shared-object-actions.md), [27](27-npc-dialogue.md).

**Status:** ready-for-human

## Acceptance criteria

- [x] Der Autor bindet eine Option oder Antwort an einen benannten Zustand eines vorhandenen Raumobjekts. Vor und nach dem Zustandswechsel sind die beabsichtigten unterschiedlichen Auswahlmöglichkeiten und Antworten sichtbar.
- [x] Eine veraltete Auswahl führt nach einer zwischenzeitlichen Änderung durch einen zweiten Spieler zu einer aktualisierten Auswahl oder verständlichen Rückmeldung nach der vereinbarten Regel.
- [x] Steht für einen vorgesehenen Zustand keine Auswahl bereit, lässt sich das Gespräch sinnvoll verlassen. Ungültige Objekt- und Zustandsverweise werden beim Laden oder Speichern erkannt.
- [x] Zwei Spieler beeinflussen einander über den gemeinsam konfigurierten Klingelzustand. Persönlicher Gesprächsfortschritt bleibt eine optionale Vertiefung.
- [x] Ein erreichbares Beispiel in der Referenzwelt zeigt am selben NPC die Gesprächsauswahl vor und nach der Betätigung eines passenden Objekts. Wo es passt, verwendet es einen vorhandenen NPC und Bedienpunkt.

## Bestätigter Umfang und Testgrenzen

Am 21.11.2026 mit „ja“ bestätigt. Eine Klingel ändert bei Nela in der Startlounge die verfügbaren Fragen und eine Antwort. Bedingungen verwenden gemeinsame Objektzustände. Wenn mehrere Antworten passen, gilt die erste in YAML-Reihenfolge. Persönlicher Gesprächsfortschritt bleibt eine eigene Entscheidung. Der Server prüft beim Klick, ob die angebotene Frage und Antwort noch gelten. Bei einer Änderung aktualisiert der Browser die Auswahl und erklärt den Wechsel. Eine leere Auswahl lässt sich schließen.

Geprüft werden Laden und Speichern einschließlich ungültiger Verweise, unveränderte ältere Referenzwelten, zwei Besucher mit einem Zustandswechsel während eines offenen Gesprächs und die isolierte Editorvorschau.

## Umsetzung und Kompatibilität

Nelas Gespräch in der [Startlounge](../../../worlds/reference/rooms/lounge.yaml) verwendet die Messingklingel rechts neben ihr. **Drücken** wechselt zwischen Bereit und Gerufen. Im Zustand Gerufen erscheint **Hast du die Klingel gehört?** und **Wer bist du?** erhält eine andere Antwort. Der Server wertet Bedingungen und Antwortvarianten unter derselben Sperre wie Objektaktionen aus. Der Browser zeigt die vom Server bestätigten Sprechblasen nur im eigenen Gespräch. Nach einer erfolgreichen Auswahl aktualisiert er eine zwischenzeitlich geänderte Fragenliste, sobald die Antwort abgespielt ist.

Format **1.12.0** ist eine additive Minor-Version mit dem optionalen `when` für eine Frage und `replies` für geordnete Antwortvarianten. Verweise gelten im selben Raum. Ein Eintrag ohne neue Felder behält seine frühere Bedeutung. Die Referenzwelt **5.3.0** ergänzt die Klingel als reversible Interaktion. Veröffentlichte Schemas und ältere Referenzfixtures wurden gegen `c9d1eb1` geprüft. Es gibt keine neuen Abhängigkeiten. Die Bedingungen werden in YAML geschrieben. In der Raum-Vorschau können Autoren die Klingel und Nelas Gespräch mit einem eigenen Objektzustand testen.

## Prüfbelege

- TypeScript-Typcheck und Clientbuild erfolgreich. Abschließende Vollsuite am 21.11.2026 mit **378 bestandenen Tests** (254 Server, 124 im Browser-Testprojekt). Keine Warnungen und keine übersprungenen Tests. Der Browserlauf dauerte 12 Minuten und 19 Sekunden.
- Die neuen Inhaltstests zeigten zuerst das fehlende YAML-Feld. Weitere Prüfungen sichern Editor-Upgrade, Ablehnung ungültiger Verweise, Reihenfolge der Antwortvarianten, leere Auswahl, getrennte Editorvorschau und die aktualisierte Auswahl in zwei Browsern. YamlDotNet schreibt wiederholte Bedingungen als vollständige Werte. Ein vorhandener Bildtest wartet jetzt auf das tatsächliche Laden des Sprite-Sheets.
- Spezifikations- und Standardsreview mit Nachprüfung. Das Gespräch verarbeitet fällige Aktionen vor der Auswahl, prüft die vollständige angebotene Frage samt Antwort, aktualisiert Fragen auch nach einer gültigen Auswahl und übernimmt in der Vorschau Klicks zum Überspringen. Beide Nachprüfungen ohne offene Befunde.
- Die lokale Abnahmekopie liefert Spiel und Editor mit HTTP 200. Spielbeitritt, Klingel, neue Frage, Antwort und Rücksetzen wurden dort im Browser geprüft. Nachweise unter `.scratch/adventure-chat/evidence/28`, einschließlich `lounge-bell-dialogue.png` und `verification.log`.

## Manuelle Abnahme

1. [Lokale Abnahmekopie](http://127.0.0.1:56074/) öffnen und die Referenzwelt betreten. **Spreche mit** wählen und Nela links vom Einstieg anklicken. **Wer bist du?** zeigt im Grundzustand „Ich bin Nela.“ und „Ich passe auf die Lounge auf.“
2. Das Gespräch offen lassen. In einem zweiten Browserfenster die Referenzwelt mit einem anderen Namen betreten. **Drücken** wählen und die kleine Messingklingel auf dem Holzständer rechts neben Nela anklicken. Im ersten Fenster **Wer bist du?** wählen. Die Fragenliste aktualisiert sich und ein Hinweis erscheint. **Wer bist du?** erneut wählen und die geänderte Antwort prüfen. **Hast du die Klingel gehört?** ist jetzt sichtbar.
3. Optional im [Raumeditor](http://127.0.0.1:56074/editor.html?worldId=reference) **Lounge → Raum-Vorschau** öffnen. Dort die Klingel drücken und mit Nela sprechen. Die Auswahl in der Vorschau richtet sich nach deren eigener Klingel.
