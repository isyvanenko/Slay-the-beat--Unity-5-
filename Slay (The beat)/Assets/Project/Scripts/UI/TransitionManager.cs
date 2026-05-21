using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages scene transitions with a fade effect and optional sound.
/// </summary>
/// <remarks>
/// This singleton component coordinates scene loading with a fade‑in/out animation.
/// It uses an Animator with a trigger "EndScene" to play the fade‑out, waits for the
/// specified duration, then loads the target scene. The fade‑in is assumed to be handled
/// automatically by the Animator on the new scene (or the same persistent GameObject).
/// 
/// The class also fires a static event <see cref="OnSceneLoadStarted"/> before the
/// transition begins. Any UI elements that need to hide or reset can subscribe to this event.
/// 
/// Inherits from <see cref="PersistentSingleton{T}"/> to ensure only one instance exists
/// across scenes and that it persists through scene loads.
/// </remarks>
public class TransitionManager : PersistentSingleton<TransitionManager>
{
    [Header("Animator handling fade in/out")]
    /// <summary>Animator that controls the fade‑in and fade‑out animations.</summary>
    /// <remarks>
    /// The animator must have a trigger parameter named "EndScene". The fade‑out duration
    /// should match the fadeTime passed to <see cref="LoadScene"/>.
    /// </remarks>
    public Animator fadeAnimator;

    /// <summary>Flag to prevent multiple concurrent transitions.</summary>
    private bool isTransitioning = false;

    /// <summary>
    /// Event raised just before the scene transition starts (after fade‑out trigger but before load).
    /// </summary>
    /// <remarks>
    /// Subscribers (e.g., UI panels, music managers) can use this event to perform cleanup,
    /// hide elements, or pause audio before a scene change.
    /// </remarks>
    public static event System.Action OnSceneLoadStarted;

    /// <summary>
    /// Loads a new scene with a fade transition.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load (must be in Build Settings).</param>
    /// <param name="fadeTime">Duration (in seconds) of the fade‑out animation. Default 0.5s.</param>
    /// <remarks>
    /// The method does nothing if a transition is already in progress.
    /// Steps performed:
    /// 1. Invoke <see cref="OnSceneLoadStarted"/> event.
    /// 2. Play the fade‑out animation trigger "EndScene" on the fadeAnimator.
    /// 3. Play a transition sound using <see cref="SFXManager"/> if available.
    /// 4. Wait for <paramref name="fadeTime"/> seconds.
    /// 5. Load the new scene via <see cref="SceneManager.LoadScene"/>.
    /// </remarks>
    public void LoadScene(string sceneName, float fadeTime = 0.5f)
    {
        if (!isTransitioning)
        {
            // Trigger the fade event BEFORE transition starts
            OnSceneLoadStarted?.Invoke();

            StartCoroutine(LoadRoutine(sceneName, fadeTime));
        }
    }

    /// <summary>
    /// Coroutine that performs the actual transition sequence.
    /// </summary>
    /// <param name="sceneName">Target scene name.</param>
    /// <param name="fadeTime">Fade‑out duration.</param>
    /// <returns>IEnumerator for the sequence.</returns>
    private IEnumerator LoadRoutine(string sceneName, float fadeTime)
    {
        isTransitioning = true;

        // Play fade-out animation
        fadeAnimator.SetTrigger("EndScene");

        // Play SFX if available
        SFXManager.Instance?.PlayTransitionSound();

        // Wait for fade-out
        yield return new WaitForSeconds(fadeTime);

        // Load next scene
        SceneManager.LoadScene(sceneName);

        // Fade-in occurs automatically from your animator
        isTransitioning = false;
    }
}