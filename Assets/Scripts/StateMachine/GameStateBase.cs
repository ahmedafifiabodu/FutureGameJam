using UnityEngine;

namespace GameStateMachine
{
    /// <summary>
    /// Base abstract class for game states with common functionality
    /// </summary>
    public abstract class GameStateBase : IGameState
    {
     protected readonly GameStateMachineManager stateMachine;

        protected GameStateBase(GameStateMachineManager stateMachine)
   {
   this.stateMachine = stateMachine;
        }

        public abstract string StateName { get; }

    public virtual void OnEnter()
        {
            if (stateMachine.EnableDebugLogs)
      Debug.Log($"[GameState] Entering {StateName}");
        }

        public virtual void OnUpdate()
    {
        }

        public virtual void OnExit()
      {
            if (stateMachine.EnableDebugLogs)
                Debug.Log($"[GameState] Exiting {StateName}");
    }

 public virtual void OnPause()
        {
            if (stateMachine.EnableDebugLogs)
                Debug.Log($"[GameState] Pausing {StateName}");
        }

  public virtual void OnResume()
        {
            if (stateMachine.EnableDebugLogs)
      Debug.Log($"[GameState] Resuming {StateName}");
        }
    }
}
