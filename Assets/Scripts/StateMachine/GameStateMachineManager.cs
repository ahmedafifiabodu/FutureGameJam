using UnityEngine;
using System;

namespace GameStateMachine
{
    /// <summary>
    /// Core game state machine that manages state transitions and game lifecycle
    /// Handles pause, resume, restart, and game over states
    /// </summary>
    public class GameStateMachineManager : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        // Current and previous states
        private IGameState currentState;

        private IGameState pausedState; // Store state when paused

        // State instances
        private ParasiteGameState parasiteState;

        private HostGameState hostState;
        private PausedGameState pausedGameState;
        private GameOverGameState gameOverState;

        // Dependencies (injected)
        private GameStateManager gameStateManager;

        private InputManager inputManager;
        private ProceduralGeneration.ProceduralLevelGenerator levelGenerator;

        // Properties
        public bool EnableDebugLogs => enableDebugLogs;

        public bool IsPaused => currentState == pausedGameState;
        public IGameState CurrentState => currentState;

        // Events
        public event Action OnGamePaused;

        public event Action OnGameResumed;

        public event Action OnGameRestarted;

        public event Action<int, float> OnGameOver;

        private void Awake()
        {
            // Register with ServiceLocator
            ServiceLocator.Instance.RegisterService(this, false);
        }

        private void Start()
        {
            // Get dependencies from ServiceLocator
            gameStateManager = ServiceLocator.Instance.GetService<GameStateManager>();
            inputManager = ServiceLocator.Instance.GetService<InputManager>();

            // Find level generator
            levelGenerator = FindFirstObjectByType<ProceduralGeneration.ProceduralLevelGenerator>();

            // Initialize states
            InitializeStates();

            // Start in parasite state
            ChangeState(parasiteState);
        }

        private void Update()
        {
            // Update current state
            currentState?.OnUpdate();

            // Check for pause input using New Input System (UI.Cancel mapped to ESC)
            if (inputManager != null && inputManager.UIActions.Cancel.WasPressedThisFrame())
            {
                if (IsPaused)
                    ResumeGame();
                else if (currentState != gameOverState)
                    PauseGame();
            }
        }

        private void InitializeStates()
        {
            parasiteState = new ParasiteGameState(this);
            hostState = new HostGameState(this);
            pausedGameState = new PausedGameState(this);
            gameOverState = new GameOverGameState(this);

            if (enableDebugLogs)
                Debug.Log("[GameStateMachine] States initialized");
        }

        #region State Transitions

        /// <summary>
        /// Change to a new state
        /// </summary>
        public void ChangeState(IGameState newState)
        {
            if (currentState == newState)
                return;

            currentState?.OnExit();
            currentState = newState;
            currentState?.OnEnter();
        }

        /// <summary>
        /// Switch to Parasite mode
        /// </summary>
        public void SwitchToParasiteMode()
        {
            ChangeState(parasiteState);
        }

        /// <summary>
        /// Switch to Host mode
        /// </summary>
        public void SwitchToHostMode()
        {
            ChangeState(hostState);
        }

        #endregion State Transitions

        #region Game Control

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused)
                return;

            pausedState = currentState;
            currentState?.OnPause();
            ChangeState(pausedGameState);

            Time.timeScale = 0f;
            OnGamePaused?.Invoke();

            if (enableDebugLogs)
                Debug.Log("[GameStateMachine] Game paused");
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused)
                return;

            Time.timeScale = 1f;

            // Return to previous state
            IGameState stateToResume = pausedState;
            pausedState = null;
            ChangeState(stateToResume);
            stateToResume?.OnResume();

            OnGameResumed?.Invoke();

            if (enableDebugLogs)
                Debug.Log("[GameStateMachine] Game resumed");
        }

        /// <summary>
        /// Restart the game from the beginning
        /// </summary>
        public void RestartGame()
        {
            if (enableDebugLogs)
                Debug.Log("[GameStateMachine] Restarting game...");

            // Unpause if paused
            if (IsPaused)
            {
                Time.timeScale = 1f;
                pausedState = null;
            }

            // Reset game state
            gameStateManager.RestartGame();

            // Reset level generator
            if (levelGenerator != null)
            {
                levelGenerator.ResetLevel();
            }

            // Return to parasite state
            ChangeState(parasiteState);

            OnGameRestarted?.Invoke();

            if (enableDebugLogs)
                Debug.Log("[GameStateMachine] Game restarted");
        }

        /// <summary>
        /// Trigger game over
        /// </summary>
        public void TriggerGameOver(int hostsConsumed, float survivalTime)
        {
            ChangeState(gameOverState);
            OnGameOver?.Invoke(hostsConsumed, survivalTime);

            if (enableDebugLogs)
                Debug.Log($"[GameStateMachine] Game Over - Hosts: {hostsConsumed}, Time: {survivalTime:F1}s");
        }

        #endregion Game Control

        #region Public API

        /// <summary>
        /// Get the game state manager reference
        /// </summary>
        public GameStateManager GetGameStateManager() => gameStateManager;

        /// <summary>
        /// Get the input manager reference
        /// </summary>
        public InputManager GetInputManager() => inputManager;

        /// <summary>
        /// Get the level generator reference
        /// </summary>
        public ProceduralGeneration.ProceduralLevelGenerator GetLevelGenerator() => levelGenerator;

        #endregion Public API
    }
}