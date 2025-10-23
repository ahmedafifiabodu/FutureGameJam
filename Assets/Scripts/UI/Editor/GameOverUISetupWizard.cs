using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor wizard for setting up the Game Over UI
/// Automatically creates and configures all required UI elements
/// </summary>
public class GameOverUISetupWizard : EditorWindow
{
    private Canvas targetCanvas;
    private GameObject gameOverUIObject;
    private bool useTextMeshPro = true;
    private bool createAudioSource = true;
    private bool createScreenOverlay = true;
    private AudioClip gameOverSound;
    private string mainMenuSceneName = "MainMenu";

    // UI Style Settings
    private Color titleColor = Color.red;
    private Color textColor = Color.white;
    private Color buttonNormalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color buttonHighlightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    private Color buttonPressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private Color panelColor = new Color(0, 0, 0, 0.95f);

    [MenuItem("Tools/Game Over UI/Setup Wizard")]
    public static void ShowWindow()
    {
        GameOverUISetupWizard window = GetWindow<GameOverUISetupWizard>("Game Over UI Setup");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Over UI Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
   "This wizard will create a complete Game Over UI with all required components.\n" +
        "Make sure you have a Canvas in your scene or one will be created.",
      MessageType.Info);

 EditorGUILayout.Space();

        // Canvas Selection
        EditorGUILayout.LabelField("Canvas Setup", EditorStyles.boldLabel);
        targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);

        if (targetCanvas == null)
      {
        EditorGUILayout.HelpBox("No canvas selected. A new canvas will be created.", MessageType.Warning);
    }

        EditorGUILayout.Space();

      // Options
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
 useTextMeshPro = EditorGUILayout.Toggle("Use TextMeshPro", useTextMeshPro);
     createAudioSource = EditorGUILayout.Toggle("Create Audio Source", createAudioSource);
        createScreenOverlay = EditorGUILayout.Toggle("Create Screen Overlay", createScreenOverlay);

        EditorGUILayout.Space();

  // Settings
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
  gameOverSound = (AudioClip)EditorGUILayout.ObjectField("Game Over Sound", gameOverSound, typeof(AudioClip), false);
    mainMenuSceneName = EditorGUILayout.TextField("Main Menu Scene", mainMenuSceneName);

        EditorGUILayout.Space();

        // Style Settings
        EditorGUILayout.LabelField("Style Settings", EditorStyles.boldLabel);
        titleColor = EditorGUILayout.ColorField("Title Color", titleColor);
        textColor = EditorGUILayout.ColorField("Text Color", textColor);
      panelColor = EditorGUILayout.ColorField("Panel Color", panelColor);
   buttonNormalColor = EditorGUILayout.ColorField("Button Normal", buttonNormalColor);
        buttonHighlightColor = EditorGUILayout.ColorField("Button Highlight", buttonHighlightColor);
        buttonPressedColor = EditorGUILayout.ColorField("Button Pressed", buttonPressedColor);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

  // Create Button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Game Over UI", GUILayout.Height(40)))
      {
            CreateGameOverUI();
 }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // Additional buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find Existing Setup"))
      {
            FindExistingSetup();
        }
        if (gameOverUIObject != null)
        {
            if (GUILayout.Button("Select in Hierarchy"))
        {
   Selection.activeGameObject = gameOverUIObject;
  EditorGUIUtility.PingObject(gameOverUIObject);
   }
  }
        EditorGUILayout.EndHorizontal();

    if (gameOverUIObject != null)
        {
       EditorGUILayout.Space();
    EditorGUILayout.HelpBox($"Game Over UI found: {gameOverUIObject.name}", MessageType.Info);
   }
    }

    private void CreateGameOverUI()
    {
     // Find or create canvas
        if (targetCanvas == null)
  {
 targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas == null)
  {
                targetCanvas = CreateCanvas();
   }
        }

     // Check if Game Over UI already exists
        GameOverUI existingUI = FindFirstObjectByType<GameOverUI>();
        if (existingUI != null)
        {
  if (!EditorUtility.DisplayDialog("Game Over UI Exists",
            "A Game Over UI already exists in the scene. Do you want to replace it?",
          "Replace", "Cancel"))
        {
        return;
            }

            DestroyImmediate(existingUI.gameObject);
        }

// Create main game over UI object
        gameOverUIObject = new GameObject("GameOverUI");
        gameOverUIObject.transform.SetParent(targetCanvas.transform, false);

      // Add GameOverUI component
        GameOverUI gameOverUI = gameOverUIObject.AddComponent<GameOverUI>();

        // Create UI structure
        GameObject panel = CreatePanel(gameOverUIObject.transform);
        GameObject screenOverlay = createScreenOverlay ? CreateScreenOverlay(gameOverUIObject.transform) : null;

        // Create content
        GameObject titleObj = CreateTitle(panel.transform);
      GameObject statsContainer = CreateStatsContainer(panel.transform);
        GameObject buttonContainer = CreateButtonContainer(panel.transform);

        // Create stats texts
        GameObject hostsText = CreateStatText(statsContainer.transform, "HostsConsumedText", "Hosts Consumed: 0");
GameObject timeText = CreateStatText(statsContainer.transform, "SurvivalTimeText", "Survival Time: 00:00");
        GameObject scoreText = CreateStatText(statsContainer.transform, "FinalScoreText", "Final Score: 0");

   // Create buttons
        GameObject restartButton = CreateButton(buttonContainer.transform, "RestartButton", "RESTART");
        GameObject mainMenuButton = CreateButton(buttonContainer.transform, "MainMenuButton", "MAIN MENU");
        GameObject quitButton = CreateButton(buttonContainer.transform, "QuitButton", "QUIT");

        // Setup audio
        AudioSource audioSource = null;
        if (createAudioSource)
        {
         audioSource = gameOverUIObject.AddComponent<AudioSource>();
      audioSource.playOnAwake = false;
    }

  // Assign references to GameOverUI component using SerializedObject
        SerializedObject serializedUI = new SerializedObject(gameOverUI);

        serializedUI.FindProperty("gameOverPanel").objectReferenceValue = panel;
        serializedUI.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();

      if (useTextMeshPro)
        {
     serializedUI.FindProperty("gameOverTitle").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
          serializedUI.FindProperty("hostsConsumedText").objectReferenceValue = hostsText.GetComponent<TextMeshProUGUI>();
         serializedUI.FindProperty("survivalTimeText").objectReferenceValue = timeText.GetComponent<TextMeshProUGUI>();
            serializedUI.FindProperty("finalScoreText").objectReferenceValue = scoreText.GetComponent<TextMeshProUGUI>();
        }
        else
 {
      serializedUI.FindProperty("gameOverTitle").objectReferenceValue = titleObj.GetComponent<Text>();
      serializedUI.FindProperty("hostsConsumedText").objectReferenceValue = hostsText.GetComponent<Text>();
            serializedUI.FindProperty("survivalTimeText").objectReferenceValue = timeText.GetComponent<Text>();
       serializedUI.FindProperty("finalScoreText").objectReferenceValue = scoreText.GetComponent<Text>();
        }

        serializedUI.FindProperty("restartButton").objectReferenceValue = restartButton.GetComponent<Button>();
        serializedUI.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton.GetComponent<Button>();
      serializedUI.FindProperty("quitButton").objectReferenceValue = quitButton.GetComponent<Button>();

        if (createAudioSource)
        {
      serializedUI.FindProperty("audioSource").objectReferenceValue = audioSource;
    }

        if (gameOverSound != null)
        {
            serializedUI.FindProperty("gameOverSound").objectReferenceValue = gameOverSound;
        }

        if (screenOverlay != null)
        {
      serializedUI.FindProperty("screenOverlay").objectReferenceValue = screenOverlay.GetComponent<Image>();
        }

        serializedUI.FindProperty("mainMenuSceneName").stringValue = mainMenuSceneName;

        serializedUI.ApplyModifiedProperties();

        // Mark as dirty
        EditorUtility.SetDirty(gameOverUI);

// Select the created object
        Selection.activeGameObject = gameOverUIObject;

  // Ping in hierarchy
    EditorGUIUtility.PingObject(gameOverUIObject);

        Debug.Log("[GameOverUISetup] Game Over UI created successfully!");

        EditorUtility.DisplayDialog("Success",
            "Game Over UI has been created successfully!\n\n" +
            "The UI is currently hidden. It will automatically show when the player dies.\n\n" +
        "You can test it using the context menu functions on the GameOverUI component.",
         "OK");
    }

    private Canvas CreateCanvas()
    {
     GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("[GameOverUISetup] Created new Canvas");
        return canvas;
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = panel.AddComponent<Image>();
     image.color = panelColor;

        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Start hidden
        panel.SetActive(false);

        return panel;
    }

    private GameObject CreateScreenOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("ScreenOverlay");
        overlay.transform.SetParent(parent, false);
        overlay.transform.SetSiblingIndex(0); // Behind panel

        RectTransform rect = overlay.AddComponent<RectTransform>();
  rect.anchorMin = Vector2.zero;
  rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = overlay.AddComponent<Image>();
      image.color = new Color(0, 0, 0, 0.7f);

  overlay.SetActive(false);

   return overlay;
    }

    private GameObject CreateTitle(Transform parent)
    {
        GameObject title = new GameObject("Title");
      title.transform.SetParent(parent, false);

        RectTransform rect = title.AddComponent<RectTransform>();
      rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
   rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -50);
        rect.sizeDelta = new Vector2(600, 100);

        if (useTextMeshPro)
        {
   TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
        text.text = "GAME OVER";
            text.fontSize = 72;
            text.color = titleColor;
            text.alignment = TextAlignmentOptions.Center;
     text.fontStyle = FontStyles.Bold;
        }
        else
        {
   Text text = title.AddComponent<Text>();
            text.text = "GAME OVER";
            text.fontSize = 60;
       text.color = titleColor;
        text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
     text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

        return title;
    }

    private GameObject CreateStatsContainer(Transform parent)
    {
        GameObject container = new GameObject("StatsContainer");
    container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
      rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(500, 200);

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
      layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20;
 layout.childForceExpandHeight = false;
        layout.childControlHeight = true;

        return container;
    }

    private GameObject CreateStatText(Transform parent, string name, string defaultText)
    {
 GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

     RectTransform rect = textObj.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(400, 40);

        if (useTextMeshPro)
        {
      TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
         text.fontSize = 32;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;
        }
        else
        {
     Text text = textObj.AddComponent<Text>();
      text.text = defaultText;
        text.fontSize = 28;
            text.color = textColor;
 text.alignment = TextAnchor.MiddleCenter;
   text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

 return textObj;
    }

    private GameObject CreateButtonContainer(Transform parent)
    {
      GameObject container = new GameObject("ButtonContainer");
     container.transform.SetParent(parent, false);

      RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 50);
        rect.sizeDelta = new Vector2(600, 80);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
 layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20;
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;

return container;
    }

    private GameObject CreateButton(Transform parent, string name, string buttonText)
    {
        GameObject buttonObj = new GameObject(name);
     buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
   rect.sizeDelta = new Vector2(180, 60);

     Image image = buttonObj.AddComponent<Image>();
        image.color = buttonNormalColor;

        Button button = buttonObj.AddComponent<Button>();

        // Setup button colors
        ColorBlock colors = button.colors;
  colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHighlightColor;
      colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHighlightColor;
        button.colors = colors;

 // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

      RectTransform textRect = textObj.AddComponent<RectTransform>();
     textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
    textRect.sizeDelta = Vector2.zero;

        if (useTextMeshPro)
        {
   TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 24;
            text.color = Color.white;
  text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }
  else
        {
 Text text = textObj.AddComponent<Text>();
            text.text = buttonText;
     text.fontSize = 20;
       text.color = Color.white;
   text.alignment = TextAnchor.MiddleCenter;
     text.fontStyle = FontStyle.Bold;
     text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      }

        return buttonObj;
    }

    private void FindExistingSetup()
    {
        GameOverUI existingUI = FindFirstObjectByType<GameOverUI>();
        if (existingUI != null)
{
     gameOverUIObject = existingUI.gameObject;
 Selection.activeGameObject = gameOverUIObject;
        EditorGUIUtility.PingObject(gameOverUIObject);
        Debug.Log("[GameOverUISetup] Found existing Game Over UI");
        }
        else
        {
     EditorUtility.DisplayDialog("Not Found",
     "No Game Over UI found in the scene.",
    "OK");
      }
    }
}
