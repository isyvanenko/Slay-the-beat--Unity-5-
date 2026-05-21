using UnityEngine;

/// <summary>
/// Manages sound effects playback throughout the game using a singleton pattern.
/// </summary>
/// <remarks>
/// This class inherits from PersistentSingleton, ensuring only one instance exists across all scenes
/// and persists between scene loads. It provides a centralized system for playing sound effects,
/// particularly useful for UI sounds and transitions.
/// 
/// Key Features:
/// - Singleton pattern for global access
/// - Automatic AudioSource creation if missing
/// - PlayOneShot support for overlapping sound effects
/// - Persistent across scene loads
/// </remarks>
public class SFXManager : PersistentSingleton<SFXManager>
{
    /// <summary>
    /// AudioSource component used for playing sound effects.
    /// </summary>
    /// <remarks>
    /// This component is configured with loop = false and playOnAwake = false
    /// to ensure it only plays when explicitly requested and doesn't loop.
    /// </remarks>
    private AudioSource sfxSource;

    /// <summary>
    /// Sound effect played during scene or screen transitions.
    /// </summary>
    /// <remarks>
    /// Typically a "whoosh" or quick transition sound. Assign this in the Unity editor.
    /// If not assigned, a warning will be logged when attempting to play.
    /// </remarks>
    [Tooltip("Drag your transition 'whoosh' sound clip here")]
    public AudioClip transitionSFX;

    /// <summary>
    /// Initializes the SFXManager singleton and sets up the AudioSource component.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during object initialization. It:
    /// 1. Calls base.Awake() to ensure proper singleton setup and DontDestroyOnLoad
    /// 2. Verifies this is the singleton instance (not a duplicate being destroyed)
    /// 3. Retrieves or creates an AudioSource component
    /// 4. Configures the AudioSource for sound effect playback (no looping, manual play)
    /// 
    /// The AudioSource configuration is optimized for PlayOneShot usage, allowing
    /// multiple overlapping sound effects.
    /// </remarks>
    protected override void Awake()
    {
        // 1. MUST call base.Awake() so the Singleton/DontDestroyOnLoad works!
        base.Awake();

        // 2. If this is the "Duplicate" instance being destroyed, stop here
        if (Instance != this) return;

        // 3. Setup the AudioSource immediately
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Plays the transition sound effect as a one-shot.
    /// </summary>
    /// <remarks>
    /// This method uses PlayOneShot which allows multiple sounds to overlap
    /// (useful for rapid UI interactions). If transitionSFX is not assigned,
    /// a warning is logged to the console for debugging.
    /// 
    /// The AudioSource is guaranteed to exist because it's created in Awake(),
    /// so no null check is needed for sfxSource.
    /// </remarks>
    public void PlayTransitionSound()
    {
        if (transitionSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(transitionSFX);
        }
        else if (transitionSFX == null)
        {
            Debug.LogWarning("SFXManager: Missing transitionSFX clip!");
        }
        // No more null warning for sfxSource because it's set in Awake!
    }
}