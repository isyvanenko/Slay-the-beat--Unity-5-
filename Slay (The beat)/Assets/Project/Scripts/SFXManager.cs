using UnityEngine;

public class SFXManager : PersistentSingleton<SFXManager>
{
    private AudioSource sfxSource;

    [Tooltip("Drag your transition 'whoosh' sound clip here")]
    public AudioClip transitionSFX;

    // Change 'void Start()' to 'protected override void Awake()'
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