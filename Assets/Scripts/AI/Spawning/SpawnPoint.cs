using UnityEngine;

namespace AI.Spawning
{
    /// <summary>
    /// Marks a point where enemies or props can spawn
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
      [Header("Spawn Settings")]
    [Tooltip("Type of spawn point")]
        public SpawnType spawnType = SpawnType.Enemy;
        
        [Tooltip("Is this spawn point active?")]
        public bool isActive = true;
        
        [Tooltip("Has this spawn point been used?")]
        [HideInInspector]
public bool hasSpawned = false;
        
        public enum SpawnType
     {
            Enemy,
 Prop,
   Both
        }
        
 /// <summary>
     /// Reset the spawn point for reuse
        /// </summary>
        public void Reset()
   {
            hasSpawned = false;
        }
        
        private void OnDrawGizmos()
{
            Gizmos.color = spawnType == SpawnType.Enemy ? Color.red : 
   spawnType == SpawnType.Prop ? Color.green : 
            Color.yellow;
   
        Gizmos.DrawWireSphere(transform.position, 0.5f);
       Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
        }
 
private void OnDrawGizmosSelected()
        {
         Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
