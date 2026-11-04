# 10: Welt auswählen und mehrere Welten getrennt betreiben

**What to build:** Ein Betreiber bietet mehrere konfigurierte Welten über denselben Server an. Besucher wählen nach dem Verbinden eine Welt und betreten deren Startraum. Gleichnamige Räume verschiedener Welten bleiben vollständig getrennt.

**Blocked by:** 09

**Status:** in-progress

Umfang, Fehlermodus, Reconnect-Regel und Testgrenzen am 02.11.2026 vom Benutzer bestätigt. Erste Teiländerung: `/api/worlds` listet konfigurierte Welt-IDs und Namen ohne Pfade. Der zugehörige öffentliche HTTP-Test wurde zuerst fehlschlagend und anschließend erfolgreich ausgeführt; Client-Typcheck und Build bestanden. Weltwahl und Isolation sind noch nicht implementiert; vollständige Checks und Review stehen aus.

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

### Zwischenstand 04.11.2026 – weltbezogene Rauminhalte

- Öffentliche Raumabfrage unter `/api/worlds/{worldId}/rooms/{id}`; unbekannte Welten oder Räume liefern 404. HTTP-Test mit zwei Welten und identischen Bildpfaden zuerst rot, anschließend grün.
- Raumdienste liefern weltbezogene Hintergrundadressen auch in Hub-Snapshots sowie nach Registrierung und Neuladen. PNG-Inhalte bleiben getrennt und werden ohne Cache ausgeliefert. Weltendpunkte in ein eigenes Modul ausgelagert; optionaler Weltbezug im TypeScript-Beitrittsvertrag ergänzt.
- Typcheck und Client-Build erfolgreich. Vollständiger Testlauf: 168 bestanden, keine Warnungen. Nach Review-Strukturkorrektur zusätzlich 36 Weltpakettests sowie Typcheck/Build erfolgreich.
- Review mit zwei tatsächlichen Subagenten: Standards fand fehlendes DTO-Feld und überladenes Startmodul; beides korrigiert. Spec fand keinen neuen Fehler im Teilstand, bestätigt aber die offenen Paketanforderungen.
- Weiterhin `in-progress`, keine menschliche Abnahme. Offen: Browserauswahl, explizites Verlassen/Wechsel, Reconnect-Weltauswahl, weltbezogener HTTP-Pfad-Fallback, Editor sowie zusätzliche Mehrweltbelege für Raumwechsel, Disconnect und verspätete Ereignisse.
- Formatbewertung: YAML, veröffentlichte Schemas, Versionsmatrix und Bedeutung der Raumdaten bleiben unverändert. Weltbezogene URLs betreffen den HTTP-/Laufzeitvertrag, keine Format-SemVer-Anhebung. Unveränderte ältere Fixtures und Schemahash-Prüfungen sind im erfolgreichen Lauf enthalten. Keine neuen Abhängigkeiten.
- Manuelle Teilprüfung: Server mit zwei unterschiedlichen Welt-IDs starten; `/api/worlds/{id}/rooms/lounge` für beide IDs öffnen, jeweils `backgroundUrl` aufrufen und mit dem jeweiligen Paketbild vergleichen; unbekannte Welt muss 404 liefern. Vollständige Browserabnahme erst nach Umsetzung der offenen Bedienabläufe. Diese Teilprüfung wurde hier nicht manuell durchgeführt.

### Zwischenstand 02.11.2026 – Sitzungsisolation

- Weltbezogener Laufzeitkatalog mit getrennten Raumdiensten, Sitzungsmanagern, Chatverläufen und Bewegungsrevisionen; SignalR-Gruppen enthalten Welt-ID und Raum-ID.
- `JoinRoom` erweitert um optionale `worldId`. Die Auswahl wird an der Verbindung gebunden; spätere Kommandos verwenden deren Welt. Unbekannte Welt-IDs und erneuter Eintritt einer bestehenden Sitzung werden abgewiesen. Der bestehende Client ohne Welt-ID verwendet vorläufig die erste konfigurierte Welt.
- Neuer öffentlicher Browser-/Hubtest mit zwei Weltpaketen und identischen Raum-IDs zuerst fehlgeschlagen (fremde Teilnehmer/Ereignisse/Verlauf, unbekannte Welt akzeptiert), nach Implementierung bestanden. Gleiche Welt erhält Chat und Bewegung weiterhin.
- TypeScript-Typcheck und Client-Build bestanden. Vollständiges `dotnet test`: 168 Tests in zwei Projekten bestanden, keine Warnungen (02.11.2026). `git diff --check` ohne Befund. Enthält unveränderte ältere Referenzwelt- und Schema-Prüfungen.
- Lokales Diff-Review durchgeführt, kein Subagent. Noch keine Paketabnahme: Browserauswahl, explizites Verlassen/Wechseln, Reconnect, weltbezogene Inhaltsabfragen/Bildrouten und Editor stehen aus. Raumwechsel-/Disconnect-Isolation braucht zusätzliche gezielte Mehrweltbelege.
- Formatprüfung: keine Änderung an YAML, unterstützten Formatversionen, veröffentlichten Schemas oder deren Interpretation; daher keine Format-SemVer-Anhebung. Die Erweiterung betrifft den Laufzeit-/Hubvertrag. Ältere Referenzfixtures und Schemas bleiben unverändert.

1. Weltverzeichnisliste über öffentliche Metadaten anbieten und Auswahl durchgehend implementieren.
2. Alle Zustands- und Ressourcengrenzen um Weltbezug ergänzen; Isolation zuerst an gleichen lokalen IDs nachweisen.
3. Verlassen, Wechsel und Reconnect konsistent abschließen.
4. Editor-Weltauswahl, lokale Checks, Review und Abnahmeanleitung ergänzen.

## Fachkontext
[Kompatibilitätsregeln](../../../docs/work-packages/world-format-compatibility.md), Ticket 09 und Root-AGENTS.md. Keine neuen Kommunikationskanäle oder persistenten Spielzustände.
