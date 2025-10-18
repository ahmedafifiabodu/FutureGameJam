using UnityEngine;

public static class Logging
{
    [System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Log(object message) => Debug.Log(message);

    [System.Diagnostics.Conditional("ENABLE_LOG")]
    internal static void LogWarning(object message) => Debug.LogWarning(message);

    [System.Diagnostics.Conditional("ENABLE_LOG")]
    internal static void LogError(object message) => Debug.LogError(message);

    [System.Diagnostics.Conditional("ENABLE_LOG")]
    internal static void DrawRay(Vector3 start, Vector3 direction, Color color) => Debug.DrawRay(start, direction, color);

    [System.Diagnostics.Conditional("ENABLE_LOG")]
    internal static void DrawDebugSphere(Vector3 position, float radius, Color color)
    {
        float theta = 0;
        float x = radius * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(theta);
        Vector3 pos = position + new Vector3(x, 0, y);
        Vector3 lastPos = pos;

        for (theta = 0.1f; theta < Mathf.PI * 2; theta += 0.1f)
        {
            x = radius * Mathf.Cos(theta);
            y = radius * Mathf.Sin(theta);
            Vector3 newPos = position + new Vector3(x, 0, y);
            Debug.DrawLine(pos, newPos, color, 2.0f);
            pos = newPos;
        }

        Debug.DrawLine(pos, lastPos, color, 2.0f);
    }

    [System.Diagnostics.Conditional("ENABLE_LOG")]
    internal static void PrintLayerMask(LayerMask layerMask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                Log($"Layer {i}: {layerName}");
            }
        }
    }
}