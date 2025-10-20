using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Represents a room piece in the procedural level
    /// </summary>
    public class Room : LevelPiece
    {
        [Header("Room Settings")]
        [SerializeField] private string roomName = "Room";
        [SerializeField] private bool startingRoom = false;

        public string RoomName => roomName;
        public bool IsStartingRoom => startingRoom;

        protected override void Awake()
        {
            base.Awake();
            
            // Starting room only has Point B (exit), no entrance
            if (startingRoom && pointA != null)
            {
                pointA.gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = startingRoom ? Color.yellow : Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }
}
