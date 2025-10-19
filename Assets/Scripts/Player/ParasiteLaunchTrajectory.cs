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
    [SerializeField] private Color validLaunchColor = new(0.0f, 1f, 0.0f, 0.8f); // Bright green, higher alpha for dots

    [SerializeField] private Color invalidLaunchColor = new(1f, 0.0f, 0.0f, 0.8f); // Bright red, higher alpha for dots
    [SerializeField] private float lineStartWidth = 0.05f; // Dot size
    [SerializeField] private float lineEndWidth = 0.05f; // Consistent dot size
    [SerializeField] private float nearCameraFadeDistance = 0.5f; // Fade out line near camera
    [SerializeField] private bool enableNearCameraFade = true;

    [Header("Material Settings")]
    [SerializeField] private Material customLineMaterial; // Assign your own material here

    [SerializeField] private bool useCustomMaterial = false; // Toggle to use custom material

    [Tooltip("Tiling multiplier for the line texture/material. Higher values = more arrows. Lower = fewer arrows with more spacing.")]
    [SerializeField][Range(0.1f, 10f)] private float textureTiling = 1f;

    [Tooltip("Offset to animate or shift the texture along the line")]
    [SerializeField][Range(-1f, 1f)] private float textureOffset = 0f;

    [Header("Arrow Animation")]
    [Tooltip("Enable animated flowing arrows")]
    [SerializeField] private bool animateArrows = true;

    [Tooltip("Speed of arrow animation. Positive = forward, Negative = backward")]
    [SerializeField][Range(-5f, 5f)] private float arrowAnimationSpeed = 1f;

    [Tooltip("Start animating only when aiming")]
    [SerializeField] private bool animateOnlyWhenVisible = true;

    [Header("Trajectory Positioning")]
    [SerializeField] private float horizontalOffset = 0f; // Visual offset left (-) or right (+) relative to camera

    [SerializeField] private Vector3 trajectoryStartOffset = Vector3.zero; // Additional visual start position offset

    [Header("Landing Indicator")]
    [SerializeField] private GameObject landingIndicatorPrefab;

    [SerializeField] private float indicatorRadius = 0.25f;
    [SerializeField] private bool showLandingIndicator = true;
    [SerializeField] private float indicatorHeightOffset = 0.01f;
    [SerializeField] private float indicatorLineWidth = 0.02f;

    [Header("Point Visualization (Optional)")]
    [SerializeField] private int minimumDots = 5; // Ensure at least 5 dots even for short trajectories

    [SerializeField] private float dotSpacing = 0.3f; // Space between dots

    private PhysicsScene physicsScene;
    private GameObject landingIndicator;
    private Material lineMaterial;
    private LineRenderer indicatorLineRenderer;
    private Transform cameraTransform;
    private bool isUsingCustomMaterial = false;
    private float animatedTextureOffset = 0f; // Accumulated offset for animation

    private void Start()
    {
        CreatePhysicsScene();
        SetupLineRenderer();
        CreateLandingIndicator();

        // Cache camera transform for fade calculations
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private void Update()
    {
        // Animate arrow texture if enabled
        if (animateArrows && line != null && line.positionCount > 0)
        {
            // Only animate if visible or if we don't care about visibility
            if (!animateOnlyWhenVisible || line.enabled)
            {
                // Accumulate offset over time
                animatedTextureOffset += arrowAnimationSpeed * Time.deltaTime;

                // Keep offset in reasonable range to prevent floating point precision issues
                if (Mathf.Abs(animatedTextureOffset) > 100f)
                {
                    animatedTextureOffset = animatedTextureOffset % 1f;
                }

                // Apply animated offset to material
                if (line.material != null)
                {
                    float finalOffset = textureOffset + animatedTextureOffset;
                    line.material.mainTextureOffset = new Vector2(finalOffset, 0f);
                }
            }
        }
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
        line.textureMode = LineTextureMode.Tile; // IMPORTANT: Use Tile mode for proper arrow alignment

        // Check if we should use custom material
        if (useCustomMaterial && customLineMaterial != null)
        {
            // Use the assigned custom material
            lineMaterial = customLineMaterial;
            isUsingCustomMaterial = true;
        }
        else
        {
            // Generate material
            isUsingCustomMaterial = false;

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

            lineMaterial = new Material(lineShader)
            {
                renderQueue = 3000 // Transparent queue
            };

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

        // Apply texture tiling and offset
        UpdateTextureTiling();
    }

    /// <summary>
    /// Updates the texture tiling and offset on the line renderer material
    /// </summary>
    private void UpdateTextureTiling()
    {
        if (line != null && line.material != null)
        {
            // For LineRenderer with Tile mode, we need to set the tiling correctly
            // The X value controls repetition along the line
            // The Y value controls tiling perpendicular to the line (usually keep at 1)
            line.material.mainTextureScale = new Vector2(1f, 1f); // Reset to default first

            // Note: We'll update this dynamically in ApplyDottedLine based on trajectory length
        }
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

            Material indicatorMat = new(indicatorShader)
            {
                renderQueue = 3000
            };

            Color indicatorColor = new(validLaunchColor.r, validLaunchColor.g, validLaunchColor.b, 0.6f);
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

        // Calculate trajectory using the ACTUAL start position (no offset)
        // This ensures accurate physics prediction
        Vector3[] trajectoryPoints = new Vector3[maxPhysicsFrameIterations];
        bool hitTarget = false;
        int actualPoints = 0;
        Vector3 landingPosition = startPosition;
        Vector3 landingNormal = Vector3.up;
        bool foundLanding = false;

        Vector3 currentPos = startPosition; // Use actual position for physics
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
            if (Vector3.Distance(startPosition, currentPos) > maxDistance)
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

        // Calculate visual start position with offsets
        // This only affects how the trajectory line is displayed, not the physics
        Vector3 visualStartPosition = startPosition + trajectoryStartOffset;

        // Apply horizontal offset relative to camera's right direction (for FPS view)
        if (Mathf.Abs(horizontalOffset) > 0.001f && cameraTransform != null)
        {
            visualStartPosition += cameraTransform.right * horizontalOffset;
        }

        // Apply visual offset to trajectory line rendering
        ApplyDottedLine(trajectoryPoints, actualPoints, visualStartPosition);

        // Update line color based on valid distance AND hit target
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

        // Update landing indicator (uses ACTUAL landing position, not offset)
        if (foundLanding)
            UpdateLandingIndicator(landingPosition, landingNormal, hitTarget && isValidDistance);
        else
            HideLandingIndicator();
    }

    private void ApplyDottedLine(Vector3[] points, int pointCount, Vector3 visualStartPosition)
    {
        if (pointCount < 2)
        {
            line.positionCount = 0;
            return;
        }

        System.Collections.Generic.List<Vector3> dottedPoints = new System.Collections.Generic.List<Vector3>();

        // Calculate the offset for the start point only
        Vector3 startOffset = visualStartPosition - points[0];

        // Calculate total trajectory length for interpolation
        float totalDistance = 0f;
        for (int i = 0; i < pointCount - 1; i++)
            totalDistance += Vector3.Distance(points[i], points[i + 1]);

        // For very short trajectories, adjust dot spacing to ensure enough points
        float effectiveDotSpacing = dotSpacing;

        if (totalDistance / dotSpacing < minimumDots)
            effectiveDotSpacing = totalDistance / minimumDots;

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

                // Calculate how far along the trajectory we are (0 = start, 1 = end)
                float trajectoryProgress = currentDistance / totalDistance;

                // Apply offset that gradually reduces from start (full offset) to end (no offset)
                // This creates a diverging trajectory that converges back to the actual landing point
                Vector3 interpolatedOffset = Vector3.Lerp(startOffset, Vector3.zero, trajectoryProgress);
                Vector3 visualPoint = point + interpolatedOffset;

                // Skip points near camera
                if (!enableNearCameraFade || cameraTransform == null ||
                    Vector3.Distance(visualPoint, cameraTransform.position) >= nearCameraFadeDistance)
                    dottedPoints.Add(visualPoint);

                currentDistance += effectiveDotSpacing;
            }
        }

        line.positionCount = dottedPoints.Count;
        if (dottedPoints.Count > 0)
        {
            line.SetPositions(dottedPoints.ToArray());
        }

        // Update texture tiling for proper arrow spacing
        // In Tile mode, the texture repeats based on world-space distance
        if (line.material != null && totalDistance > 0f)
        {
            // For short distances, reduce tiling to prevent stretching
            // For longer distances, scale normally
            float baseRepetitions = Mathf.Max(1f, totalDistance * textureTiling);

            // Ensure minimum repetitions to prevent stretching on very short trajectories
            float minRepetitions = 2f;
            float finalRepetitions = Mathf.Max(minRepetitions, baseRepetitions);

            // Apply to material (for Tile mode, this affects world-space tiling)
            line.material.mainTextureScale = new Vector2(finalRepetitions, 1f);

            // Apply offset (animated offset will be added in Update method)
            if (!animateArrows)
                line.material.mainTextureOffset = new Vector2(textureOffset, 0f);
        }
    }

    private void UpdateLineColor(Color baseColor)
    {
        // Only update material colors if not using a custom material
        if (lineMaterial != null && !isUsingCustomMaterial)
        {
            lineMaterial.SetColor("_BaseColor", baseColor);
            lineMaterial.SetColor("_Color", baseColor);
            if (lineMaterial.HasProperty("_TintColor"))
            {
                lineMaterial.SetColor("_TintColor", baseColor);
            }
        }

        // Always update line renderer colors
        line.startColor = baseColor;
        line.endColor = baseColor;
    }

    private void UpdateLandingIndicator(Vector3 position, Vector3 normal, bool isValidTarget)
    {
        if (!showLandingIndicator || landingIndicator == null) return;

        landingIndicator.SetActive(true);
        landingIndicator.transform.SetPositionAndRotation(position + normal * indicatorHeightOffset, Quaternion.FromToRotation(Vector3.up, normal));
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
                    indicatorLineRenderer.material.SetColor("_TintColor", indicatorColor);
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
            landingIndicator.SetActive(false);
    }

    /// <summary>
    /// Hides the trajectory line
    /// </summary>
    public void HideTrajectory()
    {
        if (line != null)
            line.positionCount = 0;

        // Reset animation offset when hiding
        if (animateOnlyWhenVisible)
            animatedTextureOffset = 0f;

        HideLandingIndicator();
    }

    /// <summary>
    /// Shows the trajectory line (if it was hidden)
    /// </summary>
    public void ShowTrajectory()
    {
        if (line != null)
            line.enabled = true;
    }

    private void OnDestroy()
    {
        if (landingIndicator != null)
            Destroy(landingIndicator);

        // Only destroy the material if it was generated (not a custom material)
        if (lineMaterial != null && !isUsingCustomMaterial)
            Destroy(lineMaterial);
    }
}