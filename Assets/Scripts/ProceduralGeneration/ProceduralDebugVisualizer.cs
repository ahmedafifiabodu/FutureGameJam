using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Visual debug display for procedural generation system
    /// Attach to ProceduralLevelGenerator for runtime visualization
    /// </summary>
    public class ProceduralDebugVisualizer : MonoBehaviour
    {
        [Header("Debug Display")]
        [SerializeField] private bool showDebugUI = true;

        [SerializeField] private bool showConnectionLines = true;
        [SerializeField] private Color activeRoomColor = Color.green;
        [SerializeField] private Color previousRoomColor = Color.yellow;
        [SerializeField] private Color connectionLineColor = Color.cyan;

        private ProceduralLevelGenerator generator;
        private Transform player;

        private void Start()
        {
            generator = GetComponent<ProceduralLevelGenerator>();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showConnectionLines) return;

            // Draw connection points in scene
            ConnectionPoint[] allPoints = FindObjectsByType<ConnectionPoint>(FindObjectsSortMode.None);

            foreach (var point in allPoints)
            {
                if (point == null) continue;

                // Color code by type
                Gizmos.color = point.Type == ConnectionPoint.PointType.A
                    ? new Color(0, 1, 0, 0.5f) // Green for entrance
                    : new Color(1, 0, 0, 0.5f); // Red for exit

                // Draw sphere at point
                Gizmos.DrawSphere(point.transform.position, 0.3f);

                // Draw direction arrow
                Gizmos.color = connectionLineColor;
                Gizmos.DrawRay(point.transform.position, point.transform.forward * 2f);
            }

            // Draw lines between connected pieces
            Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            Corridor[] corridors = FindObjectsByType<Corridor>(FindObjectsSortMode.None);

            // Draw room boundaries
            foreach (var room in rooms)
            {
                if (room == null) continue;

                Gizmos.color = activeRoomColor;
                Bounds bounds = CalculateBounds(room.gameObject);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // Draw corridor paths
            Gizmos.color = connectionLineColor;
            foreach (var corridor in corridors)
            {
                if (corridor == null || corridor.PointA == null || corridor.PointB == null)
                    continue;

                Gizmos.DrawLine(
                    corridor.PointA.transform.position,
                    corridor.PointB.transform.position
                );
            }
        }

        private void OnGUI()
        {
            if (!showDebugUI) return;

            int y = 10;
            int lineHeight = 25;

            GUI.Box(new Rect(10, y, 350, 200), "Procedural Generation Debug");
            y += 30;

            // Player position
            if (player != null)
            {
                GUI.Label(new Rect(20, y, 330, 20), $"Player: {player.position:F1}");
                y += lineHeight;
            }

            // Room/Corridor count
            Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            Corridor[] corridors = FindObjectsByType<Corridor>(FindObjectsSortMode.None);

            GUI.Label(new Rect(20, y, 330, 20), $"Active Rooms: {rooms.Length}");
            y += lineHeight;

            GUI.Label(new Rect(20, y, 330, 20), $"Active Corridors: {corridors.Length}");
            y += lineHeight;

            // Connection points
            ConnectionPoint[] points = FindObjectsByType<ConnectionPoint>(FindObjectsSortMode.None);
            int entrances = 0, exits = 0;

            foreach (var point in points)
            {
                if (point.Type == ConnectionPoint.PointType.A) entrances++;
                else exits++;
            }

            GUI.Label(new Rect(20, y, 330, 20), $"Entrances: {entrances} | Exits: {exits}");
            y += lineHeight;

            // Memory stats
            GUI.Label(new Rect(20, y, 330, 20),
                $"Memory: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):F2} MB");
        }

        private Bounds CalculateBounds(GameObject obj)
        {
            Bounds bounds = new(obj.transform.position, Vector3.zero);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Call this to highlight a specific room in the scene
        /// </summary>
        public void HighlightRoom(Room room, float duration = 2f)
        {
            if (room == null) return;
            StartCoroutine(HighlightCoroutine(room.gameObject, duration));
        }

        private System.Collections.IEnumerator HighlightCoroutine(GameObject obj, float duration)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Color originalColor = Color.white;

            // Store original colors
            Material[] originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null)
                    originalMaterials[i] = renderers[i].material;
            }

            // Highlight
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float alpha = Mathf.PingPong(elapsed * 2f, 1f);
                Color highlight = Color.Lerp(originalColor, Color.yellow, alpha);

                foreach (var renderer in renderers)
                {
                    if (renderer.material != null)
                        renderer.material.color = highlight;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore original materials
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                    renderers[i].material = originalMaterials[i];
            }
        }
    }
}