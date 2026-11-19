# 28 – Gesprächsoptionen und Antworten an Zustände binden

**What to build:** Autoren können die Verfügbarkeit von Gesprächsoptionen und Antworten an vorhandene Objektzustände binden. Eigener Gesprächsfortschritt bleibt als separat zu entscheidende Vertiefung vorgesehen.

**Blocked by:** [20](20-shared-object-actions.md), [27](27-npc-dialogue.md).

**Status:** draft

## Acceptance criteria

- [ ] Der Autor bindet eine Option oder Antwort an einen benannten Zustand eines vorhandenen Raumobjekts. Vor und nach dem Zustandswechsel sind die beabsichtigten unterschiedlichen Auswahlmöglichkeiten und Antworten sichtbar.
- [ ] Eine veraltete Auswahl führt nach einer zwischenzeitlichen Änderung durch einen zweiten Spieler zu einer aktualisierten Auswahl oder verständlichen Rückmeldung nach der vereinbarten Regel.
- [ ] Steht für einen vorgesehenen Zustand keine Auswahl bereit, lässt sich das Gespräch sinnvoll verlassen. Ungültige Objekt- und Zustandsverweise werden beim Laden oder Speichern erkannt.
- [ ] Falls ein eigener Gesprächsfortschritt gebraucht wird, weist ein Beispiel dessen vereinbarten Geltungsbereich nach. Zwei Spieler beeinflussen einander nur über ausdrücklich gemeinsame Zustände.
- [ ] Ein erreichbares Beispiel in der Referenzwelt zeigt am selben NPC die Gesprächsauswahl vor und nach der Betätigung eines passenden Objekts. Wo es passt, verwendet es einen vorhandenen NPC und Bedienpunkt.

## Offene Entscheidungen und vorgeschlagene Testgrenzen

Zuerst vorhandene Objektzustände verwenden. Vor der Freigabe klären, ob für eine konkrete Szene zusätzlich persönlicher Gesprächsfortschritt nötig ist, wie lange dieser gilt und wie mehrere passende Antworten priorisiert werden. Inhaltsvalidierung, Zustandswechsel während eines offenen Gesprächs, zwei Besucher und isolierte Editorvorschau als öffentliche Grenzen prüfen.
