using UnityEngine;

namespace GameStateMachine
{
    /// <summary>
    /// Interface for game state implementations
  /// Each state can handle Enter, Update, Exit, Pause, and Resume logic
    /// </summary>
    public interface IGameState
  {
   /// <summary>
    /// Called when entering this state
   /// </summary>
        void OnEnter();

        /// <summary>
        /// Called every frame while in this state
     /// </summary>
   void OnUpdate();

        /// <summary>
 /// Called when exiting this state
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called when the game is paused while in this state
        /// </summary>
 void OnPause();

/// <summary>
        /// Called when the game is resumed while in this state
    /// </summary>
        void OnResume();

 /// <summary>
   /// Get the state name for debugging
   /// </summary>
        string StateName { get; }
    }
}
