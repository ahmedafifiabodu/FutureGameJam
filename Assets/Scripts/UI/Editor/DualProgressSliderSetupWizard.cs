using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor utility to quickly setup DualProgressSlider components.
/// Can be used standalone or as part of the main HUD wizard.
/// </summary>
public class DualProgressSliderSetupWizard : EditorWindow
{
    private Transform targetParent;
    private string sliderName = "DualProgressSlider";
    private Vector2 sliderSize = new Vector2(600, 20);
    private Vector2 sliderPosition = new Vector2(0, 100);
    private Color fillColor = Color.green;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    
    [MenuItem("GameObject/UI/Dual Progress Slider", false, 2)]
    public static void CreateDualProgressSliderFromMenu()
    {
        // Quick create from menu
 Transform parent = Selection.activeTransform;
        
        if (parent == null || parent.GetComponentInParent<Canvas>() == null)
        {
 EditorUtility.DisplayDialog("Error", "Please select a GameObject under a Canvas first!", "OK");
return;
    }
        
        CreateDualProgressSlider(parent, "DualProgressSlider", new Vector2(0, 100), new Vector2(600, 20), Color.green, new Color(0.2f, 0.2f, 0.2f, 0.5f));
    }
    
    [MenuItem("Tools/UI/Dual Progress Slider Setup")]
    public static void ShowWindow()
    {
     DualProgressSliderSetupWizard window = GetWindow<DualProgressSliderSetupWizard>("Dual Slider Setup");
        window.minSize = new Vector2(350, 400);
   window.Show();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dual Progress Slider Setup", EditorStyles.boldLabel);
    EditorGUILayout.HelpBox("Creates a slider that progresses from the center outward in both directions.\n" +
            "Perfect for time-based values that feel balanced!", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        targetParent = (Transform)EditorGUILayout.ObjectField("Parent Transform", targetParent, typeof(Transform), true);
        
   if (targetParent != null && targetParent.GetComponentInParent<Canvas>() == null)
      {
 EditorGUILayout.HelpBox("Parent must be under a Canvas!", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        
        sliderName = EditorGUILayout.TextField("Slider Name", sliderName);
    sliderSize = EditorGUILayout.Vector2Field("Size", sliderSize);
        sliderPosition = EditorGUILayout.Vector2Field("Position", sliderPosition);
        
        EditorGUILayout.Space(5);
        
        fillColor = EditorGUILayout.ColorField("Fill Color", fillColor);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        
        EditorGUILayout.Space(20);
    
        GUI.enabled = targetParent != null && targetParent.GetComponentInParent<Canvas>() != null;
        
        if (GUILayout.Button("Create Dual Progress Slider", GUILayout.Height(40)))
        {
         GameObject slider = CreateDualProgressSlider(targetParent, sliderName, sliderPosition, sliderSize, fillColor, backgroundColor);
      Selection.activeGameObject = slider;
            EditorUtility.DisplayDialog("Success", "Dual Progress Slider created successfully!", "OK");
        }
   
        GUI.enabled = true;
    
      EditorGUILayout.Space(10);
      
        if (GUILayout.Button("Setup Selected Slider"))
        {
       SetupExistingSlider();
        }
    }
    
    public static GameObject CreateDualProgressSlider(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor, Color bgColor)
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
        bgImage.color = bgColor;
        
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
  leftImage.color = fillColor;
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
        rightImage.color = fillColor;
     rightImage.type = Image.Type.Filled;
     rightImage.fillMethod = Image.FillMethod.Horizontal;
        rightImage.fillOrigin = (int)Image.OriginHorizontal.Left;
  
        // Wire up references using SerializedObject
        SerializedObject serializedSlider = new SerializedObject(slider);
        serializedSlider.FindProperty("leftFillImage").objectReferenceValue = leftImage;
        serializedSlider.FindProperty("rightFillImage").objectReferenceValue = rightImage;
        serializedSlider.FindProperty("backgroundImage").objectReferenceValue = bgImage;
        serializedSlider.FindProperty("fillColor").colorValue = fillColor;
        serializedSlider.FindProperty("backgroundColor").colorValue = bgColor;
 serializedSlider.ApplyModifiedProperties();
        
 EditorUtility.SetDirty(slider);
        
  return sliderObject;
    }
    
    private void SetupExistingSlider()
    {
        GameObject selected = Selection.activeGameObject;
        
   if (selected == null)
        {
    EditorUtility.DisplayDialog("Error", "Please select a GameObject with DualProgressSlider component!", "OK");
  return;
        }
      
      DualProgressSlider slider = selected.GetComponent<DualProgressSlider>();
        
        if (slider == null)
{
            EditorUtility.DisplayDialog("Error", "Selected GameObject doesn't have a DualProgressSlider component!", "OK");
      return;
        }
  
        // Try to find existing components
        Transform bgTransform = selected.transform.Find("Background");
        Transform containerTransform = selected.transform.Find("FillContainer");
        
     Image bgImage = bgTransform?.GetComponent<Image>();
Image leftImage = containerTransform?.Find("LeftFill")?.GetComponent<Image>();
   Image rightImage = containerTransform?.Find("RightFill")?.GetComponent<Image>();
        
        if (leftImage == null || rightImage == null)
        {
        EditorUtility.DisplayDialog("Error", "Could not find required child objects (LeftFill, RightFill)!", "OK");
        return;
        }
    
        // Wire up references using SerializedObject
   SerializedObject serializedSlider = new SerializedObject(slider);
     serializedSlider.FindProperty("leftFillImage").objectReferenceValue = leftImage;
        serializedSlider.FindProperty("rightFillImage").objectReferenceValue = rightImage;
        serializedSlider.FindProperty("backgroundImage").objectReferenceValue = bgImage;
        serializedSlider.ApplyModifiedProperties();
 
     EditorUtility.SetDirty(slider);
        
    EditorUtility.DisplayDialog("Success", "DualProgressSlider setup complete!", "OK");
    }
    
    private static void SetFullScreenRect(RectTransform rect)
 {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}

/// <summary>
/// Custom inspector for DualProgressSlider with helpful utilities.
/// </summary>
[CustomEditor(typeof(DualProgressSlider))]
public class DualProgressSliderEditor : Editor
{
    private float testProgress = 1f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

  DualProgressSlider slider = (DualProgressSlider)target;

        // Show current state
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
    EditorGUILayout.LabelField($"Current Progress: {slider.GetProgress():F2}");
        EditorGUILayout.LabelField($"Target Progress: {slider.GetTargetProgress():F2}");
        EditorGUILayout.EndVertical();
      
EditorGUILayout.Space(5);

        testProgress = EditorGUILayout.Slider("Test Progress", testProgress, 0f, 1f);

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Apply Test Progress"))
        {
    slider.SetProgressImmediate(testProgress);
 EditorUtility.SetDirty(slider);
 }
     
    if (GUILayout.Button("Apply Smoothly"))
        {
     slider.SetProgress(testProgress);
  }
        
  EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
   
        if (GUILayout.Button("0%"))
        {
   slider.SetProgressImmediate(0f);
      testProgress = 0f;
         EditorUtility.SetDirty(slider);
        }

        if (GUILayout.Button("25%"))
  {
            slider.SetProgressImmediate(0.25f);
     testProgress = 0.25f;
     EditorUtility.SetDirty(slider);
        }

        if (GUILayout.Button("50%"))
        {
      slider.SetProgressImmediate(0.5f);
         testProgress = 0.5f;
          EditorUtility.SetDirty(slider);
  }

        if (GUILayout.Button("75%"))
      {
         slider.SetProgressImmediate(0.75f);
   testProgress = 0.75f;
         EditorUtility.SetDirty(slider);
        }

        if (GUILayout.Button("100%"))
  {
            slider.SetProgressImmediate(1f);
 testProgress = 1f;
       EditorUtility.SetDirty(slider);
        }
  
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Color Tests", EditorStyles.boldLabel);
      
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Test Color: Green"))
        {
      slider.SetColor(Color.green);
     EditorUtility.SetDirty(slider);
   }

   if (GUILayout.Button("Test Color: Yellow"))
  {
            slider.SetColor(Color.yellow);
   EditorUtility.SetDirty(slider);
  }

     if (GUILayout.Button("Test Color: Red"))
        {
            slider.SetColor(Color.red);
    EditorUtility.SetDirty(slider);
        }
  
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Validation
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
   EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        
   SerializedProperty leftFill = serializedObject.FindProperty("leftFillImage");
        SerializedProperty rightFill = serializedObject.FindProperty("rightFillImage");
 
        bool isValid = leftFill.objectReferenceValue != null && rightFill.objectReferenceValue != null;
        
   if (isValid)
        {
        EditorGUILayout.HelpBox("? Slider is properly configured", MessageType.Info);
        }
     else
        {
    EditorGUILayout.HelpBox("? Missing fill images! Assign them or run the setup wizard.", MessageType.Error);
  }
    
   EditorGUILayout.EndVertical();
    }
}
