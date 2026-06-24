# MergeDefenseSurvivor MVP Testplan

## Inhalt der aktuellen Version

- Hauptmenue
- Run starten
- Tower kaufen
- gleiche Tower mergen
- Gegnerwellen
- Bosswellen
- Basisleben
- Coins und Score
- Upgrades nach jeder Welle
- Meta-Fortschritt ueber Gems
- Highscore-Speicherung ueber PlayerPrefs
- Werbeplatzhalter fuer spaetere Monetarisierung

## Test in Unity

1. `git pull` ausfuehren.
2. Unity kompilieren lassen.
3. Console pruefen.
4. Play druecken.
5. Das Spiel sollte automatisch erscheinen.

## Testfaelle

### Start

Erwartung: Hauptmenue erscheint. Button `Neuer Run` ist sichtbar.

### Tower kaufen

Ablauf: Run starten und einen leeren Slot anklicken.

Erwartung: Coins sinken und ein Tower erscheint.

### Merge

Ablauf: Zwei gleiche Tower mit gleichem Level nacheinander anklicken.

Erwartung: Ein Tower steigt im Level, der andere verschwindet.

### Welle

Ablauf: Mindestens einen Tower kaufen und `Welle starten` druecken.

Erwartung: Gegner laufen zur Basis. Tower greifen automatisch an.

### Upgrade

Ablauf: Welle beenden und ein Upgrade auswaehlen.

Erwartung: Upgrade wird angewendet und die naechste Build-Phase startet.

### Boss

Ablauf: Bis Wave 5 spielen.

Erwartung: Ein groesserer Boss erscheint.

### Save

Ablauf: Game Over erreichen, Play beenden und neu starten.

Erwartung: Best Wave, Best Score und Gems bleiben gespeichert.

## Bekannte Grenzen

- Grafik ist noch Platzhalter.
- UI ist noch technische MVP-UI.
- Sounds fehlen.
- Balancing ist noch nicht final.
- Android-Build muss separat getestet werden.

## Naechste Schritte

1. Console-Fehler beheben.
2. Android-Darstellung pruefen.
3. Balancing der ersten 10 Wellen anpassen.
4. UI spaeter auf Canvas umstellen.
5. echte Sprites und einfache Animationen einbauen.
6. Audio ergaenzen.
7. Monetarisierung sauber integrieren.
8. Tutorial fuer die erste Runde einbauen.
