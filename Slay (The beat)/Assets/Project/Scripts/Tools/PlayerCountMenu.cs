using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Menu for selecting single-player or two-player mode with audio feedback and smooth transitions.
/// </summary>
/// <remarks>
/// This component presents a choice between 1-player and 2-player modes. When a selection is made:
/// - Resets device configuration to ensure clean state (clears previous controller assignments)
/// - Updates SessionConfig with the chosen player count
/// - Plays appropriate audio feedback (different sound for each mode)
/// - Smoothly fades out the menu while ducking background music
/// - Activates the next menu (device selection for Player 1)
/// - Restores original music volume after transition
/// 
/// The reset logic explicitly clears old controller devices to prevent carryover from
/// previous sessions, ensuring a fresh start for each play session.
/// </remarks>
[RequireComponent(typeof(CanvasGroup))]
public class PlayerCountMenu : MonoBehaviour
{
    [Header("Audio Setup")]
    /// <summary>Audio source for playing selection sound effects.</summary>
    public AudioSource globalAudioSource; 
    
    /// <summary>Sound effect played when 1-player mode is selected.</summary>
    public AudioClip onePlayerSound;
    
    /// <summary>Sound effect played when 2-player mode is selected.</summary>
    public AudioClip twoPlayerSound;

    [Header("Audio Ducking (Smooth Fades)")]
    /// <summary>Target volume for background music when ducked during transition.</summary>
    public float duckedMusicVolume = 0.3f;   
    
    /// <summary>Speed at which music volume drops to ducked level (seconds).</summary>
    public float duckDropSpeed = 0.3f;       
    
    /// <summary>Speed at which music volume returns to original (seconds).</summary>
    public float duckRestoreSpeed = 1.0f;    

    [Header("Next Menu")]
    /// <summary>GameObject reference to the Player 1 device selection menu.</summary>
    public GameObject deviceSelectMenuP1;

    [Header("Visual Settings")]
    /// <summary>Duration of the fade-out transition in seconds.</summary>
    public float fadeDuration = 0.5f;

    /// <summary>CanvasGroup component for fade effects and interaction blocking.</summary>
    private CanvasGroup canvasGroup;
    
    /// <summary>Flag to prevent multiple simultaneous transitions.</summary>
    private bool isTransitioning = false;

    /// <summary>
    /// Initializes component and caches CanvasGroup reference.
    /// </summary>
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Resets menu state when enabled (alpha, interactivity, transition flag).
    /// </summary>
    /// <remarks>
    /// Clears the transitioning flag and ensures the menu is fully visible and interactive
    /// each time it becomes active. Important for returning from nested menus.
    /// </remarks>
    void OnEnable()
    {
        // WE REMOVED THE RESET CODE FROM HERE
        isTransitioning = false;
        if (canvasGroup != null) 
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Handles player count selection from UI buttons.
    /// </summary>
    /// <param name="index">0 = 1-player, 1 = 2-player.</param>
    /// <remarks>
    /// This method:
    /// 1. Prevents execution if a transition is already in progress
    /// 2. Resets SessionConfig device assignments and stage counter (critical for clean state)
    /// 3. Sets the player count in SessionConfig
    /// 4. Plays the appropriate audio clip
    /// 5. Initiates the transition coroutine
    /// 
    /// The reset step ensures that leftover device references from previous sessions
    /// don't interfere with new gameplay.
    /// </remarks>
    public void HandleSelection(int index)
    {
        if (isTransitioning) return;

        // --- THE BULLETPROOF RESET ---
        // We only clear the old controllers when they explicitly click a button to start a new game!
        SessionConfig.Player1Device = null;
        SessionConfig.Player2Device = null;
        SessionConfig.CurrentStage = 1; 
        // ------------------------------

        int playerCount = (index == 0) ? 1 : 2;
        SessionConfig.PlayerCount = playerCount;
        Debug.Log("Player Count Selected: " + playerCount);

        AudioClip clipToPlay = (playerCount == 1) ? onePlayerSound : twoPlayerSound;
        
        if (globalAudioSource != null && clipToPlay != null)
        {
            globalAudioSource.PlayOneShot(clipToPlay);
        }

        StartCoroutine(TransitionSequence(clipToPlay));
    }

    /// <summary>
    /// Coroutine that handles the visual and audio transition to the next menu.
    /// </summary>
    /// <param name="playedClip">Audio clip that was played (used to time the transition).</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Transition steps:
    /// 1. Get reference to MusicManager's AudioSource for volume ducking
    /// 2. Fade background music to ducked volume (if MusicManager exists)
    /// 3. Fade out this menu's CanvasGroup over fadeDuration
    /// 4. Deactivate this menu's interaction
    /// 5. Activate the device selection menu (Player 1)
    /// 6. Wait for the played audio clip to finish (minus fade duration)
    /// 7. Fade background music back to original volume
    /// 8. Disable this GameObject
    /// 
    /// Using unscaledDeltaTime ensures transitions work correctly even if Time.timeScale
    /// is modified elsewhere (e.g., pause menus).
    /// </remarks>
    IEnumerator TransitionSequence(AudioClip playedClip)
    {
        isTransitioning = true;
        
        AudioSource musicSource = null;
        float originalMusicVolume = 1f;

        if (MusicManager.Instance != null) 
        {
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
            if (musicSource != null)
            {
                originalMusicVolume = musicSource.volume;
            }
        }

        if (musicSource != null)
        {
            StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, duckedMusicVolume, duckDropSpeed));
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (deviceSelectMenuP1 != null)
        {
            deviceSelectMenuP1.SetActive(true);
        }

        if (playedClip != null)
        {
            float remainingWait = playedClip.length - fadeDuration;
            if (remainingWait > 0) 
            {
                yield return new WaitForSecondsRealtime(remainingWait);
            }
        }

        if (musicSource != null)
        {
            yield return StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, originalMusicVolume, duckRestoreSpeed));
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Coroutine that smoothly fades the volume of an AudioSource over time.
    /// </summary>
    /// <param name="source">AudioSource to fade.</param>
    /// <param name="startVol">Starting volume.</param>
    /// <param name="endVol">Target volume.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Uses unscaledDeltaTime to ensure fading works correctly even when Time.timeScale is altered.
    /// </remarks>
    IEnumerator FadeMusicVolume(AudioSource source, float startVol, float endVol, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, t / duration);
            yield return null;
        }
        source.volume = endVol; 
    }
}