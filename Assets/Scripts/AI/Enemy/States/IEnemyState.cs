using UnityEngine;

namespace AI.Enemy.States
{
    /// <summary>
    /// Enhanced interface for enemy states with enter/exit callbacks
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// Called when entering this state
        /// </summary>
   void EnterState(EnemyController enemy);
        
        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        void UpdateState(EnemyController enemy);
 
        /// <summary>
        /// Called when exiting this state
        /// </summary>
  void ExitState(EnemyController enemy);
    }
}
