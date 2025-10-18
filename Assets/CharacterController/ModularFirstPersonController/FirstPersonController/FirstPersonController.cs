// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Complete rewrite using Unity's CharacterController for perfectly smooth, frame-synchronous movement." || version 2.0.0

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
    using UnityEditor;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerControls input;

    #region Camera Movement Variables
    public Camera playerCamera;
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;
    #endregion

    #region Movement Variables
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    private Vector3 playerVelocity;
    public float gravity = -19.62f;
    #endregion

    #region Sprint
    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;
    #endregion

    #region Jump
    public bool enableJump = true;
    public float jumpPower = 8f;
    #endregion

    #region Crouch
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = 1.0f;
    public float standingHeight = 2.0f;
    public float crouchSpeedReduction = .5f;
    public float crouchTime = 0.25f;
    private bool isCrouched = false;
    private Coroutine crouchRoutine;
    #endregion
    
    #region Head Bob
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);
    private Vector3 jointOriginalPos;
    private float timer = 0;
    #endregion

    #region Camera Zoom
    public bool enableZoom = true;
    public bool holdToZoom = false;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;
    private bool isZoomed = false;
    #endregion

    private InputAction zoomAction;
    private InputAction crouchAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new PlayerControls();
        zoomAction = input.Gameplay.Zoom;
        crouchAction = input.Gameplay.Crouch;

        crosshairObject = GetComponentInChildren<Image>();
        playerCamera.fieldOfView = fov;
        if(joint) jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
    }

    void OnEnable() { input.Enable(); }
    void OnDisable() { input.Disable(); }

    void Start()
    {
        if (lockCursor) Cursor.lockState = CursorLockMode.Locked;
        controller.height = standingHeight;

        // Crosshair Setup
        if (crosshair && crosshairObject)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else if (crosshairObject)
        {
            crosshairObject.gameObject.SetActive(false);
        }

        // Sprint Bar Setup
        sprintBarCG = GetComponentInChildren<CanvasGroup>();
        if(useSprintBar && sprintBarBG && sprintBar)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);
            if(hideBarWhenFull) sprintBarCG.alpha = 0;
        }
        else if(sprintBarBG)
        {
            sprintBarBG.gameObject.SetActive(false);
            sprintBar.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // All logic is now driven from Update for perfect frame synchronization.
        HandleMovement();
        HandleCameraRotation();
        HandleZoom();
        HandleSprintLogic();
        HandleJumpInput();
        HandleCrouchInput();
        if (enableHeadBob) HandleHeadBob();
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        
        Vector2 moveInput = input.Gameplay.Move.ReadValue<Vector2>();
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        float currentSpeed = walkSpeed;
        isSprinting = enableSprint && input.Gameplay.Sprint.IsPressed() && !isSprintCooldown && moveInput.y > 0 && isGrounded && !isCrouched;
        if (isSprinting) currentSpeed = sprintSpeed;
        if (isCrouched) currentSpeed *= crouchSpeedReduction;

        if (playerCanMove)
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    
    private void HandleCameraRotation()
    {
        if (!cameraCanMove) return;

        Vector2 look = input.Gameplay.Look.ReadValue<Vector2>() * mouseSensitivity;
        yaw += look.x;
        pitch -= look.y * (invertCamera ? -1 : 1);
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        
        transform.localRotation = Quaternion.Euler(0, yaw, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    private void HandleJumpInput()
    {
        if (enableJump && input.Gameplay.Jump.triggered && controller.isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
            if (isCrouched && !holdToCrouch) StartCrouch();
        }
    }

    private void HandleCrouchInput()
    {
        if (!enableCrouch) return;

        if (holdToCrouch)
        {
            if (crouchAction.IsPressed() && !isCrouched) StartCrouch();
            else if (!crouchAction.IsPressed() && isCrouched) StartCrouch();
        }
        else
        {
            if (crouchAction.triggered) StartCrouch();
        }
    }

    private void StartCrouch()
    {
        if (crouchRoutine != null) StopCoroutine(crouchRoutine);
        crouchRoutine = StartCoroutine(CrouchRoutine());
    }

    private IEnumerator CrouchRoutine()
    {
        float targetHeight = isCrouched ? standingHeight : crouchHeight;
        float startHeight = controller.height;
        float timer = 0;

        while (timer < crouchTime)
        {
            controller.height = Mathf.Lerp(startHeight, targetHeight, timer / crouchTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        controller.height = targetHeight;
        isCrouched = !isCrouched;
    }

    private void HandleZoom()
    {
        if (!enableZoom) return;

        if (holdToZoom) isZoomed = zoomAction.IsPressed() && !isSprinting;
        else if (zoomAction.triggered && !isSprinting) isZoomed = !isZoomed;

        float targetFov = isSprinting ? sprintFOV : (isZoomed ? zoomFOV : fov);
        float zoomTime = isSprinting ? sprintFOVStepTime : zoomStepTime;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, zoomTime * Time.deltaTime);
    }

    private void HandleSprintLogic()
    {
        if (!enableSprint || unlimitedSprint) return;
        
        if (isSprinting)
        {
            sprintRemaining -= Time.deltaTime;
            if (sprintRemaining <= 0)
            {
                sprintRemaining = 0;
                isSprinting = false;
                isSprintCooldown = true;
            }
        }
        else
        {
            if(sprintRemaining < sprintDuration)
                sprintRemaining += Time.deltaTime;
        }

        if (isSprintCooldown)
        {
            sprintCooldown -= Time.deltaTime;
            if (sprintCooldown <= 0)
            {
                sprintCooldown = 0;
                isSprintCooldown = false;
            }
        }
        else
        {
            sprintCooldown = sprintCooldownReset;
        }
        
        if(useSprintBar && sprintBarCG && sprintBar)
        {
             if (hideBarWhenFull)
                sprintBarCG.alpha = (sprintRemaining < sprintDuration) ? 1 : 0;
            
            float sprintPercent = sprintRemaining / sprintDuration;
            sprintBar.transform.localScale = new Vector3(sprintPercent, 1, 1);
        }
    }

    private void HandleHeadBob()
    {
        if (!joint) return;
        
        bool isWalking = input.Gameplay.Move.ReadValue<Vector2>().sqrMagnitude > 0;

        if (isWalking && controller.isGrounded)
        {
            float speed = isSprinting ? bobSpeed * 1.5f : (isCrouched ? bobSpeed * 0.75f : bobSpeed);
            timer += Time.deltaTime * speed;
            joint.localPosition = new Vector3(
                jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x,
                jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y,
                jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z
            );
        }
        else
        {
            timer = 0;
            joint.localPosition = Vector3.Lerp(joint.localPosition, jointOriginalPos, Time.deltaTime * bobSpeed);
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
public class FirstPersonControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();
        GUILayout.Label("Modular First Person Controller", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUILayout.Label("By Jess Case (Refactored for CharacterController)", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        GUILayout.Label("version 2.0.0", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif