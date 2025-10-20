using UnityEngine;
using System;

namespace ProceduralGeneration
{
    /// <summary>
    /// Weighted prefab data for rooms
    /// </summary>
    [Serializable]
    public class WeightedRoomPrefab
    {
        [SerializeField] private GameObject roomPrefab;
        [SerializeField, Range(1, 100)] private int weight = 50;
        [SerializeField] private string description = "";

        public GameObject RoomPrefab => roomPrefab;
        public int Weight => weight;
        public string Description => description;

        public bool IsValid()
        {
            if (roomPrefab == null) return false;
            return roomPrefab.GetComponent<Room>() != null;
        }
    }

    /// <summary>
    /// Weighted prefab data for corridors
    /// </summary>
    [Serializable]
    public class WeightedCorridorPrefab
    {
        [SerializeField] private GameObject corridorPrefab;
        [SerializeField, Range(1, 100)] private int weight = 50;
        [SerializeField] private string description = "";

        public GameObject CorridorPrefab => corridorPrefab;
        public int Weight => weight;
        public string Description => description;

        public bool IsValid()
        {
            if (corridorPrefab == null) return false;
            return corridorPrefab.GetComponent<Corridor>() != null;
        }
    }
}
