using UnityEngine;

// 1. Inherit from PersistentSingleton<SFXManager>
public class SFXManager : PersistentSingleton<SFXManager>
{
    // 2. The static 'instance' is no longer needed.
    // The base class provides 'Instance'.

    // The component that will play the sound
    private AudioSource sfxSource;

    // The sound clip you want to play
    [Tooltip("Drag your transition 'whoosh' sound clip here")]
    public AudioClip transitionSFX;

    // 3. The base class 'PersistentSingleton' handles
    //    all the Awake() logic (singleton, DontDestroyOnLoad).
    //    We move this script's setup logic to Start().
    void Start()
    {
        // Start() will only run on the *one* persistent instance,
        // *after* Awake() has destroyed any duplicates.

        // Get or add the AudioSource component
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // We don't want SFX to loop or play on awake
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Call this from any other script to play the transition sound.
    /// </summary>
    public void PlayTransitionSound()
    {
        if (transitionSFX != null)
        {
            // PlayOneShot is perfect for this. It's non-interrupting
            // and plays the clip once.
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(transitionSFX);
            }
            else
            {
                Debug.LogWarning("SFXManager: sfxSource is null. Was Start() called?");
            }
        }
        else
        {
            Debug.LogWarning("SFXManager: Missing transitionSFX clip!");
        }
    }
}