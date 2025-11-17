using UnityEngine;

/// <summary>
/// This is a generic base class for creating a persistent "lazy" singleton.
/// It will ensure only one instance of the class exists and that
/// it is not destroyed when loading new scenes.
/// 
/// If 'Instance' is accessed and no instance exists, it will:
/// 1. Try to find an existing instance in the scene.
/// 2. If none is found, try to load and instantiate a prefab from "Resources/Singletons/[ClassName]".
/// 3. If no prefab is found, create a new blank GameObject and add the component.
/// </summary>
/// <typeparam name="T">The type of the singleton class.</typeparam>
public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // The private backing field for the singleton instance
    private static T _instance;
    
    // A lock object for thread-safety, to prevent race conditions
    private static readonly object _lock = new object();

    /// <summary>
    /// The static instance of the singleton.
    /// This is public so it can be accessed from anywhere (e.g., MusicManager.Instance.PlayMusic()).
    /// If no instance exists in the scene, a new one will be created automatically.
    /// </summary>
    public static T Instance
    {
        get
        {
            // Double-check lock for thread safety
            if (_instance == null)
            {
                // Lock the thread to prevent multiple threads
                // from creating an instance simultaneously
                lock (_lock)
                {
                    // Check again inside the lock
                    if (_instance == null)
                    {
                        // 1. Try to find an instance in the scene
                        _instance = FindFirstObjectByType<T>();
                        
                        if (_instance == null)
                        {
                            // 2. If not found, try to load from Resources
                            string prefabName = typeof(T).Name;
                            GameObject prefab = Resources.Load<GameObject>($"Singletons/{prefabName}");

                            if (prefab != null)
                            {
                                // Instantiate the prefab
                                GameObject singletonObject = Instantiate(prefab);
                                // The object's Awake() will set _instance
                                // and DontDestroyOnLoad
                                singletonObject.name = $"{prefabName} (From Prefab)";
                            }
                            else
                            {
                                // 3. If no prefab, create a blank object as a fallback
                                Debug.LogWarning($"[PersistentSingleton] No prefab found at 'Resources/Singletons/{prefabName}'. Creating blank instance.");
                                GameObject singletonObject = new GameObject();
                                _instance = singletonObject.AddComponent<T>();
                                // Name the object for easy identification
                                singletonObject.name = $"{typeof(T).Name} (Auto-Created, BLANK)";
                                // The Awake() method will be called on this new instance
                                // and will handle the DontDestroyOnLoad.
                            }
                        }
                    }
                }
            }
            
            return _instance;
        }
    }

    /// <summary>
    /// This is the core singleton logic. It runs on object creation.
    /// Its job is to ensure this is the only instance and to make it persistent.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            // This is the first instance (either from scene, prefab, or auto-created)
            // Set the static instance
            _instance = this as T;
            
            // Mark this GameObject to not be destroyed when loading new scenes
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            // This is a duplicate. We already have an instance.
            // Destroy this new, duplicate GameObject.
            Debug.LogWarning($"[PersistentSingleton] Duplicate {typeof(T).Name} found in scene. Destroying.", this.gameObject);
            Destroy(this.gameObject);
        }
    }
}