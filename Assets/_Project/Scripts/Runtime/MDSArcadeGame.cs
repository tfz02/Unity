using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSArcadeGame : MonoBehaviour
    {
        private enum Phase { Menu, Build, Wave, Upgrade, GameOver }
        private enum TowerType { Cannon, Rapid, Sniper, Frost }

        private sealed class Slot
        {
            public int Index;
            public Vector2 Pos;
            public SpriteRenderer Plate;
            public SpriteRenderer Glow;
            public Tower Tower;
        }

        private sealed class Tower
        {
            public TowerType Type;
            public int Level;
            public float Cooldown;
            public GameObject Root;
            public SpriteRenderer Body;
            public Transform Barrel;
        }

        private sealed class Enemy
        {
            public int Hp;
            public int MaxHp;
            public int Reward;
            public float Speed;
            public float Slow;
            public bool Boss;
            public GameObject Root;
            public SpriteRenderer Body;
            public Transform HealthFill;
        }

        private sealed class Shot
        {
            public Vector2 From;
            public Vector2 To;
            public float Life;
            public GameObject Root;
        }

        private sealed class Pop
        {
            public string Text;
            public Vector3 World;
            public float Life;
        }

        private const string SaveKey = "MDS_ARCADE_001_";
        private const int MaxTowerLevel = 6;

        private readonly List<Slot> slots = new();
        private readonly List<Enemy> enemies = new();
        private readonly List<Shot> shots = new();
        private readonly List<Pop> pops = new();

        private Camera cam;
        private Texture2D squareTex;
        private Texture2D circleTex;
        private Sprite square;
        private Sprite circle;
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
        private bool adBonusUsed;

        private int runDamage;
        private float runFireRate = 1f;
        private int runIncome;
        private int runSlow;

        private int bestWave;
        private int bestScore;
        private int gems;
        private int metaCoins;
        private int metaDamage;
        private int metaBase;

        private readonly Vector2 spawn = new(0f, 4.3f);
        private readonly Vector2 basePos = new(0f, -4.05f);
        private bool BossWave => wave > 0 && wave % 5 == 0;
        private int BuyCost => 28 + wave * 4 + TowerCount() * 4;

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
            SetupCamera();
            CreateSprites();
            CreateArena();
        }

        private void Update()
        {
            AnimateScene(Time.deltaTime);
            UpdateShots(Time.deltaTime);
            UpdatePops(Time.deltaTime);

            if (phase == Phase.Build)
            {
                HandleTap();
            }

            if (phase == Phase.Wave)
            {
                SpawnTick(Time.deltaTime);
                EnemyTick(Time.deltaTime);
                TowerTick(Time.deltaTime);
                CheckWaveEnd();
            }
        }

        private void OnDestroy()
        {
            Save();
            if (squareTex != null) Destroy(squareTex);
            if (circleTex != null) Destroy(circleTex);
        }

        private void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.025f, 0.03f, 0.045f);
        }

        private void CreateSprites()
        {
            squareTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            squareTex.SetPixel(0, 0, Color.white);
            squareTex.Apply();
            square = Sprite.Create(squareTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            circleTex = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            for (int y = 0; y < 96; y++)
            {
                for (int x = 0; x < 96; x++)
                {
                    float dx = x - 47.5f;
                    float dy = y - 47.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = d <= 45f ? 1f : 0f;
                    circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            circleTex.Apply();
            circle = Sprite.Create(circleTex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 96f);
        }

        private void CreateArena()
        {
            Rect("Sky Gradient A", new Vector2(0, 2.4f), new Vector2(8.2f, 5.8f), new Color(0.04f, 0.07f, 0.11f), -20);
            Rect("Sky Gradient B", new Vector2(0, -2.9f), new Vector2(8.2f, 5.0f), new Color(0.025f, 0.03f, 0.05f), -21);

            for (int i = -4; i <= 4; i++)
            {
                Rect("Grid V", new Vector2(i * 0.8f, 0), new Vector2(0.018f, 10.4f), new Color(0.08f, 0.13f, 0.17f, 1f), -18);
            }
            for (int i = -6; i <= 6; i++)
            {
                Rect("Grid H", new Vector2(0, i * 0.8f), new Vector2(8.1f, 0.018f), new Color(0.08f, 0.13f, 0.17f, 1f), -18);
            }

            Rect("Road Shadow", new Vector2(0.08f, 0.04f), new Vector2(1.08f, 8.35f), new Color(0.005f, 0.008f, 0.014f), -12);
            Rect("Road", new Vector2(0, 0.1f), new Vector2(0.86f, 8.35f), new Color(0.16f, 0.18f, 0.23f), -11);
            Rect("Road Light", new Vector2(0, 0.1f), new Vector2(0.18f, 8.1f), new Color(0.23f, 0.27f, 0.34f), -10);
            Rect("Road Edge L", new Vector2(-0.49f, 0.1f), new Vector2(0.06f, 8.35f), new Color(0.12f, 0.45f, 0.75f), -9);
            Rect("Road Edge R", new Vector2(0.49f, 0.1f), new Vector2(0.06f, 8.35f), new Color(0.12f, 0.45f, 0.75f), -9);

            Rect("Spawn Glow", spawn + Vector2.up * 0.02f, new Vector2(1.85f, 0.55f), new Color(0.06f, 0.45f, 0.24f), -5);
            Rect("Spawn Gate", spawn, new Vector2(1.55f, 0.38f), new Color(0.12f, 0.9f, 0.46f), 1);
            Rect("Base Glow", basePos + Vector2.down * 0.02f, new Vector2(2.55f, 0.82f), new Color(0.5f, 0.06f, 0.08f), -5);
            Rect("Base Core", basePos, new Vector2(2.2f, 0.62f), new Color(0.95f, 0.16f, 0.16f), 2);

            Vector2[] p =
            {
                new(-2.7f, 3.05f), new(2.7f, 3.05f),
                new(-2.7f, 1.78f), new(2.7f, 1.78f),
                new(-2.7f, 0.51f), new(2.7f, 0.51f),
                new(-2.7f, -0.76f), new(2.7f, -0.76f),
                new(-2.7f, -2.03f), new(2.7f, -2.03f),
                new(-2.7f, -3.3f), new(2.7f, -3.3f)
            };

            for (int i = 0; i < p.Length; i++)
            {
                GameObject glow = Circle("Slot Glow", p[i], 0.83f, new Color(0.05f, 0.26f, 0.42f), 0);
                GameObject plate = Rect("Tower Pad", p[i], new Vector2(0.95f, 0.95f), PadColor(false), 1);
                Circle("Pad Inner", p[i], 0.58f, new Color(0.11f, 0.14f, 0.2f), 2);
                slots.Add(new Slot { Index = i, Pos = p[i], Glow = glow.GetComponent<SpriteRenderer>(), Plate = plate.GetComponent<SpriteRenderer>() });
            }
        }

        private GameObject Rect(string name, Vector2 pos, Vector2 scale, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        private GameObject Circle(string name, Vector2 pos, float scale, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        private void StartRun()
        {
            ClearEnemies();
            ClearTowers();
            ClearShots();
            wave = 1;
            coins = 130 + metaCoins * 25;
            maxBaseHp = 22 + metaBase * 4;
            baseHp = maxBaseHp;
            score = 0;
            selected = -1;
            kills = 0;
            merges = 0;
            runDamage = 0;
            runFireRate = 1f;
            runIncome = 0;
            runSlow = 0;
            adBonusUsed = false;
            phase = Phase.Build;
            PaintSlots();
        }

        private void StartWave()
        {
            if (phase != Phase.Build) return;
            selected = -1;
            adBonusUsed = false;
            spawnLeft = BossWave ? 1 : 7 + wave * 2;
            spawnTimer = 0.15f;
            phase = Phase.Wave;
            PaintSlots();
        }

        private void SpawnTick(float dt)
        {
            if (spawnLeft <= 0) return;
            spawnTimer -= dt;
            if (spawnTimer > 0f) return;
            SpawnEnemy(BossWave);
            spawnLeft--;
            spawnTimer = Mathf.Clamp(0.76f - wave * 0.022f, 0.18f, 0.76f);
        }

        private void SpawnEnemy(bool boss)
        {
            int hp = boss ? 95 + wave * 25 : 10 + wave * 4;
            float speed = boss ? 0.38f + wave * 0.012f : 0.82f + wave * 0.025f;
            int reward = boss ? 130 + wave * 9 : 9 + wave * 2;

            GameObject root = new GameObject(boss ? "BOSS" : "Drone");
            root.transform.position = spawn;
            Circle("Enemy Shadow", spawn + new Vector2(0.08f, -0.08f), boss ? 0.9f : 0.5f, new Color(0f, 0f, 0f, 0.55f), 3).transform.SetParent(root.transform, true);
            GameObject body = Circle("Enemy Body", spawn, boss ? 0.82f : 0.42f, boss ? new Color(0.78f, 0.18f, 1f) : new Color(1f, 0.42f, 0.16f), 6);
            body.transform.SetParent(root.transform, true);
            Rect("Enemy HP BG", spawn + new Vector2(0, boss ? 0.62f : 0.38f), new Vector2(boss ? 0.98f : 0.58f, 0.07f), new Color(0.16f, 0.03f, 0.04f), 8).transform.SetParent(root.transform, true);
            GameObject fill = Rect("Enemy HP", spawn + new Vector2(0, boss ? 0.62f : 0.38f), new Vector2(boss ? 0.96f : 0.56f, 0.045f), new Color(0.3f, 1f, 0.35f), 9);
            fill.transform.SetParent(root.transform, true);

            enemies.Add(new Enemy { Hp = hp, MaxHp = hp, Speed = speed, Reward = reward, Boss = boss, Root = root, Body = body.GetComponent<SpriteRenderer>(), HealthFill = fill.transform });
        }

        private void EnemyTick(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e.Slow > 0f) e.Slow -= dt;
                float slowMul = e.Slow > 0f ? Mathf.Clamp(0.56f - runSlow * 0.04f, 0.34f, 0.56f) : 1f;
                e.Root.transform.position = Vector2.MoveTowards(e.Root.transform.position, basePos, e.Speed * slowMul * dt);
                float pulse = 1f + Mathf.Sin(Time.time * 10f + i) * 0.035f;
                e.Body.transform.localScale = Vector3.one * pulse;

                if (Vector2.Distance(e.Root.transform.position, basePos) < 0.18f)
                {
                    baseHp -= e.Boss ? 5 : 1;
                    PopText(e.Boss ? "BASE -5" : "BASE -1", basePos + Vector2.up * 0.6f);
                    Destroy(e.Root);
                    enemies.RemoveAt(i);
                    if (baseHp <= 0) FinishRun();
                }
            }
        }

        private void TowerTick(float dt)
        {
            foreach (Slot slot in slots)
            {
                Tower t = slot.Tower;
                if (t == null) continue;
                t.Cooldown -= dt;
                if (t.Cooldown > 0f) continue;

                Enemy target = PickTarget(slot.Pos, Range(t));
                if (target == null) continue;

                Vector2 targetPos = target.Root.transform.position;
                Aim(t, targetPos);
                Fire(t, target);
                t.Cooldown = Delay(t);
            }
        }

        private Enemy PickTarget(Vector2 from, float range)
        {
            Enemy best = null;
            float bestProgress = 999f;
            foreach (Enemy e in enemies)
            {
                float distance = Vector2.Distance(from, e.Root.transform.position);
                if (distance > range) continue;
                float progress = Vector2.Distance(e.Root.transform.position, basePos);
                if (progress < bestProgress)
                {
                    bestProgress = progress;
                    best = e;
                }
            }
            return best;
        }

        private void Aim(Tower tower, Vector2 target)
        {
            Vector2 pos = tower.Root.transform.position;
            Vector2 dir = target - pos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            tower.Barrel.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Fire(Tower tower, Enemy enemy)
        {
            int damage = Damage(tower);
            enemy.Hp -= damage;

            if (tower.Type == TowerType.Frost)
            {
                enemy.Slow = 1.35f;
                enemy.Body.color = new Color(0.55f, 0.9f, 1f);
            }

            CreateShot(tower.Root.transform.position, enemy.Root.transform.position, TowerColor(tower.Type, tower.Level));

            if (tower.Type == TowerType.Cannon && tower.Level >= 3)
            {
                Splash(enemy.Root.transform.position, Mathf.RoundToInt(damage * 0.38f), 0.75f);
            }

            if (enemy.Hp <= 0)
            {
                Kill(enemy);
            }
            else
            {
                float hpPercent = Mathf.Clamp01(enemy.Hp / (float)enemy.MaxHp);
                enemy.HealthFill.localScale = new Vector3(hpPercent, 1f, 1f);
            }
        }

        private void Splash(Vector2 center, int damage, float radius)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (Vector2.Distance(center, e.Root.transform.position) > radius) continue;
                e.Hp -= damage;
                if (e.Hp <= 0) Kill(e);
            }
        }

        private void Kill(Enemy enemy)
        {
            int reward = Mathf.RoundToInt(enemy.Reward * (1f + runIncome / 100f));
            coins += reward;
            score += enemy.Boss ? 550 + wave * 45 : 35 + wave * 6;
            kills++;
            PopText("+" + reward, enemy.Root.transform.position + Vector3.up * 0.35f);
            SpawnBurst(enemy.Root.transform.position, enemy.Boss ? 12 : 5, enemy.Boss ? new Color(0.8f, 0.2f, 1f) : new Color(1f, 0.55f, 0.22f));
            Destroy(enemy.Root);
            enemies.Remove(enemy);
        }

        private void CheckWaveEnd()
        {
            if (phase != Phase.Wave || spawnLeft > 0 || enemies.Count > 0) return;
            coins += 22 + wave * 4;
            score += wave * 120;
            phase = Phase.Upgrade;
            PopText("WAVE CLEAR", new Vector2(0f, 0.4f));
        }

        private void FinishRun()
        {
            ClearEnemies();
            ClearShots();
            phase = Phase.GameOver;
            int earned = Mathf.Max(1, wave / 2 + kills / 30);
            gems += earned;
            bestWave = Mathf.Max(bestWave, wave);
            bestScore = Mathf.Max(bestScore, score);
            Save();
        }

        private void HandleTap()
        {
            bool down = false;
            Vector2 screen = default;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                down = true;
                screen = Input.GetTouch(0).position;
            }
            if (Input.GetMouseButtonDown(0))
            {
                down = true;
                screen = Input.mousePosition;
            }
            if (!down) return;

            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            for (int i = 0; i < slots.Count; i++)
            {
                if (Vector2.Distance(world, slots[i].Pos) <= 0.62f)
                {
                    TapSlot(i);
                    return;
                }
            }
        }

        private void TapSlot(int index)
        {
            Slot s = slots[index];
            if (s.Tower == null)
            {
                if (coins < BuyCost)
                {
                    PopText("zu wenig Coins", s.Pos + Vector2.up * 0.55f);
                    return;
                }
                coins -= BuyCost;
                s.Tower = CreateTower(s.Pos, (TowerType)random.Next(4), 1);
                selected = index;
            }
            else if (selected < 0)
            {
                selected = index;
            }
            else if (selected == index)
            {
                selected = -1;
            }
            else if (CanMerge(slots[selected], s))
            {
                Merge(slots[selected], s);
            }
            else
            {
                selected = index;
            }
            PaintSlots();
        }

        private Tower CreateTower(Vector2 pos, TowerType type, int level)
        {
            GameObject root = new GameObject(type + " L" + level);
            root.transform.position = pos;
            Circle("Tower Glow", pos, 0.72f, new Color(0.06f, 0.18f, 0.28f), 4).transform.SetParent(root.transform, true);
            Circle("Tower Body", pos, 0.46f + level * 0.045f, TowerColor(type, level), 7).transform.SetParent(root.transform, true);
            GameObject barrel = Rect("Barrel", pos + Vector2.right * 0.28f, new Vector2(0.55f, 0.13f), new Color(0.9f, 0.95f, 1f), 8);
            barrel.transform.SetParent(root.transform, true);
            return new Tower { Type = type, Level = level, Root = root, Body = root.transform.Find("Tower Body").GetComponent<SpriteRenderer>(), Barrel = barrel.transform };
        }

        private bool CanMerge(Slot a, Slot b)
        {
            return a.Tower != null && b.Tower != null && a.Tower.Type == b.Tower.Type && a.Tower.Level == b.Tower.Level && a.Tower.Level < MaxTowerLevel;
        }

        private void Merge(Slot keep, Slot remove)
        {
            keep.Tower.Level++;
            keep.Tower.Root.name = keep.Tower.Type + " L" + keep.Tower.Level;
            keep.Tower.Body.color = TowerColor(keep.Tower.Type, keep.Tower.Level);
            keep.Tower.Body.transform.localScale = Vector3.one * (1f + keep.Tower.Level * 0.09f);
            Destroy(remove.Tower.Root);
            remove.Tower = null;
            merges++;
            score += keep.Tower.Level * 30;
            selected = keep.Index;
            PopText("MERGE L" + keep.Tower.Level, keep.Pos + Vector2.up * 0.72f);
            SpawnBurst(keep.Pos, 8, TowerColor(keep.Tower.Type, keep.Tower.Level));
        }

        private int Damage(Tower t)
        {
            int baseDamage = t.Type switch { TowerType.Cannon => 5, TowerType.Rapid => 2, TowerType.Sniper => 11, TowerType.Frost => 3, _ => 3 };
            return Mathf.RoundToInt(baseDamage + baseDamage * t.Level * 0.78f + runDamage + metaDamage);
        }

        private float Range(Tower t)
        {
            return t.Type switch { TowerType.Cannon => 2.25f, TowerType.Rapid => 1.95f, TowerType.Sniper => 3.55f, TowerType.Frost => 2.45f, _ => 2.2f } + t.Level * 0.12f;
        }

        private float Delay(Tower t)
        {
            float baseDelay = t.Type switch { TowerType.Cannon => 0.72f, TowerType.Rapid => 0.3f, TowerType.Sniper => 1.32f, TowerType.Frost => 0.85f, _ => 0.7f };
            return Mathf.Max(0.08f, baseDelay * Mathf.Max(0.52f, 1f - t.Level * 0.055f) / runFireRate);
        }

        private Color TowerColor(TowerType type, int level)
        {
            Color c = type switch
            {
                TowerType.Cannon => new Color(0.2f, 0.63f, 1f),
                TowerType.Rapid => new Color(0.22f, 1f, 0.48f),
                TowerType.Sniper => new Color(1f, 0.78f, 0.22f),
                TowerType.Frost => new Color(0.48f, 0.9f, 1f),
                _ => Color.white
            };
            return Color.Lerp(c, Color.white, Mathf.Clamp01(level / 9f));
        }

        private void CreateShot(Vector2 from, Vector2 to, Color color)
        {
            GameObject go = Rect("Shot", from, new Vector2(0.12f, 0.12f), color, 12);
            shots.Add(new Shot { From = from, To = to, Life = 0.14f, Root = go });
        }

        private void UpdateShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot s = shots[i];
                s.Life -= dt;
                float t = 1f - Mathf.Clamp01(s.Life / 0.14f);
                if (s.Root != null) s.Root.transform.position = Vector2.Lerp(s.From, s.To, t);
                if (s.Life <= 0f)
                {
                    if (s.Root != null) Destroy(s.Root);
                    shots.RemoveAt(i);
                }
            }
        }

        private void SpawnBurst(Vector2 pos, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.35f;
                GameObject p = Circle("Spark", pos + offset, 0.08f, color, 14);
                Destroy(p, 0.45f);
            }
        }

        private void PopText(string text, Vector3 world)
        {
            pops.Add(new Pop { Text = text, World = world, Life = 1.0f });
        }

        private void UpdatePops(float dt)
        {
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                pops[i].Life -= dt;
                pops[i].World += Vector3.up * dt * 0.55f;
                if (pops[i].Life <= 0f) pops.RemoveAt(i);
            }
        }

        private void ClearEnemies()
        {
            foreach (Enemy e in enemies) if (e.Root != null) Destroy(e.Root);
            enemies.Clear();
            spawnLeft = 0;
        }

        private void ClearTowers()
        {
            foreach (Slot s in slots)
            {
                if (s.Tower != null && s.Tower.Root != null) Destroy(s.Tower.Root);
                s.Tower = null;
            }
        }

        private void ClearShots()
        {
            foreach (Shot s in shots) if (s.Root != null) Destroy(s.Root);
            shots.Clear();
        }

        private int TowerCount()
        {
            int count = 0;
            foreach (Slot s in slots) if (s.Tower != null) count++;
            return count;
        }

        private void Upgrade(int choice)
        {
            if (choice == 0) runDamage++;
            if (choice == 1) runFireRate += 0.18f;
            if (choice == 2) runIncome += 25;
            if (choice == 3) { maxBaseHp += 3; baseHp = Mathf.Min(maxBaseHp, baseHp + 8); }
            if (choice == 4) coins += 140;
            if (choice == 5) runSlow++;
            wave++;
            phase = Phase.Build;
        }

        private void BuyMeta(int choice)
        {
            int level = choice == 0 ? metaCoins : choice == 1 ? metaDamage : metaBase;
            int cost = 3 + level * 3;
            if (gems < cost) return;
            gems -= cost;
            if (choice == 0) metaCoins++;
            if (choice == 1) metaDamage++;
            if (choice == 2) metaBase++;
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

        private Color PadColor(bool active)
        {
            return active ? new Color(0.95f, 0.7f, 0.18f) : new Color(0.13f, 0.17f, 0.24f);
        }

        private void PaintSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i == selected;
                slots[i].Plate.color = PadColor(active);
                slots[i].Glow.color = active ? new Color(0.9f, 0.55f, 0.15f) : new Color(0.05f, 0.26f, 0.42f);
            }
        }

        private void AnimateScene(float dt)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                float p = 1f + Mathf.Sin(Time.time * 2f + i) * 0.015f;
                if (slots[i].Glow != null) slots[i].Glow.transform.localScale = Vector3.one * (0.83f * p);
            }
        }

        private void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.25f);
            int font = Mathf.RoundToInt(22 * scale);
            GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = font, normal = { textColor = Color.white }, wordWrap = true };
            GUIStyle title = new GUIStyle(label) { fontSize = Mathf.RoundToInt(34 * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = font, wordWrap = true };

            if (phase == Phase.Menu) DrawMenu(title, label, button, scale);
            if (phase == Phase.Build) DrawBuild(label, button, scale);
            if (phase == Phase.Wave) DrawWave(label, scale);
            if (phase == Phase.Upgrade) DrawUpgrade(title, button, scale);
            if (phase == Phase.GameOver) DrawGameOver(title, label, button, scale);
            DrawWorldLabels(label);
        }

        private void DrawMenu(GUIStyle title, GUIStyle label, GUIStyle button, float scale)
        {
            float w = 560 * scale;
            float h = 520 * scale;
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, 38 * scale, w, h), GUI.skin.box);
            GUILayout.Label("MERGE DEFENSE", title, GUILayout.Height(54 * scale));
            GUILayout.Label("SURVIVOR", title, GUILayout.Height(46 * scale));
            GUILayout.Space(10 * scale);
            GUILayout.Label("Best Wave " + bestWave + "  |  Best Score " + bestScore + "  |  Gems " + gems, label);
            if (GUILayout.Button("NEUER RUN", button, GUILayout.Height(62 * scale))) StartRun();
            if (GUILayout.Button("Meta: Start-Coins +25  L" + metaCoins, button, GUILayout.Height(48 * scale))) BuyMeta(0);
            if (GUILayout.Button("Meta: Schaden +1  L" + metaDamage, button, GUILayout.Height(48 * scale))) BuyMeta(1);
            if (GUILayout.Button("Meta: Basisleben +4  L" + metaBase, button, GUILayout.Height(48 * scale))) BuyMeta(2);
            GUILayout.Space(8 * scale);
            GUILayout.Label("Kaufe Tower, merge gleiche Typen, ueberlebe Bosswellen und sammle Gems fuer permanente Upgrades.", label);
            GUILayout.EndArea();
        }

        private void DrawBuild(GUIStyle label, GUIStyle button, float scale)
        {
            DrawHud(label, scale);
            float w = 650 * scale;
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, Screen.height - 168 * scale, w, 150 * scale), GUI.skin.box);
            GUILayout.Label("Leerer Slot = Tower kaufen. Zwei gleiche Tower antippen = Merge.", label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("WELLE STARTEN", button, GUILayout.Height(58 * scale))) StartWave();
            if (GUILayout.Button(adBonusUsed ? "AD-BONUS GENUTZT" : "+80 COINS AD-STUB", button, GUILayout.Height(58 * scale)))
            {
                if (!adBonusUsed) { coins += 80; adBonusUsed = true; }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawWave(GUIStyle label, float scale)
        {
            DrawHud(label, scale);
            GUILayout.BeginArea(new Rect(18 * scale, Screen.height - 100 * scale, 420 * scale, 82 * scale), GUI.skin.box);
            GUILayout.Label("Enemies " + enemies.Count + " | Spawn " + spawnLeft, label);
            GUILayout.Label("Tower feuern automatisch.", label);
            GUILayout.EndArea();
        }

        private void DrawUpgrade(GUIStyle title, GUIStyle button, float scale)
        {
            float w = 620 * scale;
            float h = 520 * scale;
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h), GUI.skin.box);
            GUILayout.Label("UPGRADE WAEHLEN", title, GUILayout.Height(58 * scale));
            if (GUILayout.Button("+1 Tower-Schaden", button, GUILayout.Height(62 * scale))) Upgrade(0);
            if (GUILayout.Button("+18% Angriffstempo", button, GUILayout.Height(62 * scale))) Upgrade(1);
            if (GUILayout.Button("+25% Coins", button, GUILayout.Height(62 * scale))) Upgrade(2);
            if (GUILayout.Button("Basis reparieren", button, GUILayout.Height(62 * scale))) Upgrade(3);
            if (GUILayout.Button("+140 Coins sofort", button, GUILayout.Height(62 * scale))) Upgrade(4);
            if (GUILayout.Button("Frost staerker", button, GUILayout.Height(62 * scale))) Upgrade(5);
            GUILayout.EndArea();
        }

        private void DrawGameOver(GUIStyle title, GUIStyle label, GUIStyle button, float scale)
        {
            float w = 560 * scale;
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, 140 * scale, w, 390 * scale), GUI.skin.box);
            GUILayout.Label("GAME OVER", title, GUILayout.Height(65 * scale));
            GUILayout.Label("Wave " + wave + " | Score " + score + " | Kills " + kills + " | Merges " + merges, label);
            GUILayout.Label("Gems " + gems, label);
            if (GUILayout.Button("NEUER RUN", button, GUILayout.Height(58 * scale))) StartRun();
            if (GUILayout.Button("HAUPTMENUE", button, GUILayout.Height(58 * scale))) phase = Phase.Menu;
            GUILayout.EndArea();
        }

        private void DrawHud(GUIStyle label, float scale)
        {
            GUILayout.BeginArea(new Rect(18 * scale, 12 * scale, 510 * scale, 112 * scale), GUI.skin.box);
            GUILayout.Label("Wave " + wave + (BossWave ? "  BOSS" : "") + " | Score " + score, label);
            GUILayout.Label("Base " + baseHp + "/" + maxBaseHp + " | Coins " + coins + " | Buy " + BuyCost, label);
            GUILayout.Label("DMG +" + (runDamage + metaDamage) + " | Speed x" + runFireRate.ToString("0.00") + " | Income +" + runIncome + "%", label);
            GUILayout.EndArea();
        }

        private void DrawWorldLabels(GUIStyle style)
        {
            WorldLabel(spawn + Vector2.up * 0.45f, "SPAWN", style, 150, 28);
            WorldLabel(basePos + Vector2.down * 0.44f, "BASE", style, 150, 28);

            foreach (Slot s in slots)
            {
                string txt = s.Tower == null ? "+" : s.Tower.Type.ToString()[0] + s.Tower.Level.ToString();
                WorldLabel(s.Pos, txt, style, 80, 28);
            }

            foreach (Pop pop in pops)
            {
                WorldLabel(pop.World, pop.Text, style, 180, 28);
            }
        }

        private void WorldLabel(Vector3 world, string text, GUIStyle style, float width, float height)
        {
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(world);
            Rect rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - height * 0.5f, width, height);
            TextAnchor old = style.alignment;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, text, style);
            style.alignment = old;
        }
    }
}
