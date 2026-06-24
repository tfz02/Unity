# MergeDefenseSurvivor

**MergeDefenseSurvivor** ist ein geplantes 2D-Mobile-Game für Android in Unity.

Arbeitstitel und Konzept:

- **Genre:** Tower Defense + Merge-Spiel + Roguelite-Upgrades
- **Zielplattform:** Android-Smartphone
- **Engine:** Unity 6000.5.0f1
- **Projektordner lokal:** `D:\Unity\Projekte\MergeDefenseSurvivor`
- **Monetarisierung:** später vorbereitet für Rewarded Ads, Interstitials und optionale In-App-Käufe

## Spielidee

Der Spieler verteidigt eine Basis gegen Gegnerwellen. Während des Spiels kauft er Türme, platziert sie auf freien Slots und merged gleiche Türme zu stärkeren Varianten. Nach bestimmten Wellen erhält der Spieler Roguelite-Upgrades, die den Lauf verändern und unterschiedliche Strategien ermöglichen.

## Kern-Gameplay-Loop

1. Gegnerwelle startet.
2. Gegner laufen Richtung Basis.
3. Türme greifen automatisch Gegner in Reichweite an.
4. Besiegte Gegner geben Währung.
5. Spieler kauft neue Türme.
6. Zwei gleiche Türme können zu einem höheren Turm-Level gemerged werden.
7. Nach einer Welle wählt der Spieler ein Upgrade.
8. Schwierigkeit und Gegnerdichte steigen mit jeder Welle.

## Geplante Hauptsysteme

- Basis/Lebenspunkte
- Gegnerwellen
- Tower-Platzierung
- Tower-Angriffssystem
- Merge-System
- Währungssystem
- Roguelite-Upgrade-Auswahl
- Mobile UI
- Savegame/Progression
- Monetarisierung

## Repository-Struktur

```text
Assets/
  _Project/
    Art/
    Audio/
    Materials/
    Prefabs/
    Scenes/
    ScriptableObjects/
    Scripts/
      Core/
      Economy/
      Enemies/
      Towers/
      UI/
      Upgrades/
      Waves/
    Settings/
    Tests/
Packages/
ProjectSettings/
```

## Lokale Einrichtung

1. Repository klonen:

```bash
git clone https://github.com/tfz02/Unity.git
```

2. Ordner lokal nach Wunsch in Unity Hub als Projekt öffnen, empfohlen:

```text
D:\Unity\Projekte\MergeDefenseSurvivor
```

3. Unity-Version verwenden:

```text
Unity 6000.5.0f1
```

4. Android Build Support prüfen:

```text
D:\Unity\Unity Hub\Editor\6000.5.0f1\Editor\Data\PlaybackEngines\AndroidPlayer
```

## Entwicklungsprinzipien

- Mobile-first: Hochformat, Touch-UI, kurze Runden.
- Systeme modular halten.
- Gameplay-Daten bevorzugt über ScriptableObjects konfigurierbar machen.
- Keine generierten Unity-Ordner committen: `Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs`.
- Jedes Unity-Asset muss seine `.meta`-Datei behalten.

## Nächste technische Schritte

- Unity-Projekt lokal öffnen und initiale Unity-Dateien erzeugen lassen.
- Erste Szene `Main.unity` anlegen.
- Core-GameLoop implementieren.
- Erste Gegnerbewegung und Basis-Schaden testen.
- Einfachen Turm mit Auto-Fire implementieren.
- Danach Merge-Mechanik und Upgrade-Auswahl ergänzen.
