using UnityEngine;

namespace GameStateMachine
{
    /// <summary>
    /// Game state for when the game is over
    /// </summary>
    public class GameOverGameState : GameStateBase
    {
        public override string StateName => "Game Over";

        public GameOverGameState(GameStateMachineManager stateMachine) : base(stateMachine)
 {
        }

        public override void OnEnter()
        {
     base.OnEnter();

            var inputManager = stateMachine.GetInputManager();
            var gameStateManager = stateMachine.GetGameStateManager();

            // Disable gameplay input
       if (inputManager != null)
    inputManager.DisableAllActions();

  // Trigger Game Over UI through GameStateManager
      if (gameStateManager != null)
            {
gameStateManager.GameOver();
            }
    }

  public override void OnUpdate()
 {
     base.OnUpdate();

// Game over menu logic
            // Check for restart/quit buttons
      }

        public override void OnExit()
{
   base.OnExit();

  // Clean up game over state
}
    }
}
