using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles trajectory prediction for the ParasiteController's launch attack.
/// Shows a line renderer preview of where the parasite will jump.
/// </summary>
public class ParasiteLaunchTrajectory : MonoBehaviour
{
    [Header("Line Renderer")]
    [SerializeField] private LineRenderer line;

    [SerializeField] private int maxPhysicsFrameIterations = 50;
    [SerializeField] private int lineSegmentResolution = 25;

    [Header("Simulation Settings")]
    [SerializeField] private float simulationRadius = 0.2f;

    [SerializeField] private LayerMask simulationLayers;

    [Header("Visual Settings - FPS Optimized")]
    [SerializeField] private Color validLaunchColor = new Color(0.0f, 1f, 0.0f, 0.8f); // Bright green, higher alpha for dots

    [SerializeField] private Color invalidLaunchColor = new Color(1f, 0.0f, 0.0f, 0.8f); // Bright red, higher alpha for dots
    [SerializeField] private float lineStartWidth = 0.05f; // Dot size
    [SerializeField] private float lineEndWidth = 0.05f; // Consistent dot size
    [SerializeField] private float nearCameraFadeDistance = 0.5f; // Fade out line near camera
    [SerializeField] private bool enableNearCameraFade = true;

    [Header("Trajectory Positioning")]
    [SerializeField] private float horizontalOffset = 0f; // Offset trajectory left (-) or right (+) - set to 0 for accurate prediction

    [SerializeField] private Vector3 trajectoryStartOffset = Vector3.zero; // Additional start position offset

    [Header("Trajectory Curve")]
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.2f);

    [SerializeField] private bool useGradient = false; // Disable gradient for dots
    [SerializeField] private bool useSmoothColors = false; // Disable smooth colors for dots

    [Header("Landing Indicator")]
    [SerializeField] private GameObject landingIndicatorPrefab;

    [SerializeField] private float indicatorRadius = 0.25f;
    [SerializeField] private bool showLandingIndicator = true;
    [SerializeField] private float indicatorHeightOffset = 0.01f;
    [SerializeField] private float indicatorLineWidth = 0.02f;

    [Header("Point Visualization (Optional)")]
    [SerializeField] private bool useDottedLine = true; // Enable dotted line

    [SerializeField] private float dotSpacing = 0.3f; // Space between dots

    private PhysicsScene physicsScene;
    private GameObject landingIndicator;
    private Material lineMaterial;
    private LineRenderer indicatorLineRenderer;
    private Transform cameraTransform;

    private void Start()
    {
        CreatePhysicsScene();
        SetupLineRenderer();
        CreateLandingIndicator();

        // Cache camera transform for fade calculations
        cameraTransform = Camera.main?.transform;
    }

    private void CreatePhysicsScene()
    {
        var simulationScene = SceneManager.CreateScene("ParasiteTrajectorySimulation_" + GetInstanceID(),
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        physicsScene = simulationScene.GetPhysicsScene();
    }

    private void SetupLineRenderer()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
            }
        }

        // Apply width settings - small spherical dots
        line.startWidth = lineStartWidth;
        line.endWidth = lineEndWidth;

        // Quality settings for smooth dots
        line.numCornerVertices = 5; // Rounder dots
        line.numCapVertices = 5; // Rounded caps make spherical dots
        line.alignment = LineAlignment.View; // Always face camera
        line.textureMode = LineTextureMode.Stretch;

        // URP-compatible shader selection
        Shader lineShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (lineShader == null)
        {
            lineShader = Shader.Find("Particles/Standard Unlit");
        }
        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Color");
        }

        lineMaterial = new Material(lineShader);
        lineMaterial.renderQueue = 3000; // Transparent queue

        // Set initial color for URP
        lineMaterial.SetColor("_BaseColor", validLaunchColor);
        lineMaterial.SetColor("_Color", validLaunchColor);
        if (lineMaterial.HasProperty("_TintColor"))
        {
            lineMaterial.SetColor("_TintColor", validLaunchColor);
        }

        // Enable transparency for URP
        if (lineMaterial.HasProperty("_Surface"))
        {
            lineMaterial.SetFloat("_Surface", 1);
        }
        if (lineMaterial.HasProperty("_Blend"))
        {
            lineMaterial.SetFloat("_Blend", 0);
        }
        if (lineMaterial.HasProperty("_SrcBlend"))
        {
            lineMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }
        if (lineMaterial.HasProperty("_DstBlend"))
        {
            lineMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        if (lineMaterial.HasProperty("_ZWrite"))
        {
            lineMaterial.SetFloat("_ZWrite", 0);
        }

        line.material = lineMaterial;

        // Set solid colors for dots (no gradient fading)
        line.startColor = validLaunchColor;
        line.endColor = validLaunchColor;

        line.positionCount = 0;
        line.enabled = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.useWorldSpace = true;
    }

    private void CreateLandingIndicator()
    {
        if (!showLandingIndicator) return;

        if (landingIndicatorPrefab != null)
        {
            landingIndicator = Instantiate(landingIndicatorPrefab);
        }
        else
        {
            // Create a flat ring/circle indicator
            landingIndicator = new GameObject("LandingIndicator");
            indicatorLineRenderer = landingIndicator.AddComponent<LineRenderer>();

            // Create circle points
            int circleSegments = 20;
            indicatorLineRenderer.positionCount = circleSegments + 1;
            indicatorLineRenderer.loop = true;
            indicatorLineRenderer.useWorldSpace = false;

            for (int i = 0; i <= circleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / circleSegments;
                float x = Mathf.Cos(angle) * indicatorRadius;
                float z = Mathf.Sin(angle) * indicatorRadius;
                indicatorLineRenderer.SetPosition(i, new Vector3(x, 0, z));
            }

            indicatorLineRenderer.startWidth = indicatorLineWidth;
            indicatorLineRenderer.endWidth = indicatorLineWidth;

            // URP-compatible shader for indicator
            Shader indicatorShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (indicatorShader == null) indicatorShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (indicatorShader == null) indicatorShader = Shader.Find("Particles/Standard Unlit");
            if (indicatorShader == null) indicatorShader = Shader.Find("Unlit/Color");

            Material indicatorMat = new Material(indicatorShader);
            indicatorMat.renderQueue = 3000;

            Color indicatorColor = new Color(validLaunchColor.r, validLaunchColor.g, validLaunchColor.b, 0.6f);
            indicatorMat.SetColor("_BaseColor", indicatorColor);
            indicatorMat.SetColor("_Color", indicatorColor);
            if (indicatorMat.HasProperty("_TintColor"))
            {
                indicatorMat.SetColor("_TintColor", indicatorColor);
            }

            // Enable transparency for URP
            if (indicatorMat.HasProperty("_Surface"))
            {
                indicatorMat.SetFloat("_Surface", 1);
            }
            if (indicatorMat.HasProperty("_Blend"))
            {
                indicatorMat.SetFloat("_Blend", 0);
            }
            if (indicatorMat.HasProperty("_SrcBlend"))
            {
                indicatorMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (indicatorMat.HasProperty("_DstBlend"))
            {
                indicatorMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (indicatorMat.HasProperty("_ZWrite"))
            {
                indicatorMat.SetFloat("_ZWrite", 0);
            }

            indicatorLineRenderer.material = indicatorMat;
            indicatorLineRenderer.startColor = indicatorColor;
            indicatorLineRenderer.endColor = indicatorColor;
            indicatorLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            indicatorLineRenderer.receiveShadows = false;
            indicatorLineRenderer.numCornerVertices = 2;
            indicatorLineRenderer.numCapVertices = 0;
            indicatorLineRenderer.alignment = LineAlignment.TransformZ;
        }

        landingIndicator.SetActive(false);
    }

    /// <summary>
    /// Simulates and displays the trajectory of the parasite launch
    /// </summary>
    /// <param name="startPosition">Starting position of the parasite</param>
    /// <param name="velocity">Initial launch velocity</param>
    /// <param name="gravity">Gravity value to apply during simulation</param>
    /// <param name="maxDistance">Maximum distance to raycast for targets</param>
    /// <param name="targetLayerMask">Layer mask for potential targets</param>
    /// <param name="isValidDistance">Whether the target is at a valid distance to launch</param>
    public void SimulateTrajectory(Vector3 startPosition, Vector3 velocity, float gravity, float maxDistance, LayerMask targetLayerMask, bool isValidDistance = true)
    {
        if (line == null || !physicsScene.IsValid())
        {
            return;
        }

        // Apply offsets to starting position
        // NOTE: horizontalOffset should be 0 for accurate prediction matching actual launch
        // Any offset will cause the trajectory to show landing at a different position than actual
        Vector3 offsetStartPosition = startPosition + trajectoryStartOffset;

        // Apply horizontal offset relative to camera's right direction (for FPS view)
        // WARNING: Non-zero horizontalOffset will cause visual mismatch with actual landing position
        if (Mathf.Abs(horizontalOffset) > 0.001f && cameraTransform != null)
        {
            offsetStartPosition += cameraTransform.right * horizontalOffset;
        }

        Vector3[] trajectoryPoints = new Vector3[maxPhysicsFrameIterations];
        bool hitTarget = false;
        int actualPoints = 0;
        Vector3 landingPosition = offsetStartPosition;
        Vector3 landingNormal = Vector3.up;
        bool foundLanding = false;

        Vector3 currentPos = offsetStartPosition;
        Vector3 currentVelocity = velocity;

        // Simulate trajectory using simple physics
        for (int i = 0; i < maxPhysicsFrameIterations; i++)
        {
            // Apply gravity
            currentVelocity.y += gravity * Time.fixedDeltaTime;
            Vector3 nextPos = currentPos + currentVelocity * Time.fixedDeltaTime;

            // Check for collision along the path
            Vector3 direction = nextPos - currentPos;
            float distance = direction.magnitude;

            if (distance > 0.001f && Physics.Raycast(currentPos, direction.normalized, out RaycastHit hit, distance, simulationLayers))
            {
                trajectoryPoints[i] = hit.point;
                actualPoints = i + 1;
                landingPosition = hit.point;
                landingNormal = hit.normal;
                foundLanding = true;

                // Check if we hit a valid target
                if (((1 << hit.collider.gameObject.layer) & targetLayerMask) != 0)
                {
                    hitTarget = true;
                }

                break;
            }

            trajectoryPoints[i] = currentPos;
            actualPoints = i + 1;
            currentPos = nextPos;

            // Check if we've traveled too far
            if (Vector3.Distance(offsetStartPosition, currentPos) > maxDistance)
            {
                landingPosition = currentPos;
                break;
            }
        }

        // If no collision found, try to find ground below last point
        if (!foundLanding && actualPoints > 0)
        {
            if (Physics.Raycast(trajectoryPoints[actualPoints - 1], Vector3.down, out RaycastHit groundHit, 50f, simulationLayers))
            {
                landingPosition = groundHit.point;
                landingNormal = groundHit.normal;
                foundLanding = true;
            }
        }

        // Apply smooth curve to line renderer
        if (useDottedLine)
        {
            ApplyDottedLine(trajectoryPoints, actualPoints, offsetStartPosition);
        }
        else
        {
            ApplySmoothLine(trajectoryPoints, actualPoints, offsetStartPosition);
        }

        // Update line color based on valid distance AND hit target
        // If distance is invalid, always show invalid color
        // If distance is valid, show valid/invalid based on whether we hit a target
        Color lineColor;
        if (!isValidDistance)
        {
            // Too close - show warning color (yellow/orange)
            lineColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
        }
        else
        {
            // Valid distance - show green if hit target, red if missed
            lineColor = hitTarget ? validLaunchColor : invalidLaunchColor;
        }

        UpdateLineColor(lineColor);

        // Update landing indicator
        if (foundLanding)
        {
            UpdateLandingIndicator(landingPosition, landingNormal, hitTarget && isValidDistance);
        }
        else
        {
            HideLandingIndicator();
        }
    }

    private void ApplySmoothLine(Vector3[] points, int pointCount, Vector3 startPosition)
    {
        if (pointCount < 2)
        {
            line.positionCount = 0;
            return;
        }

        // Use fewer points but smooth them out
        int smoothPoints = Mathf.Min(lineSegmentResolution, pointCount);
        line.positionCount = smoothPoints;

        for (int i = 0; i < smoothPoints; i++)
        {
            float t = i / (float)(smoothPoints - 1);
            int index = Mathf.FloorToInt(t * (pointCount - 1));
            index = Mathf.Clamp(index, 0, pointCount - 1);

            Vector3 smoothPoint;
            if (index < pointCount - 1)
            {
                float localT = (t * (pointCount - 1)) - index;
                smoothPoint = Vector3.Lerp(points[index], points[index + 1], localT);
            }
            else
            {
                smoothPoint = points[index];
            }

            // Skip points very close to camera for cleaner FPS view
            if (enableNearCameraFade && cameraTransform != null)
            {
                float distToCamera = Vector3.Distance(smoothPoint, cameraTransform.position);
                if (distToCamera < nearCameraFadeDistance)
                {
                    continue; // Skip this point
                }
            }

            line.SetPosition(i, smoothPoint);
        }
    }

    private void ApplyDottedLine(Vector3[] points, int pointCount, Vector3 startPosition)
    {
        if (pointCount < 2)
        {
            line.positionCount = 0;
            return;
        }

        System.Collections.Generic.List<Vector3> dottedPoints = new System.Collections.Generic.List<Vector3>();

        float totalDistance = 0f;
        for (int i = 0; i < pointCount - 1; i++)
        {
            totalDistance += Vector3.Distance(points[i], points[i + 1]);
        }

        float currentDistance = 0f;
        int segmentIndex = 0;

        while (currentDistance < totalDistance && segmentIndex < pointCount - 1)
        {
            Vector3 segmentStart = points[segmentIndex];
            Vector3 segmentEnd = points[segmentIndex + 1];
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

            while (currentDistance < totalDistance)
            {
                float t = (currentDistance - (segmentIndex > 0 ? Vector3.Distance(points[0], points[segmentIndex]) : 0f)) / segmentLength;

                if (t > 1f)
                {
                    segmentIndex++;
                    break;
                }

                Vector3 point = Vector3.Lerp(segmentStart, segmentEnd, t);

                // Skip points near camera
                if (!enableNearCameraFade || cameraTransform == null ||
                    Vector3.Distance(point, cameraTransform.position) >= nearCameraFadeDistance)
                {
                    dottedPoints.Add(point);
                }

                currentDistance += dotSpacing;
            }
        }

        line.positionCount = dottedPoints.Count;
        if (dottedPoints.Count > 0)
        {
            line.SetPositions(dottedPoints.ToArray());
        }
    }

    private void UpdateLineColor(Color baseColor)
    {
        if (lineMaterial != null)
        {
            lineMaterial.SetColor("_BaseColor", baseColor);
            lineMaterial.SetColor("_Color", baseColor);
            if (lineMaterial.HasProperty("_TintColor"))
            {
                lineMaterial.SetColor("_TintColor", baseColor);
            }
        }

        // Simple solid color for dots
        line.startColor = baseColor;
        line.endColor = baseColor;
    }

    private void UpdateLandingIndicator(Vector3 position, Vector3 normal, bool isValidTarget)
    {
        if (!showLandingIndicator || landingIndicator == null) return;

        landingIndicator.SetActive(true);
        landingIndicator.transform.position = position + normal * indicatorHeightOffset;
        landingIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);

        Color indicatorColor = isValidTarget ? validLaunchColor : invalidLaunchColor;
        indicatorColor.a = 0.6f; // Semi-transparent indicator

        if (indicatorLineRenderer != null)
        {
            indicatorLineRenderer.startColor = indicatorColor;
            indicatorLineRenderer.endColor = indicatorColor;

            if (indicatorLineRenderer.material != null)
            {
                indicatorLineRenderer.material.SetColor("_BaseColor", indicatorColor);
                indicatorLineRenderer.material.SetColor("_Color", indicatorColor);
                if (indicatorLineRenderer.material.HasProperty("_TintColor"))
                {
                    indicatorLineRenderer.material.SetColor("_TintColor", indicatorColor);
                }
            }
        }
        else
        {
            var renderer = landingIndicator.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.SetColor("_BaseColor", indicatorColor);
                renderer.material.color = indicatorColor;
            }
        }
    }

    private void HideLandingIndicator()
    {
        if (landingIndicator != null)
        {
            landingIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Hides the trajectory line
    /// </summary>
    public void HideTrajectory()
    {
        if (line != null)
        {
            line.positionCount = 0;
        }

        HideLandingIndicator();
    }

    /// <summary>
    /// Shows the trajectory line (if it was hidden)
    /// </summary>
    public void ShowTrajectory()
    {
        if (line != null)
        {
            line.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (landingIndicator != null)
        {
            Destroy(landingIndicator);
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}