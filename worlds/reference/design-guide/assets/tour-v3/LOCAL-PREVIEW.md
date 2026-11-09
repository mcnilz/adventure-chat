# Lokale Bewegungsvorschau

[Laufstudio öffnen](http://127.0.0.1:54094/production/)

Die separate Vorschau bindet ausschließlich an 127.0.0.1 und verwendet einen freien Port. Bestehende Anwendungsserver werden nicht beendet oder geändert. Alle Dateien werden vom aktuellen Designprojekt gelesen. Kein externer Dienst, keine Installation, keine Veröffentlichung.

Alternativ `production/index.html` direkt im Browser öffnen. Die Seite benötigt keine CDNs oder externe Bibliotheken. Für beide Figuren Richtung, Stand, Einzelphasen, Lauf und hellen/dunklen Untergrund wählen. Die Animation startet erst auf Knopfdruck und verwendet 160ms pro Phase.

Vorschauprozess: PID 30704. Nur diesen Prozess beenden, wenn die Vorschau nicht mehr benötigt wird. Nach einem Neustart kann `production/tools/start-preview.ps1` einen neuen separaten Vorschauprozess starten; die neue Adresse wird wieder hier eingetragen.
