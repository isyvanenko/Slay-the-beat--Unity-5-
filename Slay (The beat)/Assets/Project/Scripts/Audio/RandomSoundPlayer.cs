using UnityEngine;

/// <summary>
/// Plays random sound effects from a collection, typically triggered by animation events.
/// </summary>
/// <remarks>
/// This component is designed to work with Unity's animation event system, allowing
/// animators to trigger random sound effects at specific keyframes. It automatically
/// manages AudioSource setup and provides both immediate and delayed playback options.
/// 
/// Common use cases:
/// - Footstep sounds with random variations
/// - Impact or hit effects with different sound variants
/// - UI sounds with slight variations to avoid repetition
/// - Character vocalizations or grunts
/// - Weapon sounds with random pitch or sample variation
/// 
/// The component will create an AudioSource automatically if none is assigned or present,
/// configured with playOnAwake = false to prevent unwanted initialization sounds.
/// </remarks>
public class RandomSoundPlayer : MonoBehaviour
{
    [Header("Sound Settings")]

    /// <summary>
    /// AudioSource component used for sound playback.
    /// </summary>
    /// <remarks>
    /// If not assigned in the inspector, the component will attempt to find or create one.
    /// Using PlayOneShot allows multiple sounds to overlap naturally.
    /// </remarks>
    [SerializeField] private AudioSource audioSource;

    /// <summary>
    /// Array of sound clips to randomly select from when playback is triggered.
    /// </summary>
    /// <remarks>
    /// Each time PlayRandomSound() is called, a random clip from this array is selected.
    /// Add multiple variations of the same sound (e.g., footstep1, footstep2, footstep3)
    /// to create natural variety and reduce audible repetition.
    /// </remarks>
    [SerializeField] private AudioClip[] randomSounds;

    /// <summary>
    /// Volume level for played sounds, ranging from 0 (silent) to 1 (full volume).
    /// </summary>
    /// <remarks>
    /// This volume is applied to PlayOneShot calls and can be adjusted per instance
    /// to give different GameObjects different volume levels for the same sound clips.
    /// </remarks>
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    /// <summary>
    /// Initializes the AudioSource component for sound playback.
    /// </summary>
    /// <remarks>
    /// Called automatically before the first frame. This method ensures an AudioSource
    /// is available by:
    /// 1. Using the assigned AudioSource from the inspector if provided
    /// 2. Attempting to get an existing AudioSource component on the GameObject
    /// 3. Creating a new AudioSource as a fallback
    /// 
    /// The AudioSource is configured with playOnAwake = false to prevent
    /// unintended sounds when the GameObject is instantiated.
    /// </remarks>
    private void Start()
    {
        // Try to get AudioSource if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Create AudioSource if none exists
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Plays a randomly selected sound from the randomSounds array.
    /// </summary>
    /// <remarks>
    /// This method is designed to be called from Animation Events. It will:
    /// 1. Validate that the randomSounds array contains clips
    /// 2. Select a random index from the array
    /// 3. Play the selected clip using PlayOneShot (allows overlapping sounds)
    /// 
    /// If no sounds are assigned or the array is empty, a warning is logged.
    /// 
    /// Usage in Animation Events:
    /// - Select the GameObject with this component
    /// - In the Animation window, add an event at the desired keyframe
    /// - Set the Function to "PlayRandomSound"
    /// </remarks>
    public void PlayRandomSound()
    {
        if (randomSounds == null || randomSounds.Length == 0)
        {
            Debug.LogWarning("No random sounds assigned to " + gameObject.name);
            return;
        }

        // Select a random sound from the array
        int randomIndex = Random.Range(0, randomSounds.Length);
        AudioClip clipToPlay = randomSounds[randomIndex];

        // Play the sound
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }

    /// <summary>
    /// Plays a random sound after a specified delay.
    /// </summary>
    /// <param name="delay">Delay in seconds before playing the sound.</param>
    /// <remarks>
    /// This overload is useful for synchronizing sounds with animation events
    /// that require timing offsets or for delayed effects like:
    /// - Echoes or reverb effects
    /// - Delayed impacts (e.g., projectile hitting after travel time)
    /// - Multi-stage animation sounds
    /// 
    /// Uses Unity's Invoke method which is based on real-time, not game time.
    /// For more precise timing needs, consider using coroutines instead.
    /// </remarks>
    public void PlayRandomSoundWithDelay(float delay)
    {
        Invoke(nameof(PlayRandomSound), delay);
    }
}