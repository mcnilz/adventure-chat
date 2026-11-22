# 31 – Kombinierte Interaktionen in einer Referenzszene vorführen

**What to build:** Ein leicht auffindbarer Ablauf in der Referenzwelt verbindet Objektzustände, Figurenanimationen, zustandsabhängige NPC-Gespräche und eine freiwillige Führung. Dieselben Stationen lassen sich auch einzeln ausprobieren.

**Blocked by:** [25](25-object-state-transitions.md), [26](26-character-animations-for-object-actions.md), [28](28-state-dependent-dialogue.md), [30](30-follow-npc.md). Falls die Platzierung in die noch nicht abgenommene Gestaltung aus 19 eingreift, deren Freigabe vor der Integration berücksichtigen.

**Status:** ready-for-human

## Acceptance criteria

- [x] Der Ablauf ist in der Referenzwelt vom Einstieg aus auf kurzem, verständlichem Weg erreichbar. Er ergänzt die Beispiele aus den Funktionstickets und verwendet vorhandene Räume und Stationen, wo sie passen. Sind eigene Räume sinnvoll, kann ein zentraler Startraum direkte Zugänge anbieten.
- [x] Eine Objektinteraktion eröffnet oder verändert ein Gespräch. Der NPC bietet eine Führung über mehrere Stationen an. Der Besucher kann sie ablehnen oder abbrechen und die Stationen unabhängig davon benutzen.
- [x] Ein zweiter Besucher kann die gemeinsamen Objektänderungen und Figurenanimationen nachvollziehen. Autor und Prüfer können dieselbe Szene auch in einer isolierten Arbeitskopie ausprobieren.

## Beispiel und vorgeschlagene Testgrenzen

Eine kurze Büroeinführung ist ein mögliches Motiv. Eine Klingel verändert den Gesprächseinstieg mit einem Empfangs-NPC. Optional kann ein Auslöser nach [32](32-object-triggered-npc-reactions.md) den NPC zum Eingang rufen. Der NPC bietet einen Rundgang an, der höhenverstellbaren Schreibtisch, Sitzplatz und Tafel als Stationen nutzt. Lage und Motive vor Inhaltsintegration festlegen. Einstieg, Auffindbarkeit, Gespräch, Rundgang, Abbruch und freie Nutzung im Browser sowie in isolierter Vorschau prüfen.

## Freigegebener Umfang

Am 22.11.2026 mit „ja“ bestätigt. Die Referenzszene nutzt Lounge, Tavernen-Garten und Werkstatt. Die Klingel bietet im Gespräch mit Nela eine zusätzliche Frage, deren Antwort auf den Rundgang hinweist. Nela erklärt die Werkstatttafel, den Stuhl und den Hubtisch an drei Stationen. Die Gäste können die Führung ablehnen, sie unterwegs beenden oder die Werkstatt selbst betreten. In der isolierten Editorvorschau lassen sich Klingelgespräch und Werkstattobjekte einzeln ausprobieren. Die mehrraumige Führung läuft im Spiel.

Die Werkstatttür startet offen, damit Nela nach der Führung zu Fuß in die Lounge zurückkehrt. Besucher können die Tür wie gewohnt schließen und öffnen. Falls beide Ausgänge gesperrt sind, wartet Nela auf einen wieder nutzbaren Ausgang. Die Ergänzung erhöht die Inhaltsversion der Referenzwelt auf `5.6.0`. Sie verwendet die vorhandenen Felder und dieselbe Auswertung des Formats `1.14.0`.

## Umsetzung und Prüfung

- Der neue Browserablauf scheiterte vor der Erweiterung der Führung. Nach der Inhaltsänderung bestanden sieben gezielte Browserfälle zu Führung und Werkstatt. Zwei gezielte Fälle bestanden mit den abschließenden Prüfergänzungen. Sie beobachten die Einladung nach dem Klingeln, die drei Werkstattstationen, Nelas sichtbaren Rückweg samt erneutem Angebot und den zweiten Besucher mit Hubhöhe, Sitzpose und Tafelgeste. Die Editorvorschau zeigt Hubtisch, Sitzpose und beschriftete Tafel.
- Die getrennten Reviews gegen `f19ccc7` prüften Coding Standards und Ticketumfang. Die Prüfhilfen werden wiederverwendet. Die Animationstests prüfen Bild und Dauer. Der Test zum natürlichen Abschluss prüft den sichtbaren Rückweg zur Tür, die Rückkehr in die Lounge und die erneute Einladung. Der Spec-Review fand keine blockierenden Abweichungen.
- Typcheck, Clientbuild, Diffprüfung und die separate Abnahmekopie geprüft. Die Spielseite und der Zugriff auf die Lounge im Raumeditor lieferten HTTP 200. Veröffentlichte Schemas, ältere Referenzfixtures und Grafikdateien sind unverändert. Es gibt keine neuen Abhängigkeiten. Die Bildschirmaufnahme und lokalen Prüfprotokolle liegen unter `.scratch/adventure-chat/evidence/31`.
- Die abschließende Vollsuite bestand mit 390 Tests (257 Server, 133 Browser), ohne Fehler oder übersprungene Tests. Die menschliche Abnahme steht aus.

## Manuelle Abnahme

1. [Abnahmekopie](http://127.0.0.1:61176/) öffnen. In der Startlounge **Drücken** wählen und auf die Klingel klicken. Mit **Spreche mit** Nela ansprechen und **Hast du die Klingel gehört?** wählen. Nela bietet den Rundgang an. **Gespräch beenden** lässt den Gast selbst weiterlaufen.
2. Nela erneut ansprechen und **Führung starten** wählen. Im Garten **Führung beenden** ausprobieren. Über die linke Tür zur Lounge zurückgehen, Nelas Rückkehr abwarten und eine Führung vollständig durchlaufen lassen. Die Erklärungen erscheinen unter dem Spielfeld. Nach der letzten Station bleibt der Gast in der Werkstatt und Nela läuft zur Lounge zurück.
3. In einem zweiten Fenster frei durch den Garten zur Werkstatt gehen. Eine Pfeiltaste rechts an der Rückwand drücken. Den Stuhl **Ziehen** und **Benutzen**. Die linke Tafel **Benutzen**. Beide Fenster zeigen den bewegten Hubtisch, die Sitzpose und die Handgeste mit anschließend sichtbarem Werkstattplan. Im [Raumeditor](http://127.0.0.1:61176/editor.html?worldId=reference) lassen sich diese Aktionen in der Werkstattvorschau ebenfalls ausführen.
