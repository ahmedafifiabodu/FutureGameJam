using UnityEngine;
using System.Collections;

namespace ProceduralGeneration
{
    /// <summary>
    /// Manages door state, animation, and visual feedback (lights)
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Header("Door Components")]
        [SerializeField] private Animator doorAnimator;

        [SerializeField] private Collider doorCollider;

        [Header("Door Lights")]
        [SerializeField] private Light[] doorLights; // Multiple lights (front/back)

        [SerializeField] private Renderer[] lightRenderers; // Multiple light renderers (front/back)
        [SerializeField] private string emissiveProperty = "_EmissionColor"; // Material emission property

        [Header("Light Colors")]
        [SerializeField] private Color closedLightColor = Color.red;

        [SerializeField] private Color openLightColor = Color.green;
        [SerializeField] private float lightIntensity = 2f;

        [Header("Next Area Generation")]
        [SerializeField] private bool isExitDoor = false; // Mark this as an exit door that should trigger next area

        [SerializeField] private float preGenerationDelay = 0.5f; // Time before door opens to generate next area
        [SerializeField] private float doorOpenDelay = 1.5f; // Additional delay after generation starts

        private bool isOpen = false;
        private Material[] lightMaterials;
        private ProceduralLevelGenerator levelGenerator;
        private ConnectionPoint associatedConnectionPoint;

        public bool IsOpen => isOpen;
        public bool IsExitDoor => isExitDoor;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (doorAnimator == null)
                doorAnimator = GetComponent<Animator>();

            if (doorCollider == null)
                doorCollider = GetComponent<Collider>();

            // Find the level generator
            levelGenerator = FindFirstObjectByType<ProceduralLevelGenerator>();

            // Find associated connection point (usually in parent or siblings)
            associatedConnectionPoint = GetComponentInParent<ConnectionPoint>();
            if (associatedConnectionPoint == null)
            {
                // Look for connection point in parent's parent (LevelPiece)
                var levelPiece = GetComponentInParent<LevelPiece>();
                if (levelPiece != null)
                {
                    // Check if this door is at Point B (exit)
                    if (levelPiece.PointB != null && Vector3.Distance(transform.position, levelPiece.PointB.transform.position) < 2f)
                    {
                        associatedConnectionPoint = levelPiece.PointB;
                        isExitDoor = true;
                    }
                    // Check if this door is at Point A (entrance)
                    else if (levelPiece.PointA != null && Vector3.Distance(transform.position, levelPiece.PointA.transform.position) < 2f)
                    {
                        associatedConnectionPoint = levelPiece.PointA;
                        isExitDoor = false;
                    }
                }
            }

            // Cache light materials if renderers are assigned
            if (lightRenderers != null && lightRenderers.Length > 0)
            {
                lightMaterials = new Material[lightRenderers.Length];
                for (int i = 0; i < lightRenderers.Length; i++)
                {
                    if (lightRenderers[i] != null)
                    {
                        lightMaterials[i] = lightRenderers[i].material;
                    }
                }
            }

            // Initialize door as closed
            SetLightColor(closedLightColor);
        }

        /// <summary>
        /// Opens the door and updates visual feedback
        /// For exit doors, this will trigger next area generation before opening
        /// </summary>
        public void Open()
        {
            if (isOpen) return;

            Debug.Log($"[Door] {gameObject.name} open requested (isExitDoor: {isExitDoor})");

            if (isExitDoor && levelGenerator != null && associatedConnectionPoint != null)
            {
                // Start the process with next area generation
                StartCoroutine(OpenExitDoorWithGeneration());
            }
            else
            {
                // Regular door opening (entrance doors, etc.)
                OpenImmediately();
            }
        }

        /// <summary>
        /// Opens exit door with next area generation to prevent skybox showing
        /// </summary>
        private IEnumerator OpenExitDoorWithGeneration()
        {
            Debug.Log($"[Door] {gameObject.name} starting exit door sequence with next area generation");

            // First, change light color to indicate door is preparing to open
            SetLightColor(Color.yellow); // Intermediate color

            // Wait for pre-generation delay
            yield return new WaitForSeconds(preGenerationDelay);

            // Trigger next area generation BEFORE door opens
            if (levelGenerator != null && associatedConnectionPoint != null)
            {
                Debug.Log($"[Door] {gameObject.name} triggering next area generation");

                // Use the new public method to trigger generation
                levelGenerator.TriggerNextAreaGeneration(associatedConnectionPoint);
            }
            else
            {
                Debug.LogWarning($"[Door] {gameObject.name} cannot trigger generation - missing levelGenerator or connectionPoint");
            }

            // Wait for door open delay to allow generation to complete
            yield return new WaitForSeconds(doorOpenDelay);

            // Now actually open the door
            OpenImmediately();
        }

        /// <summary>
        /// Immediately opens the door without any generation logic
        /// </summary>
        private void OpenImmediately()
        {
            isOpen = true;

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(GameConstant.AnimationParameters.Door.Open);
            }

            if (doorCollider != null)
            {
                doorCollider.isTrigger = true;
            }

            SetLightColor(openLightColor);
            Debug.Log($"[Door] {gameObject.name} opened");
        }

        /// <summary>
        /// Closes the door and updates visual feedback
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(GameConstant.AnimationParameters.Door.Close);
            }

            if (doorCollider != null)
            {
                doorCollider.isTrigger = false;
            }

            SetLightColor(closedLightColor);
            Debug.Log($"[Door] {gameObject.name} closed");
        }

        /// <summary>
        /// Sets the light color for visual feedback on all lights
        /// </summary>
        private void SetLightColor(Color color)
        {
            // Set Light component colors for all lights
            if (doorLights != null)
            {
                foreach (Light light in doorLights)
                {
                    if (light != null)
                    {
                        light.color = color;
                        light.intensity = lightIntensity;
                    }
                }
            }

            // Set emissive material colors for all light renderers
            if (lightMaterials != null)
            {
                for (int i = 0; i < lightMaterials.Length; i++)
                {
                    if (lightMaterials[i] != null)
                    {
                        lightMaterials[i].SetColor(emissiveProperty, color * lightIntensity);
                        lightMaterials[i].EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        private void OnValidate()
        {
            // Update light color in editor when values change
            if (Application.isPlaying)
            {
                SetLightColor(isOpen ? openLightColor : closedLightColor);
            }
        }

        /// <summary>
        /// Force set this door as an exit door (for debugging)
        /// </summary>
        [ContextMenu("Mark as Exit Door")]
        public void MarkAsExitDoor()
        {
            isExitDoor = true;
            Debug.Log($"[Door] {gameObject.name} marked as exit door");
        }
    }
}