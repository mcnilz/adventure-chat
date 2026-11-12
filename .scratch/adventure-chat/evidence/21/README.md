# Ticket 21 – lokale Nachweise

Implementierungsbasis: `4d110681fde69ff3d60c03b055c63e4c65ea352b`.

- `compatibility.json`: Vergleich aller 192 bereits vorhandenen Schema-, Fixture-, Raum- und Weltdateien gegen die Basis. 103 LFS-Bilder SHA256-bytegleich; vorhandene CRLF-Textcheckouts gegenüber Git-LF berücksichtigt, keine Dateien umgeschrieben.
- `schema-extension.json`: 1.8.0 unterscheidet sich strukturell ausschließlich durch Versionsangaben und die beiden optionalen Zustandsfelder von 1.7.0.
- `validation.json`: abschließender Typcheck/Build, vollständige Suite mit 328 bestandenen Tests und 0 Warnungen sowie Reviewstand; menschliche Endabnahme weiterhin offen.
- `closed.png`, `open.png`, `stopped.png`: tatsächliche Browseraufnahmen der synthetischen Testwelt. Rote PNG-Muster stehen für Tür/Stuhl; keine endgültige Gestaltung.
- `acceptance-server.json`: lokale separate Abnahmeumgebung, HTTP200 und tatsächlicher Browsereintritt geprüft. Programm- und Weltkopien sowie Logs bleiben lokal und werden nicht versioniert.

Abnahmeschritte und vollständige tatsächliche Prüf-/Reviewhistorie stehen im [Ticket](../../issues/21-state-dependent-navigation.md). Menschliche Endabnahme ist weiterhin offen. Bestehende Umgebungen und Benutzer-Arbeitskopien bleiben erhalten.

Prüfbefehle ohne externe Dienste oder neue Abhängigkeiten:

```text
npm --prefix src/AdventureChat.Client run typecheck
npm --prefix src/AdventureChat.Client run build
dotnet test tests/AdventureChat.ServerTests --filter FullyQualifiedName~DynamicNavigation --no-restore
dotnet test tests/AdventureChat.E2ETests --filter FullyQualifiedName~DynamicNavigation --no-restore
dotnet test --no-restore
```

Die unabhängigen Reviews prüften Standards und Spec getrennt. Zwei konkrete Spec-Befunde wurden rot/grün korrigiert und erneut geprüft. Abschlussreview der Korrekturen ohne weitere konkrete Befunde; keine parallele Implementierung durch Subagenten.
