using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : PersistentSingleton<TransitionManager>
{
    [Header("Animator handling fade in/out")]
    public Animator fadeAnimator;

    private bool isTransitioning = false;

    // ⭐ NEW EVENT: UI elements subscribe to this
    public static event System.Action OnSceneLoadStarted;

    public void LoadScene(string sceneName, float fadeTime = 0.5f)
    {
        if (!isTransitioning)
        {
            // ⭐ Trigger the fade event BEFORE transition starts
            OnSceneLoadStarted?.Invoke();

            StartCoroutine(LoadRoutine(sceneName, fadeTime));
        }
    }

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