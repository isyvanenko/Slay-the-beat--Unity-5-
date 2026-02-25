using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SceneManagerWindow : EditorWindow
{
    // Add menu item to Window menu
    [MenuItem("Window/Scene Manager")]
    public static void ShowWindow()
    {
        // Get existing open window or create a new one
        EditorWindow.GetWindow(typeof(SceneManagerWindow), false, "Scene Manager");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Scene Management", EditorStyles.boldLabel);
        
        // Display all scenes in build settings
        DisplaySceneList();
        
        // Quick navigation buttons
        DisplayQuickNavigation();
        
        // Repaint to keep UI updated
        if (GUI.changed)
        {
            Repaint();
        }
    }
    
    void DisplaySceneList()
    {
        // Get all scenes from build settings
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        string[] scenePaths = new string[sceneCount];
        
        for (int i = 0; i < sceneCount; i++)
        {
            scenePaths[i] = SceneUtility.GetScenePathByBuildIndex(i);
        }
        
        // Display each scene
        foreach (string scenePath in scenePaths)
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            
            EditorGUILayout.BeginHorizontal();
            
            // Scene name with color coding based on load state
            GUI.color = scene.isLoaded ? Color.green : Color.gray;
            EditorGUILayout.LabelField(sceneName, GUILayout.Width(150));
            GUI.color = Color.white;
            
            // Load/Unload button
            if (scene.isLoaded)
            {
                if (GUILayout.Button("Unload", GUILayout.Width(60)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }
            
            // Set as active button
            if (scene.isLoaded && GUILayout.Button("Activate", GUILayout.Width(60)))
            {
                SceneManager.SetActiveScene(scene);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    void DisplayQuickNavigation()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("Quick Navigation", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save All Scenes"))
        {
            EditorSceneManager.SaveOpenScenes();
        }
        
        if (GUILayout.Button("New Scene"))
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }
        
        EditorGUILayout.EndHorizontal();
    }
}