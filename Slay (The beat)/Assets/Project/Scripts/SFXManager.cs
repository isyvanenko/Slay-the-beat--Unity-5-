using UnityEngine;

public class SFXManager : MonoBehaviour
{
    // Static instance for easy access from other scripts
    public static SFXManager instance;

    // The component that will play the sound
    private AudioSource sfxSource;

    // The sound clip you want to play
    [Tooltip("Drag your transition 'whoosh' sound clip here")]
    public AudioClip transitionSFX;

    void Awake()
    {
        // --- Singleton Pattern ---
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // -------------------------

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
            sfxSource.PlayOneShot(transitionSFX);
        }
        else
        {
            Debug.LogWarning("SFXManager: Missing transitionSFX clip!");
        }
    }
}