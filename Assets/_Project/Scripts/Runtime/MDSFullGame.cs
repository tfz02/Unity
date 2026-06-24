using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSFullGame : MonoBehaviour
    {
        private enum Phase { Menu, Build, Wave, Upgrade, GameOver }
        private enum TowerType { Cannon, Rapid, Sniper, Frost }

        private sealed class Slot
        {
            public Vector2 Pos;
            public SpriteRenderer Box;
            public Tower Tower;
        }

        private sealed class Tower
        {
            public TowerType Type;
            public int Level;
            public float Cd;
            public GameObject Go;
            public SpriteRenderer Sr;
        }

        private sealed class Enemy
        {
            public int Hp;
            public int MaxHp;
            public float Speed;
            public int Reward;
            public bool Boss;
            public float Slow;
            public GameObject Go;
            public SpriteRenderer Sr;
        }

        private const string SaveKey = "MDS_V001_";
        private const int MaxLevel = 6;

        private readonly List<Slot> slots = new();
        private readonly List<Enemy> enemies = new();
        private readonly List<string> notes = new();

        private Camera cam;
        private Sprite square;
        private Sprite circle;
        private Texture2D squareTex;
        private Texture2D circleTex;
        private System.Random random;

        private Phase phase = Phase.Menu;
        private int wave;
        private int coins;
        private int baseHp;
        private int maxBaseHp;
        private int score;
        private int selected = -1;
        private int spawnLeft;
        private float spawnTimer;
        private int kills;
        private int merges;
        private bool adCoinsUsed;

        private int runDamage;
        private float runSpeed = 1f;
        private int runIncome;
        private int runFrost;

        private int bestWave;
        private int bestScore;
        private int gems;
        private int metaCoins;
        private int metaDamage;
        private int metaBase;

        private readonly Vector2 spawn = new(0f, 4.45f);
        private readonly Vector2 core = new(0f, -4.0f);

        private int BuyCost => 30 + wave * 4 + CountTowers() * 4;
        private bool BossWave => wave > 0 && wave % 5 == 0;

        private void Awake()
        {
            random = new System.Random();
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }
        }

        private void Start()
        {
            Load();
            CreateCamera();
            CreateSprites();
            CreateWorld();
        }

        private void Update()
        {
            if (phase == Phase.Build)
            {
                HandleTap();
            }

            if (phase == Phase.Wave)
            {
                SpawnTick(Time.deltaTime);
                EnemyTick(Time.deltaTime);
                TowerTick(Time.deltaTime);
                EndWaveCheck();
            }
        }

        private void OnDestroy()
        {
            Save();
            if (squareTex != null) Destroy(squareTex);
            if (circleTex != null) Destroy(circleTex);
        }

        private void CreateCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                GameObject c = new GameObject("Main Camera");
                c.tag = "MainCamera";
                cam = c.AddComponent<Camera>();
                c.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateSprites()
        {
            squareTex = new Texture2D(1, 1);
            squareTex.SetPixel(0, 0, Color.white);
            squareTex.Apply();
            square = Sprite.Create(squareTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            circleTex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 31.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    circleTex.SetPixel(x, y, d <= 30f ? Color.white : Color.clear);
                }
            }
            circleTex.Apply();
            circle = Sprite.Create(circleTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        private void CreateWorld()
        {
            RectObj("BG", Vector2.zero, new Vector2(8f, 10.8f), new Color(0.07f, 0.08f, 0.11f), -10);
            RectObj("PATH", new Vector2(0f, 0.1f), new Vector2(0.7f, 8.6f), new Color(0.18f, 0.19f, 0.22f), -1);
            RectObj("SPAWN", spawn, new Vector2(1.6f, 0.45f), new Color(0.08f, 0.46f, 0.22f), 1);
            RectObj("BASE", core, new Vector2(2.2f, 0.7f), new Color(0.75f, 0.18f, 0.15f), 1);

            Vector2[] p =
            {
                new(-2.7f, 3.1f), new(2.7f, 3.1f),
                new(-2.7f, 1.85f), new(2.7f, 1.85f),
                new(-2.7f, 0.6f), new(2.7f, 0.6f),
                new(-2.7f, -0.65f), new(2.7f, -0.65f),
                new(-2.7f, -1.9f), new(2.7f, -1.9f),
                new(-2.7f, -3.15f), new(2.7f, -3.15f)
            };

            for (int i = 0; i < p.Length; i++)
            {
                GameObject b = RectObj("SLOT", p[i], new Vector2(0.95f, 0.95f), SlotColor(false), 2);
                slots.Add(new Slot { Pos = p[i], Box = b.GetComponent<SpriteRenderer>() });
            }
        }

        private GameObject RectObj(string n, Vector2 p, Vector2 s, Color c, int order)
        {
            GameObject g = new GameObject(n);
            g.transform.position = p;
            g.transform.localScale = s;
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = c;
            sr.sortingOrder = order;
            return g;
        }

        private GameObject CircleObj(string n, Vector2 p, float s, Color c, int order)
        {
            GameObject g = new GameObject(n);
            g.transform.position = p;
            g.transform.localScale = Vector3.one * s;
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.color = c;
            sr.sortingOrder = order;
            return g;
        }

        private void NewRun()
        {
            ClearEnemies();
            ClearTowers();
            wave = 1;
            coins = 120 + metaCoins * 25;
            maxBaseHp = 20 + metaBase * 4;
            baseHp = maxBaseHp;
            score = 0;
            kills = 0;
            merges = 0;
            selected = -1;
            runDamage = 0;
            runSpeed = 1f;
            runIncome = 0;
            runFrost = 0;
            adCoinsUsed = false;
            phase = Phase.Build;
            RepaintSlots();
        }

        private void StartWave()
        {
            if (phase != Phase.Build) return;
            selected = -1;
            adCoinsUsed = false;
            spawnLeft = BossWave ? 1 : 6 + wave * 2;
            spawnTimer = 0.2f;
            phase = Phase.Wave;
            RepaintSlots();
        }

        private void SpawnTick(float dt)
        {
            if (spawnLeft <= 0) return;
            spawnTimer -= dt;
            if (spawnTimer > 0f) return;
            SpawnEnemy(BossWave);
            spawnLeft--;
            spawnTimer = Mathf.Clamp(0.82f - wave * 0.025f, 0.18f, 0.82f);
        }

        private void SpawnEnemy(bool boss)
        {
            int hp = boss ? 75 + wave * 24 : 8 + wave * 4;
            float speed = boss ? 0.42f + wave * 0.01f : 0.86f + wave * 0.025f;
            int reward = boss ? 110 + wave * 10 : 8 + wave * 2;
            GameObject go = CircleObj(boss ? "BOSS" : "ENEMY", spawn, boss ? 0.82f : 0.42f, boss ? new Color(0.7f, 0.15f, 0.9f) : new Color(0.9f, 0.35f, 0.14f), 5);
            enemies.Add(new Enemy { Hp = hp, MaxHp = hp, Speed = speed, Reward = reward, Boss = boss, Go = go, Sr = go.GetComponent<SpriteRenderer>() });
        }

        private void EnemyTick(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e.Slow > 0f) e.Slow -= dt;
                float m = e.Slow > 0f ? Mathf.Clamp(0.55f - runFrost * 0.04f, 0.32f, 0.55f) : 1f;
                e.Go.transform.position = Vector2.MoveTowards(e.Go.transform.position, core, e.Speed * m * dt);
                if (Vector2.Distance(e.Go.transform.position, core) < 0.18f)
                {
                    baseHp -= e.Boss ? 5 : 1;
                    Destroy(e.Go);
                    enemies.RemoveAt(i);
                    if (baseHp <= 0) GameOver();
                }
            }
        }

        private void TowerTick(float dt)
        {
            foreach (Slot s in slots)
            {
                Tower t = s.Tower;
                if (t == null) continue;
                t.Cd -= dt;
                if (t.Cd > 0f) continue;
                Enemy e = Target(s.Pos, Range(t));
                if (e == null) continue;
                Hit(t, e);
                t.Cd = Delay(t);
            }
        }

        private Enemy Target(Vector2 pos, float range)
        {
            Enemy best = null;
            float bestDist = 999f;
            foreach (Enemy e in enemies)
            {
                float d = Vector2.Distance(pos, e.Go.transform.position);
                if (d > range) continue;
                float progress = Vector2.Distance(e.Go.transform.position, core);
                if (progress < bestDist)
                {
                    bestDist = progress;
                    best = e;
                }
            }
            return best;
        }

        private void Hit(Tower t, Enemy e)
        {
            e.Hp -= Damage(t);
            if (t.Type == TowerType.Frost)
            {
                e.Slow = 1.2f;
                e.Sr.color = new Color(0.55f, 0.9f, 1f);
            }
            if (t.Type == TowerType.Cannon && t.Level >= 3)
            {
                Splash(e.Go.transform.position, Mathf.RoundToInt(Damage(t) * 0.4f), 0.75f);
            }
            if (e.Hp <= 0) Kill(e);
            else e.Go.transform.localScale = Vector3.one * Mathf.Lerp(e.Boss ? 0.58f : 0.3f, e.Boss ? 0.82f : 0.42f, e.Hp / (float)e.MaxHp);
        }

        private void Splash(Vector2 p, int dmg, float r)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (Vector2.Distance(p, e.Go.transform.position) > r) continue;
                e.Hp -= dmg;
                if (e.Hp <= 0) Kill(e);
            }
        }

        private void Kill(Enemy e)
        {
            coins += Mathf.RoundToInt(e.Reward * (1f + runIncome / 100f));
            score += e.Boss ? 500 + wave * 40 : 35 + wave * 5;
            kills++;
            Destroy(e.Go);
            enemies.Remove(e);
        }

        private void EndWaveCheck()
        {
            if (phase == Phase.Wave && spawnLeft <= 0 && enemies.Count == 0)
            {
                coins += 20 + wave * 4;
                score += wave * 100;
                phase = Phase.Upgrade;
            }
        }

        private void GameOver()
        {
            ClearEnemies();
            phase = Phase.GameOver;
            int earned = Mathf.Max(1, wave / 2 + kills / 35);
            gems += earned;
            if (wave > bestWave) bestWave = wave;
            if (score > bestScore) bestScore = score;
            Save();
        }

        private void HandleTap()
        {
            bool down = false;
            Vector2 sp = default;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) { down = true; sp = Input.GetTouch(0).position; }
            if (Input.GetMouseButtonDown(0)) { down = true; sp = Input.mousePosition; }
            if (!down) return;
            Vector2 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10f));
            for (int i = 0; i < slots.Count; i++)
            {
                if (Vector2.Distance(w, slots[i].Pos) > 0.6f) continue;
                TapSlot(i);
                return;
            }
        }

        private void TapSlot(int i)
        {
            Slot s = slots[i];
            if (s.Tower == null)
            {
                if (coins < BuyCost) return;
                coins -= BuyCost;
                s.Tower = CreateTower(s.Pos, (TowerType)random.Next(4), 1);
                selected = i;
            }
            else if (selected < 0) selected = i;
            else if (selected == i) selected = -1;
            else if (CanMerge(slots[selected], s)) Merge(slots[selected], s);
            else selected = i;
            RepaintSlots();
        }

        private Tower CreateTower(Vector2 p, TowerType type, int level)
        {
            GameObject go = CircleObj(type.ToString(), p, 0.58f + level * 0.045f, TowerColor(type, level), 6);
            return new Tower { Type = type, Level = level, Go = go, Sr = go.GetComponent<SpriteRenderer>() };
        }

        private bool CanMerge(Slot a, Slot b)
        {
            return a.Tower != null && b.Tower != null && a.Tower.Type == b.Tower.Type && a.Tower.Level == b.Tower.Level && a.Tower.Level < MaxLevel;
        }

        private void Merge(Slot a, Slot b)
        {
            a.Tower.Level++;
            a.Tower.Go.transform.localScale = Vector3.one * (0.58f + a.Tower.Level * 0.045f);
            a.Tower.Sr.color = TowerColor(a.Tower.Type, a.Tower.Level);
            Destroy(b.Tower.Go);
            b.Tower = null;
            merges++;
            score += 25 * a.Tower.Level;
        }

        private int Damage(Tower t)
        {
            int baseDmg = t.Type switch { TowerType.Cannon => 5, TowerType.Rapid => 2, TowerType.Sniper => 11, TowerType.Frost => 3, _ => 3 };
            return Mathf.RoundToInt(baseDmg + t.Level * baseDmg * 0.75f + runDamage + metaDamage);
        }

        private float Range(Tower t)
        {
            return t.Type switch { TowerType.Cannon => 2.2f, TowerType.Rapid => 1.9f, TowerType.Sniper => 3.4f, TowerType.Frost => 2.4f, _ => 2.1f } + t.Level * 0.12f;
        }

        private float Delay(Tower t)
        {
            float d = t.Type switch { TowerType.Cannon => 0.75f, TowerType.Rapid => 0.32f, TowerType.Sniper => 1.35f, TowerType.Frost => 0.88f, _ => 0.7f };
            return Mathf.Max(0.08f, d * Mathf.Max(0.52f, 1f - t.Level * 0.055f) / runSpeed);
        }

        private Color TowerColor(TowerType type, int level)
        {
            Color c = type switch { TowerType.Cannon => new Color(0.25f, 0.63f, 1f), TowerType.Rapid => new Color(0.22f, 0.95f, 0.48f), TowerType.Sniper => new Color(1f, 0.78f, 0.24f), TowerType.Frost => new Color(0.48f, 0.9f, 1f), _ => Color.white };
            return Color.Lerp(c, Color.white, Mathf.Clamp01(level / 10f));
        }

        private int CountTowers()
        {
            int count = 0;
            foreach (Slot s in slots) if (s.Tower != null) count++;
            return count;
        }

        private void RepaintSlots()
        {
            for (int i = 0; i < slots.Count; i++) slots[i].Box.color = SlotColor(i == selected);
        }

        private Color SlotColor(bool sel)
        {
            return sel ? new Color(0.95f, 0.72f, 0.18f) : new Color(0.19f, 0.21f, 0.27f);
        }

        private void ClearEnemies()
        {
            foreach (Enemy e in enemies) if (e.Go != null) Destroy(e.Go);
            enemies.Clear();
            spawnLeft = 0;
        }

        private void ClearTowers()
        {
            foreach (Slot s in slots) { if (s.Tower != null && s.Tower.Go != null) Destroy(s.Tower.Go); s.Tower = null; }
        }

        private void Upgrade(int i)
        {
            if (i == 0) runDamage++;
            if (i == 1) runSpeed += 0.17f;
            if (i == 2) runIncome += 25;
            if (i == 3) { maxBaseHp += 3; baseHp = Mathf.Min(maxBaseHp, baseHp + 8); }
            if (i == 4) coins += 130;
            if (i == 5) runFrost++;
            wave++;
            phase = Phase.Build;
        }

        private void BuyMeta(int i)
        {
            int lvl = i == 0 ? metaCoins : i == 1 ? metaDamage : metaBase;
            int cost = 3 + lvl * 3;
            if (gems < cost) return;
            gems -= cost;
            if (i == 0) metaCoins++;
            if (i == 1) metaDamage++;
            if (i == 2) metaBase++;
            Save();
        }

        private void Load()
        {
            bestWave = PlayerPrefs.GetInt(SaveKey + "BestWave", 0);
            bestScore = PlayerPrefs.GetInt(SaveKey + "BestScore", 0);
            gems = PlayerPrefs.GetInt(SaveKey + "Gems", 0);
            metaCoins = PlayerPrefs.GetInt(SaveKey + "MetaCoins", 0);
            metaDamage = PlayerPrefs.GetInt(SaveKey + "MetaDamage", 0);
            metaBase = PlayerPrefs.GetInt(SaveKey + "MetaBase", 0);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKey + "BestWave", bestWave);
            PlayerPrefs.SetInt(SaveKey + "BestScore", bestScore);
            PlayerPrefs.SetInt(SaveKey + "Gems", gems);
            PlayerPrefs.SetInt(SaveKey + "MetaCoins", metaCoins);
            PlayerPrefs.SetInt(SaveKey + "MetaDamage", metaDamage);
            PlayerPrefs.SetInt(SaveKey + "MetaBase", metaBase);
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            int fs = Mathf.Clamp(Screen.height / 45, 16, 28);
            GUIStyle l = new GUIStyle(GUI.skin.label) { fontSize = fs, normal = { textColor = Color.white }, wordWrap = true };
            GUIStyle b = new GUIStyle(GUI.skin.button) { fontSize = fs, wordWrap = true };
            if (phase == Phase.Menu) Menu(l, b);
            else if (phase == Phase.Build) BuildUi(l, b);
            else if (phase == Phase.Wave) WaveUi(l);
            else if (phase == Phase.Upgrade) UpgradeUi(l, b);
            else if (phase == Phase.GameOver) GameOverUi(l, b);
            Labels(l);
        }

        private void Menu(GUIStyle l, GUIStyle b)
        {
            GUILayout.BeginArea(new Rect(30, 30, 520, 520), GUI.skin.box);
            GUILayout.Label("MergeDefenseSurvivor", l);
            GUILayout.Label("Best Wave " + bestWave + " | Best Score " + bestScore + " | Gems " + gems, l);
            if (GUILayout.Button("Neuer Run", b, GUILayout.Height(55))) NewRun();
            if (GUILayout.Button("Meta: Start-Coins +25 L" + metaCoins, b, GUILayout.Height(48))) BuyMeta(0);
            if (GUILayout.Button("Meta: Schaden +1 L" + metaDamage, b, GUILayout.Height(48))) BuyMeta(1);
            if (GUILayout.Button("Meta: Basisleben +4 L" + metaBase, b, GUILayout.Height(48))) BuyMeta(2);
            GUILayout.Label("MVP v0.1: Tower kaufen, gleiche Tower mergen, Upgrades wählen, Highscore speichern.", l);
            GUILayout.EndArea();
        }

        private void BuildUi(GUIStyle l, GUIStyle b)
        {
            GUILayout.BeginArea(new Rect(20, 20, 590, 195), GUI.skin.box);
            GUILayout.Label("Wave " + wave + (BossWave ? " BOSS" : "") + " | Base " + baseHp + "/" + maxBaseHp + " | Coins " + coins + " | Buy " + BuyCost, l);
            GUILayout.Label("Leerer Slot = kaufen. Zwei gleiche Tower = Merge.", l);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Welle starten", b, GUILayout.Height(55))) StartWave();
            if (GUILayout.Button(adCoinsUsed ? "Ad-Bonus genutzt" : "+80 Coins Ad-Stub", b, GUILayout.Height(55))) { if (!adCoinsUsed) { coins += 80; adCoinsUsed = true; } }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void WaveUi(GUIStyle l)
        {
            GUILayout.BeginArea(new Rect(20, 20, 560, 135), GUI.skin.box);
            GUILayout.Label("Wave " + wave + (BossWave ? " BOSS" : "") + " | Base " + baseHp + "/" + maxBaseHp + " | Score " + score, l);
            GUILayout.Label("Enemies " + enemies.Count + " | Spawn " + spawnLeft, l);
            GUILayout.EndArea();
        }

        private void UpgradeUi(GUIStyle l, GUIStyle b)
        {
            GUILayout.BeginArea(new Rect(30, 130, 590, 500), GUI.skin.box);
            GUILayout.Label("Upgrade wählen", l);
            if (GUILayout.Button("+1 Tower-Schaden", b, GUILayout.Height(62))) Upgrade(0);
            if (GUILayout.Button("+17% Angriffstempo", b, GUILayout.Height(62))) Upgrade(1);
            if (GUILayout.Button("+25% Coins", b, GUILayout.Height(62))) Upgrade(2);
            if (GUILayout.Button("Basis reparieren", b, GUILayout.Height(62))) Upgrade(3);
            if (GUILayout.Button("+130 Coins sofort", b, GUILayout.Height(62))) Upgrade(4);
            if (GUILayout.Button("Frost stärker", b, GUILayout.Height(62))) Upgrade(5);
            GUILayout.EndArea();
        }

        private void GameOverUi(GUIStyle l, GUIStyle b)
        {
            GUILayout.BeginArea(new Rect(35, 130, 520, 390), GUI.skin.box);
            GUILayout.Label("Game Over", l);
            GUILayout.Label("Wave " + wave + " | Score " + score + " | Kills " + kills + " | Merges " + merges, l);
            GUILayout.Label("Gems " + gems, l);
            if (GUILayout.Button("Neuer Run", b, GUILayout.Height(55))) NewRun();
            if (GUILayout.Button("Menü", b, GUILayout.Height(55))) phase = Phase.Menu;
            GUILayout.EndArea();
        }

        private void Labels(GUIStyle l)
        {
            foreach (Slot s in slots)
            {
                string txt = s.Tower == null ? "+" : s.Tower.Type.ToString()[0] + s.Tower.Level.ToString();
                WorldLabel(s.Pos, txt, l, 80, 24);
            }
            foreach (Enemy e in enemies)
            {
                WorldLabel(e.Go.transform.position + Vector3.up * 0.38f, e.Hp.ToString(), l, 100, 24);
            }
            WorldLabel(spawn + Vector2.up * 0.42f, "SPAWN", l, 120, 24);
            WorldLabel(core + Vector2.down * 0.42f, "BASE", l, 120, 24);
        }

        private void WorldLabel(Vector3 w, string text, GUIStyle l, float width, float height)
        {
            if (cam == null) return;
            Vector3 s = cam.WorldToScreenPoint(w);
            Rect r = new Rect(s.x - width * 0.5f, Screen.height - s.y - height * 0.5f, width, height);
            TextAnchor old = l.alignment;
            l.alignment = TextAnchor.MiddleCenter;
            GUI.Label(r, text, l);
            l.alignment = old;
        }
    }
}
