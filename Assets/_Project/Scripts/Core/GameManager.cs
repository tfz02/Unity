using System;
using UnityEngine;

namespace MergeDefenseSurvivor.Core
{
    public enum GameState
    {
        Boot,
        BuildPhase,
        WaveRunning,
        UpgradeSelection,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Boot;

        public event Action<GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SetState(GameState.BuildPhase);
        }

        public void StartWave()
        {
            if (CurrentState != GameState.BuildPhase)
            {
                return;
            }

            SetState(GameState.WaveRunning);
        }

        public void CompleteWave()
        {
            if (CurrentState != GameState.WaveRunning)
            {
                return;
            }

            SetState(GameState.UpgradeSelection);
        }

        public void ContinueAfterUpgrade()
        {
            if (CurrentState != GameState.UpgradeSelection)
            {
                return;
            }

            SetState(GameState.BuildPhase);
        }

        public void TriggerGameOver()
        {
            SetState(GameState.GameOver);
        }

        private void SetState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            StateChanged?.Invoke(CurrentState);
            Debug.Log($"Game state changed to {CurrentState}");
        }
    }
}
