using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSModern3DGame : MonoBehaviour
    {
        private enum Phase { Menu, Build, Wave, Upgrade, GameOver }
        private enum TowerType { Cannon, Rapid, Sniper, Frost }

        private sealed class Slot
        {
            public int Index;
            public Vector3 Position;
            public GameObject Pad;
            public Tower Tower;
        }

        private sealed class Tower
        {
            public TowerType Type;
            public int Level;
            public float Cooldown;
            public GameObject Root;
            public Transform Head;
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
            public Transform HealthFill;
        }

        private sealed class Projectile
        {
            public GameObject Root;
            public Vector3 From;
            public Vector3 To;
            public float Life;
            public float MaxLife;
        }

        private sealed class FloatingText
        {
            public string Text;
            public Vector3 World;
            public float Life;
        }

        private const string SaveKey = "MDS_3D_001_";
        private const int MaxTowerLevel = 6;

        private readonly List<Slot> slots = new();
        private readonly List<Enemy> enemies = new();
        private readonly List<Projectile> projectiles = new();
        private readonly List<FloatingText> floatingTexts = new();
        private readonly Dictionary<string, Material> materials = new();

        private Camera cam;
        private System.Random random;

        private Phase phase = Phase.Menu;
        private int wave;
        private int coins;
        private int baseHp;
        private int maxBaseHp;
        private int score;
        private int selectedSlot = -1;
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

        private readonly Vector3 spawnPoint = new(0f, 0.15f, 4.7f);
        private readonly Vector3 basePoint = new(0f, 0.15f, -4.45f);

        private bool BossWave => wave > 0 && wave % 5 == 0;
        private int BuyCost => 30 + wave * 4 + TowerCount() * 4;

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
            SetupLighting();
            CreateMaterials();
            CreateWorld();
        }

        private void Update()
        {
            Animate(Time.deltaTime);
            UpdateProjectiles(Time.deltaTime);
            UpdateFloatingText(Time.deltaTime);

            if (phase == Phase.Build)
            {
                HandleInput();
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
        }

        private void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cam = cameraObject.AddComponent<Camera>();
            }

            cam.transform.position = new Vector3(0f, 8.8f, -9.7f);
            cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = 5.9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.67f, 0.76f, 0.84f);
        }

        private void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                light.intensity = 0.75f;
            }

            GameObject keyLight = new GameObject("Key Light");
            Light key = keyLight.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.35f;
            key.color = new Color(1f, 0.96f, 0.88f);
            keyLight.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

            GameObject fillLight = new GameObject("Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.42f;
            fill.color = new Color(0.75f, 0.85f, 1f);
            fillLight.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
        }

        private void CreateMaterials()
        {
            AddMat("ground", new Color(0.38f, 0.51f, 0.42f));
            AddMat("groundDark", new Color(0.29f, 0.40f, 0.34f));
            AddMat("path", new Color(0.47f, 0.48f, 0.45f));
            AddMat("pathLine", new Color(0.78f, 0.72f, 0.58f));
            AddMat("pad", new Color(0.28f, 0.31f, 0.34f));
            AddMat("padSelected", new Color(0.95f, 0.72f, 0.32f));
            AddMat("base", new Color(0.36f, 0.48f, 0.63f));
            AddMat("baseCore", new Color(0.92f, 0.28f, 0.22f));
            AddMat("spawn", new Color(0.22f, 0.62f, 0.33f));
            AddMat("enemy", new Color(0.78f, 0.24f, 0.16f));
            AddMat("boss", new Color(0.52f, 0.27f, 0.72f));
            AddMat("hp", new Color(0.25f, 0.82f, 0.32f));
            AddMat("hpBack", new Color(0.18f, 0.08f, 0.07f));
            AddMat("metal", new Color(0.62f, 0.66f, 0.69f));
            AddMat("darkMetal", new Color(0.18f, 0.22f, 0.26f));
            AddMat("cannon", new Color(0.25f, 0.46f, 0.72f));
            AddMat("rapid", new Color(0.18f, 0.58f, 0.35f));
            AddMat("sniper", new Color(0.74f, 0.55f, 0.22f));
            AddMat("frost", new Color(0.40f, 0.72f, 0.86f));
            AddMat("projectile", new Color(1f, 0.80f, 0.28f));
        }

        private void AddMat(string key, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            materials[key] = material;
        }

        private void CreateWorld()
        {
            Cube("Ground", new Vector3(0f, -0.06f, 0f), new Vector3(8.8f, 0.12f, 10.8f), "ground");
            Cube("Ground Backplate", new Vector3(0f, -0.09f, 0f), new Vector3(9.3f, 0.08f, 11.3f), "groundDark");

            Cube("Road", new Vector3(0f, 0.01f, 0f), new Vector3(0.95f, 0.10f, 9.35f), "path");
            Cube("Road Center Mark", new Vector3(0f, 0.08f, 0f), new Vector3(0.12f, 0.04f, 8.6f), "pathLine");
            Cube("Road Left Edge", new Vector3(-0.56f, 0.09f, 0f), new Vector3(0.07f, 0.04f, 9.35f), "darkMetal");
            Cube("Road Right Edge", new Vector3(0.56f, 0.09f, 0f), new Vector3(0.07f, 0.04f, 9.35f), "darkMetal");

            Cube("Spawn Platform", new Vector3(0f, 0.12f, 4.82f), new Vector3(1.9f, 0.25f, 0.65f), "spawn");
            Cube("Base Platform", new Vector3(0f, 0.14f, -4.62f), new Vector3(2.55f, 0.28f, 0.78f), "base");
            Cube("Base Core", new Vector3(0f, 0.42f, -4.62f), new Vector3(1.55f, 0.48f, 0.42f), "baseCore");
            Cube("Base Tower L", new Vector3(-0.82f, 0.62f, -4.62f), new Vector3(0.28f, 0.82f, 0.35f), "base");
            Cube("Base Tower R", new Vector3(0.82f, 0.62f, -4.62f), new Vector3(0.28f, 0.82f, 0.35f), "base");

            Vector3[] slotPositions =
            {
                new(-2.75f, 0.08f, 3.25f), new(2.75f, 0.08f, 3.25f),
                new(-2.75f, 0.08f, 1.95f), new(2.75f, 0.08f, 1.95f),
                new(-2.75f, 0.08f, 0.65f), new(2.75f, 0.08f, 0.65f),
                new(-2.75f, 0.08f, -0.65f), new(2.75f, 0.08f, -0.65f),
                new(-2.75f, 0.08f, -1.95f), new(2.75f, 0.08f, -1.95f),
                new(-2.75f, 0.08f, -3.25f), new(2.75f, 0.08f, -3.25f)
            };

            for (int i = 0; i < slotPositions.Length; i++)
            {
                GameObject pad = Cylinder("Build Pad", slotPositions[i], new Vector3(0.82f, 0.08f, 0.82f), "pad");
                slots.Add(new Slot { Index = i, Position = slotPositions[i], Pad = pad });
            }
        }

        private GameObject Cube(string name, Vector3 position, Vector3 scale, string matKey)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            SetMat(go, matKey);
            return go;
        }

        private GameObject Sphere(string name, Vector3 position, Vector3 scale, string matKey)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            SetMat(go, matKey);
            return go;
        }

        private GameObject Cylinder(string name, Vector3 position, Vector3 scale, string matKey)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            SetMat(go, matKey);
            return go;
        }

        private void SetMat(GameObject go, string matKey)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && materials.TryGetValue(matKey, out Material material))
            {
                renderer.sharedMaterial = material;
            }
        }

        private void StartRun()
        {
            ClearEnemies();
            ClearTowers();
            ClearProjectiles();
            wave = 1;
            coins = 130 + metaCoins * 25;
            maxBaseHp = 22 + metaBase * 4;
            baseHp = maxBaseHp;
            score = 0;
            selectedSlot = -1;
            spawnLeft = 0;
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
            selectedSlot = -1;
            adBonusUsed = false;
            spawnLeft = BossWave ? 1 : 7 + wave * 2;
            spawnTimer = 0.12f;
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
            spawnTimer = Mathf.Clamp(0.78f - wave * 0.024f, 0.18f, 0.78f);
        }

        private void SpawnEnemy(bool boss)
        {
            GameObject root = new GameObject(boss ? "Boss Drone" : "Enemy Drone");
            root.transform.position = spawnPoint;

            if (boss)
            {
                Sphere("Boss Body", spawnPoint + Vector3.up * 0.55f, new Vector3(0.75f, 0.55f, 0.75f), "boss").transform.SetParent(root.transform, true);
                Cube("Boss Armor", spawnPoint + Vector3.up * 0.55f, new Vector3(0.95f, 0.22f, 0.95f), "darkMetal").transform.SetParent(root.transform, true);
            }
            else
            {
                Sphere("Enemy Body", spawnPoint + Vector3.up * 0.35f, new Vector3(0.45f, 0.35f, 0.45f), "enemy").transform.SetParent(root.transform, true);
                Cube("Enemy Wing L", spawnPoint + new Vector3(-0.34f, 0.33f, 0f), new Vector3(0.32f, 0.08f, 0.18f), "darkMetal").transform.SetParent(root.transform, true);
                Cube("Enemy Wing R", spawnPoint + new Vector3(0.34f, 0.33f, 0f), new Vector3(0.32f, 0.08f, 0.18f), "darkMetal").transform.SetParent(root.transform, true);
            }

            GameObject hpBack = Cube("HP Back", spawnPoint + new Vector3(0f, boss ? 1.05f : 0.78f, -0.05f), new Vector3(boss ? 0.95f : 0.62f, 0.06f, 0.06f), "hpBack");
            hpBack.transform.SetParent(root.transform, true);
            GameObject hpFill = Cube("HP Fill", spawnPoint + new Vector3(0f, boss ? 1.055f : 0.785f, -0.05f), new Vector3(boss ? 0.9f : 0.58f, 0.07f, 0.07f), "hp");
            hpFill.transform.SetParent(root.transform, true);

            int hp = boss ? 100 + wave * 28 : 10 + wave * 4;
            float speed = boss ? 0.42f + wave * 0.01f : 0.86f + wave * 0.025f;
            int reward = boss ? 135 + wave * 12 : 9 + wave * 2;
            enemies.Add(new Enemy { Hp = hp, MaxHp = hp, Reward = reward, Speed = speed, Boss = boss, Root = root, HealthFill = hpFill.transform });
        }

        private void EnemyTick(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy.Slow > 0f) enemy.Slow -= dt;
                float slowMul = enemy.Slow > 0f ? Mathf.Clamp(0.56f - runSlow * 0.04f, 0.34f, 0.56f) : 1f;
                enemy.Root.transform.position = Vector3.MoveTowards(enemy.Root.transform.position, basePoint, enemy.Speed * slowMul * dt);
                enemy.Root.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 4f + i) * 8f, 0f);

                if (Vector3.Distance(enemy.Root.transform.position, basePoint) < 0.18f)
                {
                    baseHp -= enemy.Boss ? 5 : 1;
                    FloatText(enemy.Boss ? "BASE -5" : "BASE -1", basePoint + Vector3.up * 1.0f);
                    Destroy(enemy.Root);
                    enemies.RemoveAt(i);
                    if (baseHp <= 0) FinishRun();
                }
            }
        }

        private void TowerTick(float dt)
        {
            foreach (Slot slot in slots)
            {
                Tower tower = slot.Tower;
                if (tower == null) continue;
                tower.Cooldown -= dt;
                if (tower.Cooldown > 0f) continue;

                Enemy target = PickTarget(slot.Position, Range(tower));
                if (target == null) continue;

                Aim(tower, target.Root.transform.position);
                Fire(tower, target);
                tower.Cooldown = Delay(tower);
            }
        }

        private Enemy PickTarget(Vector3 from, float range)
        {
            Enemy best = null;
            float bestProgress = 999f;
            foreach (Enemy enemy in enemies)
            {
                float distance = Vector3.Distance(from, enemy.Root.transform.position);
                if (distance > range) continue;
                float progress = Vector3.Distance(enemy.Root.transform.position, basePoint);
                if (progress < bestProgress)
                {
                    bestProgress = progress;
                    best = enemy;
                }
            }
            return best;
        }

        private void Aim(Tower tower, Vector3 target)
        {
            Vector3 look = target - tower.Head.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                tower.Head.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }
        }

        private void Fire(Tower tower, Enemy enemy)
        {
            int damage = Damage(tower);
            enemy.Hp -= damage;

            if (tower.Type == TowerType.Frost)
            {
                enemy.Slow = 1.3f;
                SetChildMaterials(enemy.Root, "frost");
            }

            CreateProjectile(tower.Barrel.position, enemy.Root.transform.position + Vector3.up * 0.45f, tower.Type);

            if (tower.Type == TowerType.Cannon && tower.Level >= 3)
            {
                Splash(enemy.Root.transform.position, Mathf.RoundToInt(damage * 0.38f), 0.85f);
            }

            if (enemy.Hp <= 0)
            {
                Kill(enemy);
                return;
            }

            float hp = Mathf.Clamp01(enemy.Hp / (float)enemy.MaxHp);
            enemy.HealthFill.localScale = new Vector3(hp, 1f, 1f);
        }

        private void Splash(Vector3 center, int damage, float radius)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (Vector3.Distance(center, enemy.Root.transform.position) > radius) continue;
                enemy.Hp -= damage;
                if (enemy.Hp <= 0) Kill(enemy);
            }
        }

        private void Kill(Enemy enemy)
        {
            int reward = Mathf.RoundToInt(enemy.Reward * (1f + runIncome / 100f));
            coins += reward;
            score += enemy.Boss ? 550 + wave * 45 : 35 + wave * 6;
            kills++;
            FloatText("+" + reward, enemy.Root.transform.position + Vector3.up * 1.0f);
            Burst(enemy.Root.transform.position + Vector3.up * 0.45f, enemy.Boss ? 12 : 5);
            Destroy(enemy.Root);
            enemies.Remove(enemy);
        }

        private void CheckWaveEnd()
        {
            if (phase != Phase.Wave || spawnLeft > 0 || enemies.Count > 0) return;
            coins += 24 + wave * 4;
            score += wave * 120;
            phase = Phase.Upgrade;
            FloatText("WAVE CLEAR", new Vector3(0f, 1.0f, 0f));
        }

        private void FinishRun()
        {
            ClearEnemies();
            ClearProjectiles();
            phase = Phase.GameOver;
            int earned = Mathf.Max(1, wave / 2 + kills / 30);
            gems += earned;
            bestWave = Mathf.Max(bestWave, wave);
            bestScore = Mathf.Max(bestScore, score);
            Save();
        }

        private void HandleInput()
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

            Ray ray = cam.ScreenPointToRay(screen);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float enter)) return;
            Vector3 world = ray.GetPoint(enter);

            for (int i = 0; i < slots.Count; i++)
            {
                if (Vector3.Distance(world, slots[i].Position) <= 0.72f)
                {
                    TapSlot(i);
                    return;
                }
            }
        }

        private void TapSlot(int index)
        {
            Slot slot = slots[index];
            if (slot.Tower == null)
            {
                if (coins < BuyCost)
                {
                    FloatText("zu wenig Coins", slot.Position + Vector3.up * 1.0f);
                    return;
                }
                coins -= BuyCost;
                slot.Tower = CreateTower(slot.Position, (TowerType)random.Next(4), 1);
                selectedSlot = index;
            }
            else if (selectedSlot < 0)
            {
                selectedSlot = index;
            }
            else if (selectedSlot == index)
            {
                selectedSlot = -1;
            }
            else if (CanMerge(slots[selectedSlot], slot))
            {
                Merge(slots[selectedSlot], slot);
            }
            else
            {
                selectedSlot = index;
            }
            PaintSlots();
        }

        private Tower CreateTower(Vector3 position, TowerType type, int level)
        {
            GameObject root = new GameObject(type + " Tower L" + level);
            root.transform.position = position;

            Cylinder("Tower Base", position + Vector3.up * 0.16f, new Vector3(0.56f, 0.16f, 0.56f), "darkMetal").transform.SetParent(root.transform, true);
            GameObject body = Cube("Tower Body", position + Vector3.up * 0.52f, new Vector3(0.48f, 0.58f, 0.48f), TowerMat(type));
            body.transform.SetParent(root.transform, true);
            GameObject head = Cylinder("Tower Head", position + Vector3.up * 0.94f, new Vector3(0.34f, 0.18f, 0.34f), TowerMat(type));
            head.transform.SetParent(root.transform, true);
            GameObject barrel = Cube("Tower Barrel", position + new Vector3(0f, 0.96f, 0.42f), new Vector3(0.16f, 0.14f, 0.70f), "metal");
            barrel.transform.SetParent(head.transform, true);

            return new Tower { Type = type, Level = level, Root = root, Head = head.transform, Barrel = barrel.transform };
        }

        private string TowerMat(TowerType type)
        {
            return type switch
            {
                TowerType.Cannon => "cannon",
                TowerType.Rapid => "rapid",
                TowerType.Sniper => "sniper",
                TowerType.Frost => "frost",
                _ => "metal"
            };
        }

        private bool CanMerge(Slot first, Slot second)
        {
            return first.Tower != null && second.Tower != null
                && first.Tower.Type == second.Tower.Type
                && first.Tower.Level == second.Tower.Level
                && first.Tower.Level < MaxTowerLevel;
        }

        private void Merge(Slot keep, Slot remove)
        {
            keep.Tower.Level++;
            keep.Tower.Root.name = keep.Tower.Type + " Tower L" + keep.Tower.Level;
            keep.Tower.Root.transform.localScale = Vector3.one * (1f + keep.Tower.Level * 0.08f);
            Destroy(remove.Tower.Root);
            remove.Tower = null;
            selectedSlot = keep.Index;
            merges++;
            score += keep.Tower.Level * 30;
            FloatText("MERGE L" + keep.Tower.Level, keep.Position + Vector3.up * 1.2f);
            Burst(keep.Position + Vector3.up * 0.6f, 8);
        }

        private int Damage(Tower tower)
        {
            int baseDamage = tower.Type switch { TowerType.Cannon => 5, TowerType.Rapid => 2, TowerType.Sniper => 11, TowerType.Frost => 3, _ => 3 };
            return Mathf.RoundToInt(baseDamage + baseDamage * tower.Level * 0.78f + runDamage + metaDamage);
        }

        private float Range(Tower tower)
        {
            return tower.Type switch { TowerType.Cannon => 2.3f, TowerType.Rapid => 2.0f, TowerType.Sniper => 3.7f, TowerType.Frost => 2.5f, _ => 2.2f } + tower.Level * 0.12f;
        }

        private float Delay(Tower tower)
        {
            float baseDelay = tower.Type switch { TowerType.Cannon => 0.72f, TowerType.Rapid => 0.30f, TowerType.Sniper => 1.32f, TowerType.Frost => 0.85f, _ => 0.7f };
            return Mathf.Max(0.08f, baseDelay * Mathf.Max(0.52f, 1f - tower.Level * 0.055f) / runFireRate);
        }

        private void CreateProjectile(Vector3 from, Vector3 to, TowerType type)
        {
            GameObject projectile = Sphere("Projectile", from, Vector3.one * 0.16f, type == TowerType.Frost ? "frost" : "projectile");
            projectiles.Add(new Projectile { Root = projectile, From = from, To = to, Life = 0.16f, MaxLife = 0.16f });
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = projectiles[i];
                projectile.Life -= dt;
                float t = 1f - Mathf.Clamp01(projectile.Life / projectile.MaxLife);
                if (projectile.Root != null)
                {
                    projectile.Root.transform.position = Vector3.Lerp(projectile.From, projectile.To, t);
                }
                if (projectile.Life <= 0f)
                {
                    if (projectile.Root != null) Destroy(projectile.Root);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void Burst(Vector3 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.05f, 0.45f), Random.Range(-0.35f, 0.35f));
                GameObject spark = Sphere("Impact Spark", position + offset, Vector3.one * Random.Range(0.05f, 0.12f), "projectile");
                Destroy(spark, 0.45f);
            }
        }

        private void SetChildMaterials(GameObject root, string matKey)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (!materials.TryGetValue(matKey, out Material material)) return;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.name.Contains("HP")) continue;
                renderer.sharedMaterial = material;
            }
        }

        private void ClearEnemies()
        {
            foreach (Enemy enemy in enemies) if (enemy.Root != null) Destroy(enemy.Root);
            enemies.Clear();
            spawnLeft = 0;
        }

        private void ClearTowers()
        {
            foreach (Slot slot in slots)
            {
                if (slot.Tower != null && slot.Tower.Root != null) Destroy(slot.Tower.Root);
                slot.Tower = null;
            }
        }

        private void ClearProjectiles()
        {
            foreach (Projectile projectile in projectiles) if (projectile.Root != null) Destroy(projectile.Root);
            projectiles.Clear();
        }

        private void FloatText(string text, Vector3 world)
        {
            floatingTexts.Add(new FloatingText { Text = text, World = world, Life = 1.15f });
        }

        private void UpdateFloatingText(float dt)
        {
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                floatingTexts[i].Life -= dt;
                floatingTexts[i].World += Vector3.up * dt * 0.75f;
                if (floatingTexts[i].Life <= 0f) floatingTexts.RemoveAt(i);
            }
        }

        private void Animate(float dt)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                float y = 0.08f + Mathf.Sin(Time.time * 2f + i) * 0.01f;
                Vector3 pos = slots[i].Pad.transform.position;
                slots[i].Pad.transform.position = new Vector3(pos.x, y, pos.z);
            }
        }

        private int TowerCount()
        {
            int count = 0;
            foreach (Slot slot in slots) if (slot.Tower != null) count++;
            return count;
        }

        private void PaintSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                SetMat(slots[i].Pad, i == selectedSlot ? "padSelected" : "pad");
            }
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
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, 38 * scale, w, 500 * scale), GUI.skin.box);
            GUILayout.Label("MERGE DEFENSE", title, GUILayout.Height(54 * scale));
            GUILayout.Label("MODERN 3D", title, GUILayout.Height(44 * scale));
            GUILayout.Label("Best Wave " + bestWave + "  |  Best Score " + bestScore + "  |  Gems " + gems, label);
            if (GUILayout.Button("NEUER RUN", button, GUILayout.Height(62 * scale))) StartRun();
            if (GUILayout.Button("Meta: Start-Coins +25  L" + metaCoins, button, GUILayout.Height(48 * scale))) BuyMeta(0);
            if (GUILayout.Button("Meta: Schaden +1  L" + metaDamage, button, GUILayout.Height(48 * scale))) BuyMeta(1);
            if (GUILayout.Button("Meta: Basisleben +4  L" + metaBase, button, GUILayout.Height(48 * scale))) BuyMeta(2);
            GUILayout.Label("Isometrische 3D-MVP-Optik. Spaeter koennen FBX/GLB-Modelle die Platzhalter ersetzen.", label);
            GUILayout.EndArea();
        }

        private void DrawBuild(GUIStyle label, GUIStyle button, float scale)
        {
            DrawHud(label, scale);
            float w = 680 * scale;
            GUILayout.BeginArea(new Rect((Screen.width - w) * 0.5f, Screen.height - 170 * scale, w, 150 * scale), GUI.skin.box);
            GUILayout.Label("Pad anklicken = Tower kaufen. Zwei gleiche Tower = Merge.", label);
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
            GUILayout.BeginArea(new Rect(18 * scale, Screen.height - 104 * scale, 430 * scale, 86 * scale), GUI.skin.box);
            GUILayout.Label("Enemies " + enemies.Count + " | Spawn " + spawnLeft, label);
            GUILayout.Label("Tower feuern automatisch.", label);
            GUILayout.EndArea();
        }

        private void DrawUpgrade(GUIStyle title, GUIStyle button, float scale)
        {
            float w = 630 * scale;
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
            GUILayout.BeginArea(new Rect(18 * scale, 12 * scale, 520 * scale, 112 * scale), GUI.skin.box);
            GUILayout.Label("Wave " + wave + (BossWave ? "  BOSS" : "") + " | Score " + score, label);
            GUILayout.Label("Base " + baseHp + "/" + maxBaseHp + " | Coins " + coins + " | Buy " + BuyCost, label);
            GUILayout.Label("DMG +" + (runDamage + metaDamage) + " | Speed x" + runFireRate.ToString("0.00") + " | Income +" + runIncome + "%", label);
            GUILayout.EndArea();
        }

        private void DrawWorldLabels(GUIStyle style)
        {
            WorldLabel(spawnPoint + Vector3.up * 0.8f, "SPAWN", style, 150, 28);
            WorldLabel(basePoint + Vector3.up * 0.9f, "BASE", style, 150, 28);
            foreach (Slot slot in slots)
            {
                string text = slot.Tower == null ? "+" : slot.Tower.Type.ToString()[0] + slot.Tower.Level.ToString();
                WorldLabel(slot.Position + Vector3.up * 0.32f, text, style, 80, 28);
            }
            foreach (FloatingText text in floatingTexts)
            {
                WorldLabel(text.World, text.Text, style, 190, 28);
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
