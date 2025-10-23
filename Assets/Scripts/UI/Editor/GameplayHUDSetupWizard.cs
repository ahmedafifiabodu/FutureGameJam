using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Comprehensive setup wizard for the new Canvas-based Gameplay HUD system.
/// Automatically creates all required UI elements with proper layout and styling.
/// </summary>
public class GameplayHUDSetupWizard : EditorWindow
{
    private Canvas targetCanvas;
    private bool createNewCanvas = true;
    private bool includeDebugInfo = true;
    private bool includeCenterDot = false;
    
    [MenuItem("Tools/Gameplay/Setup Gameplay HUD")]
    public static void ShowWindow()
  {
        GameplayHUDSetupWizard window = GetWindow<GameplayHUDSetupWizard>("Gameplay HUD Setup");
    window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Gameplay HUD Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This wizard will create a complete Canvas-based HUD system with:\n" +
    "� Parasite UI Panel\n" +
  "� Host UI Panel\n" +
    "� Weapon UI Panel\n" +
   "� Dual-Progress Lifetime Sliders\n" +
     "� Canvas-based Crosshair", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
     createNewCanvas = EditorGUILayout.Toggle("Create New Canvas", createNewCanvas);
        
        if (!createNewCanvas)
        {
        targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);
        }
     
        EditorGUILayout.Space(5);
 includeDebugInfo = EditorGUILayout.Toggle("Include Debug Info", includeDebugInfo);
        includeCenterDot = EditorGUILayout.Toggle("Crosshair Center Dot", includeCenterDot);
        
        EditorGUILayout.Space(20);
    
  GUI.enabled = createNewCanvas || targetCanvas != null;
        
     if (GUILayout.Button("Create Gameplay HUD", GUILayout.Height(40)))
        {
     CreateGameplayHUD();
   }
        
 GUI.enabled = true;
     
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("After creation, assign the ParasiteController and RangedWeapon references in the Inspector.", MessageType.Warning);
    }
    
    private void CreateGameplayHUD()
    {
        // Step 1: Create or get canvas
  Canvas canvas = createNewCanvas ? CreateCanvas() : targetCanvas;
        
        if (canvas == null)
{
         EditorUtility.DisplayDialog("Error", "Failed to create or find Canvas!", "OK");
          return;
   }
        
     // Step 2: Create main HUD GameObject
 GameObject hudObject = new GameObject("GameplayHUD");
        hudObject.transform.SetParent(canvas.transform, false);
     
        RectTransform hudRect = hudObject.AddComponent<RectTransform>();
        SetFullScreenRect(hudRect);
      
  GameplayHUD hud = hudObject.AddComponent<GameplayHUD>();
        
        // Step 3: Create all UI panels
        GameObject parasitePanel = CreateParasitePanel(hudRect);
        GameObject hostPanel = CreateHostPanel(hudRect);
        GameObject weaponPanel = CreateWeaponPanel(hudRect);
   GameObject crosshairObject = CreateCrosshair(hudRect);
        
        // Step 4: Wire up references using SerializedObject
        SerializedObject serializedHUD = new SerializedObject(hud);
     
        serializedHUD.FindProperty("hudCanvas").objectReferenceValue = canvas;
        serializedHUD.FindProperty("parasitePanel").objectReferenceValue = parasitePanel;
    serializedHUD.FindProperty("hostPanel").objectReferenceValue = hostPanel;
     serializedHUD.FindProperty("weaponPanel").objectReferenceValue = weaponPanel;
      serializedHUD.FindProperty("crosshair").objectReferenceValue = crosshairObject.GetComponent<CanvasCrosshair>();
        
        // Wire up parasite panel references
        WireUpParasitePanel(serializedHUD, parasitePanel);
        
    // Wire up host panel references
   WireUpHostPanel(serializedHUD, hostPanel);
        
        // Wire up weapon panel references
        WireUpWeaponPanel(serializedHUD, weaponPanel);
        
   serializedHUD.ApplyModifiedProperties();
        
   // Mark scene as dirty
    EditorUtility.SetDirty(hud);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
   
        // Select the created HUD
     Selection.activeGameObject = hudObject;
        
   EditorUtility.DisplayDialog("Success", "Gameplay HUD created successfully!\n\nPlease assign:\n� ParasiteController\n� Current Host\n� Current Weapon\n\nin the Inspector.", "OK");
 }
    
    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("GameplayHUD_Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
     
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
      
  canvasObject.AddComponent<GraphicRaycaster>();
     
  return canvas;
    }
    
    private GameObject CreateParasitePanel(RectTransform parent)
    {
   GameObject panel = new GameObject("ParasitePanel");
     panel.transform.SetParent(parent, false);
     
     RectTransform rect = panel.AddComponent<RectTransform>();
        SetFullScreenRect(rect);
        
      // Create mode text (top-left)
     CreateText("ParasiteModeText", panel.transform, new Vector2(10, -10), new Vector2(300, 40), "Parasite Mode", 18, TextAlignmentOptions.TopLeft);
        
        // Create lifetime slider (bottom center)
        GameObject slider = CreateDualProgressSlider("ParasiteLifetimeSlider", panel.transform, new Vector2(0, 100), new Vector2(600, 20));
        
        if (includeDebugInfo)
        {
          // Create debug info text (bottom-left)
            CreateText("ParasiteDebugText", panel.transform, new Vector2(10, 50), new Vector2(400, 100), "Debug Info...", 14, TextAlignmentOptions.BottomLeft);
        }
        
        // Create status text (center)
        CreateText("ParasiteStatusText", panel.transform, new Vector2(0, 0), new Vector2(400, 60), "", 16, TextAlignmentOptions.Center);
     
        // Create cooldown text (center)
        CreateText("ParasiteCooldownText", panel.transform, new Vector2(0, -40), new Vector2(400, 40), "", 14, TextAlignmentOptions.Center);
        
        panel.SetActive(false); // Start disabled
        return panel;
    }
    
    private GameObject CreateHostPanel(RectTransform parent)
    {
        GameObject panel = new GameObject("HostPanel");
        panel.transform.SetParent(parent, false);
      
        RectTransform rect = panel.AddComponent<RectTransform>();
        SetFullScreenRect(rect);
        
      
        // Create lifetime slider (bottom center)
        GameObject slider = CreateDualProgressSlider("HostLifetimeSlider", panel.transform, new Vector2(0, 100), new Vector2(600, 20));
        
   // Create exit hint (top-right, below lifetime)
        CreateText("HostExitHintText", panel.transform, new Vector2(-220, -60), new Vector2(200, 30), "Hold RMB to Exit", 12, TextAlignmentOptions.TopRight);
        
     panel.SetActive(false); // Start disabled
        return panel;
    }
    
    private GameObject CreateWeaponPanel(RectTransform parent)
    {
        GameObject panel = new GameObject("WeaponPanel");
     panel.transform.SetParent(parent, false);
    
        RectTransform rect = panel.AddComponent<RectTransform>();
        SetFullScreenRect(rect);

        panel.SetActive(false); // Start disabled
     return panel;
    }
    
private GameObject CreateCrosshair(RectTransform parent)
    {
    GameObject crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(parent, false);
        
        RectTransform rect = crosshair.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
     rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(100, 100);
        
        CanvasCrosshair crosshairComponent = crosshair.AddComponent<CanvasCrosshair>();
     
        // Create crosshair lines
        RectTransform topLine = CreateCrosshairLine("TopLine", rect);
        RectTransform bottomLine = CreateCrosshairLine("BottomLine", rect);
        RectTransform leftLine = CreateCrosshairLine("LeftLine", rect);
        RectTransform rightLine = CreateCrosshairLine("RightLine", rect);
        
        Image centerDot = null;
        if (includeCenterDot)
        {
  GameObject dotObject = new GameObject("CenterDot");
            dotObject.transform.SetParent(rect, false);
            
            RectTransform dotRect = dotObject.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
       dotRect.anchoredPosition = Vector2.zero;
       dotRect.sizeDelta = new Vector2(2, 2);
     
            centerDot = dotObject.AddComponent<Image>();
       centerDot.color = Color.white;
   }
      
        // Wire up references
        crosshairComponent.EditorSetup(topLine, bottomLine, leftLine, rightLine, centerDot);
   
        return crosshair;
    }
    
  private GameObject CreateDualProgressSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
 // Create main slider object
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        
    RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    
        // Add the dual progress slider component
        DualProgressSlider slider = sliderObject.AddComponent<DualProgressSlider>();
        
      // Create background
        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(rect, false);
        RectTransform bgRect = bgObject.AddComponent<RectTransform>();
     SetFullScreenRect(bgRect);
      Image bgImage = bgObject.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
  
        // Create container for fills
     GameObject fillContainer = new GameObject("FillContainer");
        fillContainer.transform.SetParent(rect, false);
      RectTransform containerRect = fillContainer.AddComponent<RectTransform>();
        SetFullScreenRect(containerRect);
        
        // Create left fill
        GameObject leftFill = new GameObject("LeftFill");
    leftFill.transform.SetParent(containerRect, false);
    RectTransform leftRect = leftFill.AddComponent<RectTransform>();
  leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.5f, 1f);
        leftRect.pivot = new Vector2(1f, 0.5f);
        leftRect.anchoredPosition = Vector2.zero;
  leftRect.sizeDelta = Vector2.zero;
        Image leftImage = leftFill.AddComponent<Image>();
 leftImage.color = Color.green;
        leftImage.type = Image.Type.Filled;
  leftImage.fillMethod = Image.FillMethod.Horizontal;
        leftImage.fillOrigin = (int)Image.OriginHorizontal.Right;
      
        // Create right fill
        GameObject rightFill = new GameObject("RightFill");
        rightFill.transform.SetParent(containerRect, false);
        RectTransform rightRect = rightFill.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.5f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
  rightRect.pivot = new Vector2(0f, 0.5f);
        rightRect.anchoredPosition = Vector2.zero;
      rightRect.sizeDelta = Vector2.zero;
 Image rightImage = rightFill.AddComponent<Image>();
     rightImage.color = Color.green;
        rightImage.type = Image.Type.Filled;
        rightImage.fillMethod = Image.FillMethod.Horizontal;
        rightImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        
 // Wire up references
   slider.EditorSetup(leftImage, rightImage, bgImage);
  
        return sliderObject;
    }
    
    private RectTransform CreateCrosshairLine(string name, RectTransform parent)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent, false);
        
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(2, 10);
        
        Image image = line.AddComponent<Image>();
        image.color = Color.white;
        
        return rect;
    }
    
    private GameObject CreateText(string name, Transform parent, Vector2 position, Vector2 size, string text, int fontSize, TextAlignmentOptions alignment)
    {
GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        
        RectTransform rect = textObject.AddComponent<RectTransform>();
        
        // Set anchor based on alignment
        if (alignment == TextAlignmentOptions.TopLeft)
        {
         rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
     }
  else if (alignment == TextAlignmentOptions.TopRight)
        {
     rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
      rect.pivot = new Vector2(1f, 1f);
      }
   else if (alignment == TextAlignmentOptions.BottomLeft)
   {
       rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
         rect.pivot = new Vector2(0f, 0f);
        }
        else if (alignment == TextAlignmentOptions.BottomRight)
     {
            rect.anchorMin = new Vector2(1f, 0f);
  rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
     }
     else // Center
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
 rect.pivot = new Vector2(0.5f, 0.5f);
        }
      
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
   tmp.alignment = alignment;
        
        return textObject;
    }
    
  private GameObject CreateProgressBar(string name, Transform parent, Vector2 position, Vector2 size)
    {
      // Create container
        GameObject container = new GameObject(name + "_Container");
    container.transform.SetParent(parent, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f);
  containerRect.anchoredPosition = position;
        containerRect.sizeDelta = size;
    
        // Create background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(containerRect, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
 SetFullScreenRect(bgRect);
 Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        
        // Create fill
        GameObject fill = new GameObject(name);
        fill.transform.SetParent(containerRect, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
  SetFullScreenRect(fillRect);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.yellow;
  fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        
        return fill;
    }
    
    private void SetFullScreenRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
rect.anchorMax = Vector2.one;
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
  }
    
    private void WireUpParasitePanel(SerializedObject serializedHUD, GameObject panel)
    {
        serializedHUD.FindProperty("parasiteModeText").objectReferenceValue = panel.transform.Find("ParasiteModeText")?.GetComponent<TextMeshProUGUI>();
  serializedHUD.FindProperty("parasiteStatusText").objectReferenceValue = panel.transform.Find("ParasiteStatusText")?.GetComponent<TextMeshProUGUI>();
        serializedHUD.FindProperty("parasiteCooldownText").objectReferenceValue = panel.transform.Find("ParasiteCooldownText")?.GetComponent<TextMeshProUGUI>();
    serializedHUD.FindProperty("parasiteLifetimeSlider").objectReferenceValue = panel.transform.Find("ParasiteLifetimeSlider")?.GetComponent<DualProgressSlider>();
        
if (includeDebugInfo)
        {
            serializedHUD.FindProperty("parasiteDebugText").objectReferenceValue = panel.transform.Find("ParasiteDebugText")?.GetComponent<TextMeshProUGUI>();
        }
    }
    
    private void WireUpHostPanel(SerializedObject serializedHUD, GameObject panel)
    {
        serializedHUD.FindProperty("hostLifetimeText").objectReferenceValue = panel.transform.Find("HostLifetimeText")?.GetComponent<TextMeshProUGUI>();
        serializedHUD.FindProperty("hostExitHintText").objectReferenceValue = panel.transform.Find("HostExitHintText")?.GetComponent<TextMeshProUGUI>();
  serializedHUD.FindProperty("hostLifetimeSlider").objectReferenceValue = panel.transform.Find("HostLifetimeSlider")?.GetComponent<DualProgressSlider>();
    }
    
    private void WireUpWeaponPanel(SerializedObject serializedHUD, GameObject panel)
    {
 serializedHUD.FindProperty("ammoText").objectReferenceValue = panel.transform.Find("AmmoText")?.GetComponent<TextMeshProUGUI>();
        serializedHUD.FindProperty("reserveAmmoText").objectReferenceValue = panel.transform.Find("ReserveAmmoText")?.GetComponent<TextMeshProUGUI>();
        serializedHUD.FindProperty("aimingText").objectReferenceValue = panel.transform.Find("AimingText")?.GetComponent<TextMeshProUGUI>();
        serializedHUD.FindProperty("reloadProgressBar").objectReferenceValue = panel.transform.Find("WeaponPanel/ReloadProgressBar_Container/ReloadProgressBar")?.GetComponent<Image>();
        serializedHUD.FindProperty("reloadingText").objectReferenceValue = panel.transform.Find("ReloadingText")?.GetComponent<TextMeshProUGUI>();
    }
}
