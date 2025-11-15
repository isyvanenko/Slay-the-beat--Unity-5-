using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class TransitionManager : MonoBehaviour
{
   
    public static TransitionManager Instance;

    [Header("Animator handling fade in/out")]
    public Animator fadeAnimator;

    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

       public void LoadScene(string sceneName, float fadeTime = 0.5f)
    {
        if (!isTransitioning)
            StartCoroutine(LoadRoutine(sceneName, fadeTime));
    }

    private IEnumerator LoadRoutine(string sceneName, float fadeTime)
    {
        isTransitioning = true;

        // Play your original fade out animation
        fadeAnimator.SetTrigger("EndScene");

        // optional sound
        SFXManager.instance?.PlayTransitionSound();

        // wait until fade-out finishes
        yield return new WaitForSeconds(fadeTime);

        // load next scene
        SceneManager.LoadScene(sceneName);

        // animator will fade in automatically because your transitions handle it
        isTransitioning = false;
    }
}