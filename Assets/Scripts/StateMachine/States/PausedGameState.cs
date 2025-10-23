using UnityEngine;

namespace GameStateMachine
{
    /// <summary>
 /// Game state for when the game is paused
    /// </summary>
    public class PausedGameState : GameStateBase
    {
        public override string StateName => "Paused";

        public PausedGameState(GameStateMachineManager stateMachine) : base(stateMachine)
        {
   }

        public override void OnEnter()
        {
     base.OnEnter();

  var inputManager = stateMachine.GetInputManager();

// Disable gameplay input
     if (inputManager != null)
      inputManager.DisableAllActions();

         // Show pause UI (if implemented)
   // TODO: Implement pause menu UI
  }

   public override void OnUpdate()
   {
      base.OnUpdate();

    // Pause menu logic can go here
     // Check for resume/quit/restart buttons
        }

        public override void OnExit()
     {
       base.OnExit();

       // Hide pause UI
  }
    }
}
