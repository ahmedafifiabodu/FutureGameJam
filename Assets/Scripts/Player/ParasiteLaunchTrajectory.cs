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

    [Header("Arrow Animation")]
    [SerializeField] private bool animateArrows = true;

    [SerializeField][Range(-5f, 5f)] private float arrowAnimationSpeed = 1f;
    [SerializeField] private bool animateOnlyWhenVisible = true;
    [SerializeField] private Texture2D arrowTexture; // Assign your arrow texture here in inspector
    [SerializeField] private Material lineMaterial; // Optional: Assign your existing material here

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
    private LineRenderer indicatorLineRenderer;
    private Transform cameraTransform;
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

                // Apply the texture offset to the material
                if (line.material != null)
                {
                    line.material.mainTextureOffset = new Vector2(animatedTextureOffset, 0f);
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

        // Use existing material if assigned, otherwise create a new one
        if (lineMaterial != null)
        {
            // Use the assigned material
            line.material = lineMaterial;
        }
        else if (line.material == null || line.sharedMaterial == null)
        {
            // Create a material with proper shader for color tinting
            Shader lineShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lineShader == null) lineShader = Shader.Find("Particles/Standard Unlit");
            if (lineShader == null) lineShader = Shader.Find("Sprites/Default"); // Good fallback with color tinting
            if (lineShader == null) lineShader = Shader.Find("Unlit/Transparent");

            if (lineShader != null)
            {
                Material newLineMaterial = new Material(lineShader);
                
                // Enable transparency
                if (newLineMaterial.HasProperty("_Surface"))
                {
                    newLineMaterial.SetFloat("_Surface", 1); // Transparent
                }
                if (newLineMaterial.HasProperty("_Blend"))
                {
                    newLineMaterial.SetFloat("_Blend", 0); // Alpha blend
                }
                if (newLineMaterial.HasProperty("_SrcBlend"))
                {
                    newLineMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                }
                if (newLineMaterial.HasProperty("_DstBlend"))
                {
                    newLineMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                if (newLineMaterial.HasProperty("_ZWrite"))
                {
                    newLineMaterial.SetFloat("_ZWrite", 0);
                }

                // Enable color tinting from vertex colors
                if (newLineMaterial.HasProperty("_ColorMode"))
                {
                    newLineMaterial.SetFloat("_ColorMode", 1); // Multiply mode
                }

                // Assign arrow texture if provided
                if (arrowTexture != null)
                {
                    newLineMaterial.mainTexture = arrowTexture;
                    if (newLineMaterial.HasProperty("_BaseMap"))
                    {
                        newLineMaterial.SetTexture("_BaseMap", arrowTexture);
                    }
                }

                newLineMaterial.renderQueue = 3000; // Render in transparent queue
                line.material = newLineMaterial;
            }
        }

        // Set solid colors for dots (no gradient fading)
        line.startColor = validLaunchColor;
        line.endColor = validLaunchColor;

        line.positionCount = 0;
        line.enabled = true;
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
    }

    private void UpdateLineColor(Color baseColor)
    {
        line.startColor = baseColor;
        line.endColor = baseColor;

        // Also set color on the material for shader-based rendering
        if (line.material != null)
        {
            // Try different common color properties
            if (line.material.HasProperty("_BaseColor"))
            {
                line.material.SetColor("_BaseColor", baseColor);
            }
            if (line.material.HasProperty("_Color"))
            {
                line.material.SetColor("_Color", baseColor);
            }
            if (line.material.HasProperty("_TintColor"))
            {
                line.material.SetColor("_TintColor", baseColor);
            }
        }
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

    private void OnDestroy()
    {
        if (landingIndicator != null)
            Destroy(landingIndicator);
    }
}