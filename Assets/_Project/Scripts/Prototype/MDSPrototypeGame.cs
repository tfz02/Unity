using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Prototype
{
    public sealed class MDSPrototypeGame : MonoBehaviour
    {
        private enum PrototypePhase
        {
            Build,
            Wave,
            Upgrade,
            GameOver
        }

        private sealed class TowerSlotRuntime
        {
            public int Index;
            public Vector2 Position;
            public GameObject Root;
            public SpriteRenderer Renderer;
            public TowerRuntime Tower;
        }

        private sealed class TowerRuntime
        {
            public int Level = 1;
            public float Cooldown;
            public GameObject Root;
            public SpriteRenderer Renderer;
        }

        private sealed class EnemyRuntime
        {
            public int Health;
            public int MaxHealth;
            public float Speed;
            public int Reward;
            public GameObject Root;
            public SpriteRenderer Renderer;
        }

        private const int MaxTowerLevel = 5;
        private const int StartCoins = 110;
        private const int StartBaseHealth = 20;

        private readonly List<TowerSlotRuntime> towerSlots = new();
        private readonly List<EnemyRuntime> enemies = new();

        private Camera gameCamera;
        private Sprite squareSprite;
        private Sprite circleSprite;
        private Texture2D squareTexture;
        private Texture2D circleTexture;

        private PrototypePhase phase = PrototypePhase.Build;
        private int coins = StartCoins;
        private int baseHealth = StartBaseHealth;
        private int maxBaseHealth = StartBaseHealth;
        private int waveNumber = 1;
        private int enemiesToSpawn;
        private float spawnTimer;
        private int selectedSlotIndex = -1;

        private int flatTowerDamageBonus;
        private float fireRateMultiplier = 1f;
        private int incomeBonusPercent;

        private readonly Vector2 spawnPosition = new(0f, 4.2f);
        private readonly Vector2 basePosition = new(0f, -3.6f);

        private int CurrentBuyCost => 30 + Mathf.Max(0, waveNumber - 1) * 5;

        private void Start()
        {
            SetupCamera();
            CreateRuntimeSprites();
            CreateBoard();
            UpdateSlotVisuals();
        }

        private void Update()
        {
            HandleWorldInput();

            if (phase == PrototypePhase.Wave)
            {
                UpdateSpawning(Time.deltaTime);
                UpdateEnemies(Time.deltaTime);
                UpdateTowers(Time.deltaTime);
                TryCompleteWave();
            }
        }

        private void OnDestroy()
        {
            if (squareTexture != null)
            {
                Destroy(squareTexture);
            }

            if (circleTexture != null)
            {
                Destroy(circleTexture);
            }
        }

        private void SetupCamera()
        {
            gameCamera = Camera.main;

            if (gameCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 5f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
        }

        private void CreateRuntimeSprites()
        {
            squareTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            squareTexture.SetPixel(0, 0, Color.white);
            squareTexture.Apply();
            squareSprite = Sprite.Create(squareTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 31.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    circleTexture.SetPixel(x, y, distance <= 30f ? Color.white : Color.clear);
                }
            }

            circleTexture.Apply();
            circleSprite = Sprite.Create(circleTexture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        }

        private void CreateBoard()
        {
            CreateRect("Path", new Vector2(0f, 0.35f), new Vector2(0.55f, 7.4f), new Color(0.22f, 0.24f, 0.27f), 0);
            CreateRect("Spawn", spawnPosition, new Vector2(1.35f, 0.45f), new Color(0.1f, 0.55f, 0.25f), 1);
            CreateRect("Base", basePosition, new Vector2(2.2f, 0.65f), new Color(0.75f, 0.18f, 0.16f), 1);

            Vector2[] slotPositions =
            {
                new(-2.25f, 2.75f),
                new(2.25f, 2.75f),
                new(-2.25f, 1.25f),
                new(2.25f, 1.25f),
                new(-2.25f, -0.25f),
                new(2.25f, -0.25f),
                new(-2.25f, -1.75f),
                new(2.25f, -1.75f)
            };

            for (int i = 0; i < slotPositions.Length; i++)
            {
                GameObject slotObject = CreateRect($"Tower Slot {i + 1}", slotPositions[i], new Vector2(0.95f, 0.95f), new Color(0.2f, 0.22f, 0.28f), 2);
                towerSlots.Add(new TowerSlotRuntime
                {
                    Index = i,
                    Position = slotPositions[i],
                    Root = slotObject,
                    Renderer = slotObject.GetComponent<SpriteRenderer>()
                });
            }
        }

        private GameObject CreateRect(string objectName, Vector2 position, Vector2 scale, Color color, int sortingOrder)
        {
            GameObject root = new GameObject(objectName);
            root.transform.position = position;
            root.transform.localScale = scale;

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return root;
        }

        private GameObject CreateCircle(string objectName, Vector2 position, float scale, Color color, int sortingOrder)
        {
            GameObject root = new GameObject(objectName);
            root.transform.position = position;
            root.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return root;
        }

        private void HandleWorldInput()
        {
            if (phase != PrototypePhase.Build || gameCamera == null)
            {
                return;
            }

            bool pressed = false;
            Vector2 screenPosition = default;

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pressed = true;
                screenPosition = Input.GetTouch(0).position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                pressed = true;
                screenPosition = Input.mousePosition;
            }

            if (!pressed)
            {
                return;
            }

            Vector3 world = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            TryHandleSlotClick(world);
        }

        private void TryHandleSlotClick(Vector2 worldPosition)
        {
            for (int i = 0; i < towerSlots.Count; i++)
            {
                TowerSlotRuntime slot = towerSlots[i];

                if (Vector2.Distance(worldPosition, slot.Position) > 0.58f)
                {
                    continue;
                }

                if (slot.Tower == null)
                {
                    TryBuyTower(slot);
                }
                else
                {
                    HandleTowerSelection(slot);
                }

                UpdateSlotVisuals();
                return;
            }
        }

        private void TryBuyTower(TowerSlotRuntime slot)
        {
            if (coins < CurrentBuyCost)
            {
                Debug.Log("Not enough coins to buy tower.");
                return;
            }

            coins -= CurrentBuyCost;
            slot.Tower = CreateTower(slot.Position);
            selectedSlotIndex = slot.Index;
        }

        private TowerRuntime CreateTower(Vector2 position)
        {
            GameObject towerObject = CreateCircle("Tower L1", position, 0.62f, GetTowerColor(1), 5);

            return new TowerRuntime
            {
                Level = 1,
                Cooldown = 0f,
                Root = towerObject,
                Renderer = towerObject.GetComponent<SpriteRenderer>()
            };
        }

        private void HandleTowerSelection(TowerSlotRuntime clickedSlot)
        {
            if (selectedSlotIndex < 0)
            {
                selectedSlotIndex = clickedSlot.Index;
                return;
            }

            if (selectedSlotIndex == clickedSlot.Index)
            {
                selectedSlotIndex = -1;
                return;
            }

            TowerSlotRuntime selectedSlot = towerSlots[selectedSlotIndex];

            if (TryMerge(selectedSlot, clickedSlot))
            {
                selectedSlotIndex = selectedSlot.Index;
                return;
            }

            selectedSlotIndex = clickedSlot.Index;
        }

        private bool TryMerge(TowerSlotRuntime targetSlot, TowerSlotRuntime consumedSlot)
        {
            if (targetSlot.Tower == null || consumedSlot.Tower == null)
            {
                return false;
            }

            if (targetSlot.Tower.Level != consumedSlot.Tower.Level || targetSlot.Tower.Level >= MaxTowerLevel)
            {
                return false;
            }

            targetSlot.Tower.Level++;
            targetSlot.Tower.Root.name = $"Tower L{targetSlot.Tower.Level}";
            targetSlot.Tower.Renderer.color = GetTowerColor(targetSlot.Tower.Level);
            targetSlot.Tower.Root.transform.localScale = Vector3.one * (0.62f + targetSlot.Tower.Level * 0.06f);

            Destroy(consumedSlot.Tower.Root);
            consumedSlot.Tower = null;
            return true;
        }

        private void StartWave()
        {
            if (phase != PrototypePhase.Build)
            {
                return;
            }

            selectedSlotIndex = -1;
            enemiesToSpawn = 5 + waveNumber * 2;
            spawnTimer = 0.35f;
            phase = PrototypePhase.Wave;
            UpdateSlotVisuals();
        }

        private void UpdateSpawning(float deltaTime)
        {
            if (enemiesToSpawn <= 0)
            {
                return;
            }

            spawnTimer -= deltaTime;

            if (spawnTimer > 0f)
            {
                return;
            }

            SpawnEnemy();
            enemiesToSpawn--;
            spawnTimer = Mathf.Max(0.22f, 0.95f - waveNumber * 0.035f);
        }

        private void SpawnEnemy()
        {
            int maxHealth = 5 + waveNumber * 3;
            float speed = 0.9f + waveNumber * 0.035f;
            int reward = 8 + waveNumber;

            GameObject enemyObject = CreateCircle($"Enemy W{waveNumber}", spawnPosition, 0.42f, new Color(0.88f, 0.38f, 0.16f), 4);
            enemies.Add(new EnemyRuntime
            {
                Health = maxHealth,
                MaxHealth = maxHealth,
                Speed = speed,
                Reward = reward,
                Root = enemyObject,
                Renderer = enemyObject.GetComponent<SpriteRenderer>()
            });
        }

        private void UpdateEnemies(float deltaTime)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyRuntime enemy = enemies[i];
                Vector2 current = enemy.Root.transform.position;
                Vector2 next = Vector2.MoveTowards(current, basePosition, enemy.Speed * deltaTime);
                enemy.Root.transform.position = next;

                if (Vector2.Distance(next, basePosition) <= 0.16f)
                {
                    baseHealth = Mathf.Max(0, baseHealth - 1);
                    Destroy(enemy.Root);
                    enemies.RemoveAt(i);

                    if (baseHealth <= 0)
                    {
                        phase = PrototypePhase.GameOver;
                        ClearAllEnemies();
                        return;
                    }
                }
            }
        }

        private void UpdateTowers(float deltaTime)
        {
            foreach (TowerSlotRuntime slot in towerSlots)
            {
                TowerRuntime tower = slot.Tower;

                if (tower == null)
                {
                    continue;
                }

                tower.Cooldown -= deltaTime;

                if (tower.Cooldown > 0f)
                {
                    continue;
                }

                EnemyRuntime target = FindClosestEnemy(slot.Position, GetTowerRange(tower.Level));

                if (target == null)
                {
                    continue;
                }

                DamageEnemy(target, GetTowerDamage(tower.Level));
                tower.Cooldown = GetTowerFireDelay(tower.Level);
            }
        }

        private EnemyRuntime FindClosestEnemy(Vector2 towerPosition, float range)
        {
            EnemyRuntime closest = null;
            float closestDistance = float.MaxValue;

            foreach (EnemyRuntime enemy in enemies)
            {
                if (enemy.Root == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(towerPosition, enemy.Root.transform.position);

                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        private void DamageEnemy(EnemyRuntime enemy, int damage)
        {
            enemy.Health -= damage;

            if (enemy.Health > 0)
            {
                float healthPercent = Mathf.Clamp01(enemy.Health / (float)enemy.MaxHealth);
                enemy.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 0.42f, healthPercent);
                return;
            }

            int reward = Mathf.RoundToInt(enemy.Reward * (1f + incomeBonusPercent / 100f));
            coins += reward;
            Destroy(enemy.Root);
            enemies.Remove(enemy);
        }

        private void TryCompleteWave()
        {
            if (phase == PrototypePhase.Wave && enemiesToSpawn <= 0 && enemies.Count == 0)
            {
                phase = PrototypePhase.Upgrade;
            }
        }

        private void ApplyUpgrade(int upgradeIndex)
        {
            if (phase != PrototypePhase.Upgrade)
            {
                return;
            }

            switch (upgradeIndex)
            {
                case 0:
                    flatTowerDamageBonus += 1;
                    break;
                case 1:
                    fireRateMultiplier += 0.18f;
                    break;
                case 2:
                    maxBaseHealth += 4;
                    baseHealth = Mathf.Min(maxBaseHealth, baseHealth + 8);
                    break;
                case 3:
                    incomeBonusPercent += 20;
                    break;
            }

            waveNumber++;
            coins += 15;
            phase = PrototypePhase.Build;
            UpdateSlotVisuals();
        }

        private void RestartRun()
        {
            ClearAllEnemies();

            foreach (TowerSlotRuntime slot in towerSlots)
            {
                if (slot.Tower != null)
                {
                    Destroy(slot.Tower.Root);
                    slot.Tower = null;
                }
            }

            coins = StartCoins;
            baseHealth = StartBaseHealth;
            maxBaseHealth = StartBaseHealth;
            waveNumber = 1;
            enemiesToSpawn = 0;
            selectedSlotIndex = -1;
            flatTowerDamageBonus = 0;
            fireRateMultiplier = 1f;
            incomeBonusPercent = 0;
            phase = PrototypePhase.Build;
            UpdateSlotVisuals();
        }

        private void ClearAllEnemies()
        {
            foreach (EnemyRuntime enemy in enemies)
            {
                if (enemy.Root != null)
                {
                    Destroy(enemy.Root);
                }
            }

            enemies.Clear();
            enemiesToSpawn = 0;
        }

        private int GetTowerDamage(int level)
        {
            return level * 2 + flatTowerDamageBonus;
        }

        private float GetTowerRange(int level)
        {
            return 2.2f + level * 0.18f;
        }

        private float GetTowerFireDelay(int level)
        {
            float baseDelay = Mathf.Max(0.18f, 0.85f - level * 0.08f);
            return Mathf.Max(0.08f, baseDelay / fireRateMultiplier);
        }

        private Color GetTowerColor(int level)
        {
            return level switch
            {
                1 => new Color(0.25f, 0.62f, 1f),
                2 => new Color(0.25f, 0.95f, 0.55f),
                3 => new Color(0.95f, 0.85f, 0.25f),
                4 => new Color(0.95f, 0.45f, 0.95f),
                _ => new Color(1f, 0.95f, 0.95f)
            };
        }

        private void UpdateSlotVisuals()
        {
            for (int i = 0; i < towerSlots.Count; i++)
            {
                TowerSlotRuntime slot = towerSlots[i];
                bool selected = selectedSlotIndex == i;
                slot.Renderer.color = selected
                    ? new Color(0.95f, 0.75f, 0.18f)
                    : new Color(0.2f, 0.22f, 0.28f);
            }
        }

        private void OnGUI()
        {
            int fontSize = Mathf.Clamp(Screen.height / 42, 16, 28);
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white }
            };

            GUIStyle titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = fontSize + 6,
                fontStyle = FontStyle.Bold
            };

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize
            };

            GUILayout.BeginArea(new Rect(16f, 12f, 460f, 330f), GUI.skin.box);
            GUILayout.Label("MergeDefenseSurvivor - Prototype", titleStyle);
            GUILayout.Label($"Phase: {phase} | Wave: {waveNumber}", labelStyle);
            GUILayout.Label($"Base: {baseHealth}/{maxBaseHealth} | Coins: {coins} | Buy: {CurrentBuyCost}", labelStyle);
            GUILayout.Label($"Damage +{flatTowerDamageBonus} | Fire x{fireRateMultiplier:0.00} | Income +{incomeBonusPercent}%", labelStyle);

            if (phase == PrototypePhase.Build)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Klick: leeren Slot kaufen. Zwei gleiche Tower klicken = Merge.", labelStyle);

                if (GUILayout.Button("Start Wave", buttonStyle, GUILayout.Height(46f)))
                {
                    StartWave();
                }
            }
            else if (phase == PrototypePhase.Wave)
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Enemies: {enemies.Count} | Remaining spawn: {enemiesToSpawn}", labelStyle);
            }
            else if (phase == PrototypePhase.Upgrade)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Upgrade wählen:", labelStyle);

                if (GUILayout.Button("+1 Tower Damage", buttonStyle, GUILayout.Height(42f)))
                {
                    ApplyUpgrade(0);
                }

                if (GUILayout.Button("+18% Fire Rate", buttonStyle, GUILayout.Height(42f)))
                {
                    ApplyUpgrade(1);
                }

                if (GUILayout.Button("+4 Max Base / +8 Heal", buttonStyle, GUILayout.Height(42f)))
                {
                    ApplyUpgrade(2);
                }

                if (GUILayout.Button("+20% Coin Income", buttonStyle, GUILayout.Height(42f)))
                {
                    ApplyUpgrade(3);
                }
            }
            else if (phase == PrototypePhase.GameOver)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Game Over", titleStyle);

                if (GUILayout.Button("Restart Run", buttonStyle, GUILayout.Height(46f)))
                {
                    RestartRun();
                }
            }

            GUILayout.EndArea();

            DrawWorldLabels(labelStyle);
        }

        private void DrawWorldLabels(GUIStyle style)
        {
            if (gameCamera == null)
            {
                return;
            }

            foreach (TowerSlotRuntime slot in towerSlots)
            {
                string text = slot.Tower == null ? "+" : $"L{slot.Tower.Level}";
                DrawLabelAtWorld(slot.Position, text, style, 70f, 26f);
            }

            foreach (EnemyRuntime enemy in enemies)
            {
                if (enemy.Root != null)
                {
                    DrawLabelAtWorld(enemy.Root.transform.position + Vector3.up * 0.38f, enemy.Health.ToString(), style, 70f, 24f);
                }
            }

            DrawLabelAtWorld(spawnPosition + Vector2.up * 0.42f, "SPAWN", style, 120f, 28f);
            DrawLabelAtWorld(basePosition + Vector2.down * 0.45f, "BASE", style, 120f, 28f);
        }

        private void DrawLabelAtWorld(Vector3 worldPosition, string text, GUIStyle style, float width, float height)
        {
            Vector3 screen = gameCamera.WorldToScreenPoint(worldPosition);
            Rect rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - height * 0.5f, width, height);

            TextAnchor oldAlignment = style.alignment;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, text, style);
            style.alignment = oldAlignment;
        }
    }
}
