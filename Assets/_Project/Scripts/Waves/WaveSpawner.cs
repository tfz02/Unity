using System.Collections;
using MergeDefenseSurvivor.Core;
using MergeDefenseSurvivor.Economy;
using MergeDefenseSurvivor.Enemies;
using UnityEngine;

namespace MergeDefenseSurvivor.Waves
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private WaveDefinition[] waves;
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform enemyTarget;
        [SerializeField] private CurrencyWallet wallet;

        private int currentWaveIndex;
        private int aliveEnemies;
        private bool isSpawning;

        public void StartNextWave()
        {
            if (isSpawning || waves == null || waves.Length == 0)
            {
                return;
            }

            WaveDefinition wave = waves[Mathf.Clamp(currentWaveIndex, 0, waves.Length - 1)];
            GameManager.Instance?.StartWave();
            StartCoroutine(SpawnWaveRoutine(wave));
        }

        private IEnumerator SpawnWaveRoutine(WaveDefinition wave)
        {
            isSpawning = true;

            if (wave.StartDelay > 0f)
            {
                yield return new WaitForSeconds(wave.StartDelay);
            }

            for (int i = 0; i < wave.EnemyCount; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(wave.SpawnInterval);
            }

            isSpawning = false;
            currentWaveIndex++;
            TryCompleteWave();
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null || spawnPoint == null)
            {
                return;
            }

            Enemy enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            enemy.Initialize(enemyTarget, wallet);
            enemy.Died += OnEnemyDied;
            aliveEnemies++;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied;
            }

            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
            TryCompleteWave();
        }

        private void TryCompleteWave()
        {
            if (!isSpawning && aliveEnemies == 0)
            {
                GameManager.Instance?.CompleteWave();
            }
        }
    }
}
