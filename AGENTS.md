# Arbeitsregeln für Codex

## Auftrag und Start
Implementiere einen begehbaren Multiplayer-Chat im Browser gemäß SPEC.md. Dieses Dokument ist eine Projektvorlage; noch kein Code vorhanden.
Lies zuerst active-state.md, danach GLOSSARY.md, das ausgewählte Ticket und passende Architekturentscheidungen. SPEC.md ist maßgeblich für den Umfang; Tickets konkretisieren ihn. Melde echte Widersprüche, erfinde keine Abnahme.

## Arbeitsablauf
1. Schlage das nächste nicht blockierte Ticket vor, nenne Ergebnis, offene Detailentscheidungen und Testgrenzen. Warte vor dessen Implementierung auf Bestätigung.
2. Setze nur dieses Paket um. Führe passende Checks aus; prüfe Funktion und Diff gegen Ticket und Regeln.
3. Stelle Ergebnis, Testbelege, Grenzen und konkrete manuelle Abnahmeschritte vor.
4. Setze Status auf ready-for-human. Erst ausdrückliche Abnahme macht ein Ticket done. Feedback führt zurück zu in-progress.
5. Aktualisiere Ticket und active-state.md. Ein neuer Kontext darf keinen Gesprächsverlauf benötigen.

## Technik und Umfang
TypeScript strict; ASP.NET Core/.NET 10; Canvas 2D; HTML/CSS; SignalR als vorgeschlagene Echtzeitverbindung. Keine Node-Serverlaufzeit. Node/npm als kleine Build-Werkzeugkette erlaubt, aber ebenfalls zu prüfen.
Keine SCUMM-Kompatibilität, keine eigene Skript-VM, keine Inventar-/Dialogsysteme im MVP.
Eine lauffähige Anwendung, keine Microservices, kein generisches Pluginsystem, keine vorsorglichen Repository-/Eventbus-Schichten. Rendering für Client und Editor gemeinsam nutzen, ohne Editorfunktionen im Spiel zu aktivieren.

## Abhängigkeiten
Jede neue direkte Abhängigkeit begründen: Nutzen, Alternative, Lizenz, Wartung, vollständiger transitiver Baum und Installationsskripte. Bekannte Herkunft ersetzt keine Prüfung. MIT-/BSD-/Apache-artige Lizenzen bevorzugen; andere vor Aufnahme klären.
Exakte direkte Versionen und Lockfiles für npm und NuGet; reproduzierbarer Restore. Kein npx ...@latest ohne Sichtung und bewusste Freigabe der Werkzeugversion. Keine Bibliotheken zur Laufzeit von einem CDN laden.
Keine FluentAssertions. Bedeutungsvolle Tests an den in TESTING.md bestätigten öffentlichen Grenzen; keine privaten Implementierungsdetails testen.

## Grenzen
Keine Remote-Repositories, Issues, Pushes, PRs, Deployments oder Infrastruktur ohne konkreten Auftrag. Nur lokale Arbeit.
Ein Implementierungsauftrag autorisiert einen lokalen Commit nach erfolgreichem Review, sofern der Benutzer keine abweichende Vorgabe macht und ein Git-Repository vorhanden ist. Nicht pushen; kein Repository stillschweigend initialisieren.
Skills nur nutzen, wenn tatsächlich installiert/verfügbar. Niemals Skill-Aufruf oder Subagent-Review behaupten, der nicht erfolgt ist. Fehlende Skills melden und mit gleichwertiger lokaler Arbeitsweise fortfahren, soweit autorisiert.
Keine parallele Umsetzung voraussetzen; implement-spec erst bei explizitem Wunsch nach paralleler Gesamtausführung einsetzen.

## Agent skills
### Issue tracker
Lokale Markdown-Tickets; siehe [Trackerregeln](docs/agents/issue-tracker.md).
### Domain docs
Ein Glossar und gezielte ADRs; siehe [Fachkontext](docs/agents/domain.md).
### Workflow
[SKILLS.md](SKILLS.md) beschreibt geprüfte Quellen, optionale Skills und Startprompts. Projektvorgaben und konkrete Benutzeranweisungen haben Vorrang vor Skill-Defaults.

