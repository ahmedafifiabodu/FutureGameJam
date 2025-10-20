using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to quickly create weapon profile presets.
/// Access via: Tools > Weapons > Create Weapon Presets
/// Now supports both RangedWeaponProfile and ShootingFeedbackProfile!
/// </summary>
public class WeaponProfileCreator : EditorWindow
{
    private string weaponProfilePath = "Assets/Weapons/Profiles";
    private string feedbackProfilePath = "Assets/Weapons/Feedback";
    private Vector2 scrollPosition;

    [MenuItem("Tools/Weapons/Create Weapon Presets")]
    public static void ShowWindow()
    {
        GetWindow<WeaponProfileCreator>("Weapon Profile Creator");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Quick Weapon Profile Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // ========== RANGED WEAPON PROFILES ==========
        DrawSectionHeader("Ranged Weapon Profiles");

        GUILayout.Label("Save Location:", EditorStyles.label);
        weaponProfilePath = EditorGUILayout.TextField(weaponProfilePath);

        GUILayout.Space(5);
        GUILayout.Label("Click to create a weapon profile preset:", EditorStyles.helpBox);
        GUILayout.Space(5);

        if (GUILayout.Button("Create Pistol Profile", GUILayout.Height(30)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreatePistolPreset(), "Pistol_Profile");
        }

        if (GUILayout.Button("Create Rifle Profile", GUILayout.Height(30)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateRiflePreset(), "Rifle_Profile");
        }

        if (GUILayout.Button("Create Shotgun Profile", GUILayout.Height(30)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateShotgunPreset(), "Shotgun_Profile");
        }

        if (GUILayout.Button("Create Sniper Profile", GUILayout.Height(30)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateSniperPreset(), "Sniper_Profile");
        }

        if (GUILayout.Button("Create SMG Profile", GUILayout.Height(30)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateSMGPreset(), "SMG_Profile");
        }

        GUILayout.Space(20);

        // ========== SHOOTING FEEDBACK PROFILES ==========
        DrawSectionHeader("Shooting Feedback Profiles");

        GUILayout.Label("Save Location:", EditorStyles.label);
        feedbackProfilePath = EditorGUILayout.TextField(feedbackProfilePath);

        GUILayout.Space(5);
        GUILayout.Label("Click to create a feedback profile preset:", EditorStyles.helpBox);
        GUILayout.Space(5);

        if (GUILayout.Button("Create Pistol Feedback", GUILayout.Height(30)))
        {
            CreateFeedbackProfile(ShootingFeedbackProfile.CreatePistolPreset(), "Pistol_Feedback");
        }

        if (GUILayout.Button("Create Rifle Feedback", GUILayout.Height(30)))
        {
            CreateFeedbackProfile(ShootingFeedbackProfile.CreateRiflePreset(), "Rifle_Feedback");
        }

        if (GUILayout.Button("Create Shotgun Feedback", GUILayout.Height(30)))
        {
            CreateFeedbackProfile(ShootingFeedbackProfile.CreateShotgunPreset(), "Shotgun_Feedback");
        }

        if (GUILayout.Button("Create Sniper Feedback", GUILayout.Height(30)))
        {
            CreateFeedbackProfile(ShootingFeedbackProfile.CreateSniperPreset(), "Sniper_Feedback");
        }

        GUILayout.Space(20);

        // ========== BATCH CREATE ==========
        DrawSectionHeader("Batch Create (All Profiles)");

        GUILayout.Label("Create all weapon + feedback profiles at once:", EditorStyles.helpBox);
        GUILayout.Space(5);

        if (GUILayout.Button("Create All Pistol Profiles", GUILayout.Height(35)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreatePistolPreset(), "Pistol_Profile");
            CreateFeedbackProfile(ShootingFeedbackProfile.CreatePistolPreset(), "Pistol_Feedback");
        }

        if (GUILayout.Button("Create All Rifle Profiles", GUILayout.Height(35)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateRiflePreset(), "Rifle_Profile");
            CreateFeedbackProfile(ShootingFeedbackProfile.CreateRiflePreset(), "Rifle_Feedback");
        }

        if (GUILayout.Button("Create All Shotgun Profiles", GUILayout.Height(35)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateShotgunPreset(), "Shotgun_Profile");
            CreateFeedbackProfile(ShootingFeedbackProfile.CreateShotgunPreset(), "Shotgun_Feedback");
        }

        if (GUILayout.Button("Create All Sniper Profiles", GUILayout.Height(35)))
        {
            CreateWeaponProfile(RangedWeaponProfile.CreateSniperPreset(), "Sniper_Profile");
            CreateFeedbackProfile(ShootingFeedbackProfile.CreateSniperPreset(), "Sniper_Feedback");
        }

        GUILayout.Space(15);

        // ========== FOOTER ==========
        EditorGUILayout.HelpBox(
            "Tip: You can change the save paths above to organize your profiles.\n" +
            "Example: Assets/MyGame/Weapons/Configs/",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }

    private void CreateWeaponProfile(RangedWeaponProfile profile, string fileName)
    {
        // Validate and create directory
        string folderPath = ValidateAndCreateFolder(weaponProfilePath);
        if (folderPath == null) return;

        // Create unique file name
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{fileName}.asset");

        // Save asset
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select and ping the new asset
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);

        Debug.Log($"[WeaponProfileCreator] Created weapon profile at: {path}");
    }

    private void CreateFeedbackProfile(ShootingFeedbackProfile profile, string fileName)
    {
        // Validate and create directory
        string folderPath = ValidateAndCreateFolder(feedbackProfilePath);
        if (folderPath == null) return;

        // Create unique file name
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{fileName}.asset");

        // Save asset
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select and ping the new asset
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);

        Debug.Log($"[WeaponProfileCreator] Created feedback profile at: {path}");
    }

    private string ValidateAndCreateFolder(string path)
    {
        // Ensure path starts with Assets/
        if (!path.StartsWith("Assets/"))
        {
            EditorUtility.DisplayDialog(
                "Invalid Path",
                "Path must start with 'Assets/'\n\nExample: Assets/Weapons/Profiles",
                "OK"
            );
            return null;
        }

        // Create all necessary folders in the path
        string[] folders = path.Split('/');
        string currentPath = folders[0]; // Start with "Assets"

        for (int i = 1; i < folders.Length; i++)
        {
            string newPath = currentPath + "/" + folders[i];

            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
                Debug.Log($"[WeaponProfileCreator] Created folder: {newPath}");
            }

            currentPath = newPath;
        }

        return path;
    }
}
