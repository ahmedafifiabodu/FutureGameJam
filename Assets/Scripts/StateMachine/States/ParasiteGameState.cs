using UnityEngine;

namespace GameStateMachine
{
    /// <summary>
    /// Game state for when player is in Parasite mode
    /// Manages ParasiteController lifecycle and state
    /// </summary>
    public class ParasiteGameState : GameStateBase
    {
        private ParasiteController parasiteController;

        public override string StateName => "Parasite Mode";

        public ParasiteGameState(GameStateMachineManager stateMachine) : base(stateMachine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            var gameStateManager = stateMachine.GetGameStateManager();
            var inputManager = stateMachine.GetInputManager();

            // Get parasite controller reference
            parasiteController = gameStateManager?.ParasiteController;

            // Enable parasite-specific input
            if (inputManager != null)
            {
                inputManager.EnableParasiteActions();
            }

            // Enable parasite controller
            if (parasiteController != null)
            {
                parasiteController.enabled = true;

                // Ensure CharacterController is enabled
                var characterController = parasiteController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = true;
                }

                if (stateMachine.EnableDebugLogs)
                {
                    Debug.Log("[ParasiteGameState] Parasite controller enabled and ready");
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Monitor parasite state
            if (parasiteController != null && stateMachine.EnableDebugLogs)
            {
                // Could add lifetime warnings or other monitoring here
                float lifetimePercentage = parasiteController.GetLifetimePercentage();
                if (lifetimePercentage < 0.2f) // Less than 20% lifetime
                {
                    // Could trigger warning effects
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            // Cleanup parasite-specific resources
            if (parasiteController != null)
            {
                // Hide any active UI elements
                // Trajectory should be hidden by the controller itself
            }
        }

        public override void OnPause()
        {
            base.OnPause();

            // Disable parasite controller during pause
            if (parasiteController != null)
            {
                parasiteController.enabled = false;

                if (stateMachine.EnableDebugLogs)
                {
                    Debug.Log("[ParasiteGameState] Parasite controller disabled (paused)");
                }
            }
        }

        public override void OnResume()
        {
            base.OnResume();

            // Re-enable parasite controller after resume
            if (parasiteController != null)
            {
                parasiteController.enabled = true;

                if (stateMachine.EnableDebugLogs)
                {
                    Debug.Log("[ParasiteGameState] Parasite controller re-enabled (resumed)");
                }
            }
        }
    }
}