using UnityEngine;

namespace GameStateMachine
{
    /// <summary>
    /// Game state for when player is controlling a Host
    /// Manages HostController lifecycle and state
    /// </summary>
    public class HostGameState : GameStateBase
    {
        private HostController hostController;

        public override string StateName => "Host Mode";

        public HostGameState(GameStateMachineManager stateMachine) : base(stateMachine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            var gameStateManager = stateMachine.GetGameStateManager();
            var inputManager = stateMachine.GetInputManager();

            // Get host controller reference
            hostController = gameStateManager?.CurrentHostController;

            // Enable host-specific input (player actions)
            if (inputManager != null)
            {
                inputManager.EnablePlayerActions();
            }

            // Enable host controller
            if (hostController != null)
            {
                hostController.enabled = true;

                if (stateMachine.EnableDebugLogs)
                {
                    Debug.Log($"[HostGameState] Host controller enabled - Lifetime: {hostController.GetLifetimePercentage() * 100f:F1}%");
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Monitor host lifetime
            if (hostController != null && stateMachine.EnableDebugLogs)
            {
                float lifetimePercentage = hostController.GetLifetimePercentage();

                // Could add visual/audio warnings when lifetime is low
                if (lifetimePercentage < 0.3f && lifetimePercentage > 0.29f) // Around 30%
                {
                    // Trigger warning effect
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            // Cleanup host-specific resources
            if (hostController != null)
            {
                // Hide any active trajectory or UI elements
            }
        }

        public override void OnPause()
        {
            base.OnPause();

            // Disable host controller during pause
            if (hostController != null)
            {
                hostController.enabled = false;

                if (stateMachine.EnableDebugLogs)
                {
                    Debug.Log("[HostGameState] Host controller disabled (paused)");
                }
            }
        }

        public override void OnResume()
        {
            base.OnResume();

            // Re-enable host controller after resume
            if (hostController != null)
            {
                hostController.enabled = true;

                if (stateMachine.EnableDebugLogs)
                {
                    Debug.Log("[HostGameState] Host controller re-enabled (resumed)");
                }
            }
        }
    }
}