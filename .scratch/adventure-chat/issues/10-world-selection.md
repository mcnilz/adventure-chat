# 10: Welt auswählen und mehrere Welten getrennt betreiben

**What to build:** Ein Betreiber bietet mehrere konfigurierte Welten über denselben Server an. Besucher wählen nach dem Verbinden eine Welt und betreten deren Startraum. Gleichnamige Räume verschiedener Welten bleiben vollständig getrennt.

**Blocked by:** 09

**Status:** ready-for-human

Umfang, Fehlermodus, Reconnect-Regel und Testgrenzen am 02.11.2026 bestätigt. Implementierung und automatisierte Prüfung am 04.11.2026 abgeschlossen; 172 Tests bestanden, keine Warnungen. Beide Reviewachsen ohne offene Befunde nach Korrekturen. Menschliche Abnahme steht aus; Anleitung unten. Die Kriterienhaken bezeichnen den technisch geprüften Stand, keine menschliche Abnahme.

## Acceptance criteria

- [x] Server lädt alle ausdrücklich konfigurierten Weltverzeichnisse mit der Versions- und Inhaltsprüfung aus 09. Doppelte Welt-IDs und ungültige Pakete liefern klare Fehler. Vorgeschlagener Fehlermodus: Start bei fehlerhafter Konfiguration abbrechen, keine unbemerkte Teilmenge anbieten; vor Implementierung bestätigen.
- [x] Client zeigt vor Eintritt eine Auswahl mit mindestens Namen der verfügbaren Welten. Lokale Pfade bleiben serverintern. Keine Welt wird aufgrund einer vom Client gelieferten freien Verzeichnisangabe geladen.
- [x] Auswahl führt an den Start-Spawn der ausgewählten Welt. Auch bei nur einer Welt bleibt sie auswählbar; die direkte Ein-Welt-Einstiegsregel aus 09 wird damit bewusst durch den Auswahlablauf ersetzt.
- [x] Server bindet jede Sitzung an genau eine Welt. Raumidentität ist mindestens Welt-ID plus Raum-ID. Zwei Welten mit identischen Raum-, Spawn- und Ausgangs-IDs sowie identischen relativen Bildpfaden funktionieren unabhängig.
- [x] Teilnehmer, Bewegung, Snapshots, Raumrevisionen, SignalR-Gruppen, Raumnachrichten und Chat gelten ausschließlich innerhalb der gewählten Welt und des jeweiligen Raums. Bildauslieferung, Caches und Inhaltsabfragen berücksichtigen ebenfalls die Welt-ID.
- [x] Server prüft Weltbeitritt und alle folgenden Befehle. Manipulierte Welt-/Raumangaben ermöglichen weder fremde Raumbeitritte noch Zugriff auf private Zustände anderer Welten. Raumtüren wechseln nur innerhalb der eigenen Welt.
- [x] Rückkehr zur Weltwahl beendet die aktuelle Raumsitzung und Gruppenmitgliedschaft vollständig. Danach kann eine andere Welt betreten werden; keine Geisterfiguren oder verspäteten Ereignisse der alten Welt.
- [x] Reconnect stellt genau eine konsistente Sitzung her. Vorschlag: letzte gewählte Welt im Arbeitsspeicher des Clients merken und dort frisch am Start-Spawn eintreten; falls nicht verfügbar, Auswahl anzeigen. Keine serverseitige Spielstandpersistenz. Konkrete Regel vor Implementierung bestätigen.
- [x] Editor wählt im lokalen Autorenmodus eine konfigurierte Welt; Laden, Vorschau und Speichern sind darauf begrenzt. Keine Weltverwaltung, Uploads oder Übergänge zwischen Welten erforderlich.
- [x] Zwei Browser in verschiedenen Welten sehen und hören sich nicht; in derselben Welt funktioniert der vorhandene Multiplayer einschließlich Raumwechseln unverändert.
- [x] Anleitungen, Verträge, Testbelege, Tracker und active-state.md sind aktualisiert. Menschliche Abnahme bleibt eigener Schritt.

## Testgrenzen – vor Beginn bestätigen

Öffentliche Sitzungs-/Hub- und Inhaltsgrenzen sowie Browserabläufe. Zwei Fixtures besitzen absichtlich dieselben lokalen IDs und Ressourcenpfade, aber unterschiedliche Bilder/Inhalte.
Prüfen: zwei Browser in derselben Welt; zwei Browser in verschiedenen Welten; fremde Befehle und Raumwechsel abweisen; Bild-/Cache-Isolation; Welt wechseln und alte Gruppen vollständig verlassen; wiederholter Beitritt; verspätete Ereignisse; Reconnect; nur eine Welt; keine gültige Welt; doppelte Welt-ID; nicht unterstützte Formatversion. Vorhandene Raumchat-/Bewegungstests weiterverwenden.

## Vorgehen

### Abschluss 04.11.2026 – Weltbedienung und Autorenmodus

- Gemeinsame Weltwahl für Spiel und Editor, auch bei genau einer konfigurierten Welt. Anzeigenamen der Welten werden als Text ausgegeben; nur konfigurierte IDs gelangen an den Server.
- `LeaveWorld` entfernt Sitzung und Gruppenmitgliedschaft. Der Browser wartet auf das Verlassen und startet die Seite für eine neue Auswahl neu; damit bleiben keine alten Spielinstanzen, Chatansichten oder ausstehenden Antworten beim Weltwechsel erhalten. Erneuter Beitritt auf derselben Hubverbindung ist nach explizitem Verlassen ebenfalls geprüft.
- Reconnect verwendet die im Spielobjekt gemerkte Welt-ID und einen frischen Start-Snapshot. Fehlender Weltstart führt zurück zur Auswahl. Initialer Verbindungsfehler zeigt einen Wiederholungsbutton; der zugehörige Browsertest war vor der Korrektur rot.
- Bei konfigurierten Welten ist Bewegung ausschließlich an die Hub-Sitzung gebunden. Der bisherige HTTP-Bewegungsfallback bleibt dem Legacy-Betrieb vorbehalten. Die bestehenden HTTP-Raumrouten sind dokumentierte Zugänge zur ersten Welt; weltbezogene Raumdaten und Bildadressen verwenden explizite Welt-IDs.
- Lokaler Editor wählt die Welt, bindet alle Autorenanfragen über `worldId` daran und verwendet denselben Raumdienst wie deren Spielsitzungen. Unbekannte IDs liefern 404. Rohdaten behalten ihre relativen Ressourcenpfade; Bearbeitungsansicht, Galerie und Vorschau lösen sie zur gewählten Welt auf. Ein Seitenwechsel zur Weltwahl verwendet die bestehende Warnung vor ungespeicherten Änderungen.
- Neue Browsertests: Weltwahl/Wechsel mit Chat- und Anwesenheitsbereinigung; Editor-Speichern und Neuladen ohne Änderung der anderen Welt; unterschiedliche Bildinhalte in Bearbeitung und Vorschau; Türwechsel und Reconnect mit Beobachter in anderer Welt; nicht mehr verfügbarer Weltstart; initial fehlgeschlagene Verbindung; alte Gruppen nach direktem Hub-Weltwechsel. Erste Auswahl- und Editor-Speichertests zuerst rot, danach grün.
- Review durch zwei Subagenten (Standards und Spec) gegen `869643b`: Renderer-Verantwortung und initialer Verbindungsfehler korrigiert und erneut geprüft; keine verbleibenden Befunde. Typcheck/Build und fünf gezielte Browsertests bestanden. Abschließende Vollsuite nach Reviewkorrektur: 172 Tests in zwei Projekten bestanden, keine Warnungen. Diff-Whitespaceprüfung ohne Befund. Ein früherer Lauf wurde durch Browserstartrechte verhindert; der erneute Lauf mit passenden lokalen Rechten bestand. Eine zwischenzeitliche parallele Testkompilierung scheiterte an einer Dateisperre; die abschließenden Läufe wurden nacheinander ausgeführt.
- Keine neuen Abhängigkeiten. Keine Änderungen an Autorenformat, Bedeutung veröffentlichter Welten, Schemas oder Versionsmatrix; daher keine Format-SemVer-Anhebung. Unveränderte ältere Fixtures und Schemahashes bleiben Teil der Suite. Browserbedienung ersetzt bewusst den direkten Ein-Welt-Eintritt aus 09 gemäß bestätigtem Ticketumfang.

### Manuelle Abnahme für Ticket 10

Zwei lokale Kopien der Referenzwelt vorbereiten (PowerShell im Repository):

```powershell
$acceptanceRoot = Join-Path $env:TEMP ("adventure-chat-acceptance-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $acceptanceRoot
Copy-Item -Recurse worlds/reference (Join-Path $acceptanceRoot "first")
Copy-Item -Recurse worlds/reference (Join-Path $acceptanceRoot "other")
$otherManifest = Join-Path $acceptanceRoot "other/world.yaml"
(Get-Content -Raw $otherManifest).Replace("id: reference", "id: other").Replace("name: Referenzwelt", "name: Zweite Welt") | Set-Content -Encoding utf8 $otherManifest
Copy-Item (Join-Path $acceptanceRoot "other/assets/images/garden.png") (Join-Path $acceptanceRoot "other/assets/images/lounge.png") -Force
dotnet run --project src/AdventureChat.Server -- --authoring --urls http://127.0.0.1:5080 --World:Roots:0 (Join-Path $acceptanceRoot "first") --World:Roots:1 (Join-Path $acceptanceRoot "other")
```

1. `http://127.0.0.1:5080` in zwei getrennten Browserfenstern öffnen. In beiden „Referenzwelt“ wählen und betreten. Figuren, Bewegung und Chat müssen gegenseitig sichtbar sein; eine Tür benutzen und die Raumtrennung prüfen.
2. In einem Fenster „Zur Weltwahl“ wählen. Die Figur muss im anderen verschwinden. „Zweite Welt“ betreten: anderer Hintergrund trotz identischer Raum-ID, keine fremden Figuren oder Nachrichten. Danach beide in dieselbe Welt bringen und gemeinsamen Chat erneut prüfen.
3. In den Browser-Entwicklerwerkzeugen die Verbindung kurz unterbrechen und wieder freigeben. Nach Wiederverbindung steht genau eine eigene Figur am Start-Spawn derselben Welt. Im anderen Fenster darf kein Doppelgänger bleiben.
4. `http://127.0.0.1:5080/editor.html` öffnen, „Zweite Welt“ wählen. Raumname oder Notiz ändern, speichern und Seite neu laden. Änderung und gewähltes Bild müssen erhalten sein; Vorschau testen. Zur Weltwahl wechseln, Referenzwelt öffnen und prüfen, dass deren Inhalte unverändert sind. Bei ungespeicherter Änderung muss ein Weltwechsel die Browserwarnung auslösen.
5. Optional nur einen `World:Roots`-Eintrag starten: Die Welt bleibt vor Eintritt auswählbar. Fehlerhafte oder doppelte Weltpakete müssen den Serverstart mit Fehler beenden.

Testgrenzen: Automatisierte Prüfungen sichern HTTP-/Hubverträge und Browserabläufe. Bewegungsgefühl, Lesbarkeit, Bilddarstellung und Editorbedienung bleiben Teil dieser menschlichen Abnahme. Noch keine menschliche Abnahme erfolgt.

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
