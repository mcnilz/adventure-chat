# 10: Welt auswählen und mehrere Welten getrennt betreiben

**What to build:** Ein Betreiber bietet mehrere konfigurierte Welten über denselben Server an. Besucher wählen nach dem Verbinden eine Welt und betreten deren Startraum. Gleichnamige Räume verschiedener Welten bleiben vollständig getrennt.

**Blocked by:** 09

**Status:** draft

## Acceptance criteria

- [ ] Server lädt alle ausdrücklich konfigurierten Weltverzeichnisse mit der Versions- und Inhaltsprüfung aus 09. Doppelte Welt-IDs und ungültige Pakete liefern klare Fehler. Vorgeschlagener Fehlermodus: Start bei fehlerhafter Konfiguration abbrechen, keine unbemerkte Teilmenge anbieten; vor Implementierung bestätigen.
- [ ] Client zeigt vor Eintritt eine Auswahl mit mindestens Namen der verfügbaren Welten. Lokale Pfade bleiben serverintern. Keine Welt wird aufgrund einer vom Client gelieferten freien Verzeichnisangabe geladen.
- [ ] Auswahl führt an den Start-Spawn der ausgewählten Welt. Auch bei nur einer Welt bleibt sie auswählbar; die direkte Ein-Welt-Einstiegsregel aus 09 wird damit bewusst durch den Auswahlablauf ersetzt.
- [ ] Server bindet jede Sitzung an genau eine Welt. Raumidentität ist mindestens Welt-ID plus Raum-ID. Zwei Welten mit identischen Raum-, Spawn- und Ausgangs-IDs sowie identischen relativen Bildpfaden funktionieren unabhängig.
- [ ] Teilnehmer, Bewegung, Snapshots, Raumrevisionen, SignalR-Gruppen, Raumnachrichten und Chat gelten ausschließlich innerhalb der gewählten Welt und des jeweiligen Raums. Bildauslieferung, Caches und Inhaltsabfragen berücksichtigen ebenfalls die Welt-ID.
- [ ] Server prüft Weltbeitritt und alle folgenden Befehle. Manipulierte Welt-/Raumangaben ermöglichen weder fremde Raumbeitritte noch Zugriff auf private Zustände anderer Welten. Raumtüren wechseln nur innerhalb der eigenen Welt.
- [ ] Rückkehr zur Weltwahl beendet die aktuelle Raumsitzung und Gruppenmitgliedschaft vollständig. Danach kann eine andere Welt betreten werden; keine Geisterfiguren oder verspäteten Ereignisse der alten Welt.
- [ ] Reconnect stellt genau eine konsistente Sitzung her. Vorschlag: letzte gewählte Welt im Arbeitsspeicher des Clients merken und dort frisch am Start-Spawn eintreten; falls nicht verfügbar, Auswahl anzeigen. Keine serverseitige Spielstandpersistenz. Konkrete Regel vor Implementierung bestätigen.
- [ ] Editor wählt im lokalen Autorenmodus eine konfigurierte Welt; Laden, Vorschau und Speichern sind darauf begrenzt. Keine Weltverwaltung, Uploads oder Übergänge zwischen Welten erforderlich.
- [ ] Zwei Browser in verschiedenen Welten sehen und hören sich nicht; in derselben Welt funktioniert der vorhandene Multiplayer einschließlich Raumwechseln unverändert.
- [ ] Anleitungen, Verträge, Testbelege, Tracker und active-state.md sind aktualisiert. Menschliche Abnahme bleibt eigener Schritt.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Sitzungs-/Hub- und Inhaltsgrenzen sowie Browserabläufe. Zwei Fixtures besitzen absichtlich dieselben lokalen IDs und Ressourcenpfade, aber unterschiedliche Bilder/Inhalte.
Prüfen: zwei Browser in derselben Welt; zwei Browser in verschiedenen Welten; fremde Befehle und Raumwechsel abweisen; Bild-/Cache-Isolation; Welt wechseln und alte Gruppen vollständig verlassen; wiederholter Beitritt; verspätete Ereignisse; Reconnect; nur eine Welt; keine gültige Welt; doppelte Welt-ID; nicht unterstützte Formatversion. Vorhandene Raumchat-/Bewegungstests weiterverwenden.

## Vorgehen

1. Weltverzeichnisliste über öffentliche Metadaten anbieten und Auswahl durchgehend implementieren.
2. Alle Zustands- und Ressourcengrenzen um Weltbezug ergänzen; Isolation zuerst an gleichen lokalen IDs nachweisen.
3. Verlassen, Wechsel und Reconnect konsistent abschließen.
4. Editor-Weltauswahl, lokale Checks, Review und Abnahmeanleitung ergänzen.

## Fachkontext
[Kompatibilitätsregeln](../../../docs/work-packages/world-format-compatibility.md), Ticket 09 und Root-AGENTS.md. Keine neuen Kommunikationskanäle oder persistenten Spielzustände.
