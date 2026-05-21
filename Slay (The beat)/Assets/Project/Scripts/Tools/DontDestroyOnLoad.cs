using UnityEngine;

/// <summary>
/// Keeps the attached GameObject alive when Unity loads a new scene.
/// </summary>
public class DontDestroyOnLoad : MonoBehaviour
{
    /// <summary>
    /// Marks this GameObject so Unity does not destroy it during scene changes.
    /// </summary>
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
