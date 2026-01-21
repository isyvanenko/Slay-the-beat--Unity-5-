using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class PlayerCountMenu : MonoBehaviour
{
    [Header("Audio Setup")]
    // Drag an EXTERNAL AudioSource here (e.g., from the Main Camera)
    public AudioSource globalAudioSource; 
    public AudioClip onePlayerSound;
    public AudioClip twoPlayerSound;

    [Header("Next Menu")]
    public GameObject deviceSelectMenuP1;

    [Header("Visual Settings")]
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void HandleSelection(int index)
    {
        if (isTransitioning) return;

        // 1. Logic
        int playerCount = (index == 0) ? 1 : 2;
        SessionConfig.PlayerCount = playerCount;
        Debug.Log("Player Count Selected: " + playerCount);

        // 2. Play Audio (Using the external source so it doesn't cut off)
        AudioClip clipToPlay = (playerCount == 1) ? onePlayerSound : twoPlayerSound;
        
        if (globalAudioSource != null && clipToPlay != null)
        {
            globalAudioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            Debug.LogWarning("AudioSource or Clip is missing in the Inspector!");
        }

        // 3. Start Fade
        StartCoroutine(TransitionSequence());
    }

    IEnumerator TransitionSequence()
    {
        isTransitioning = true;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (deviceSelectMenuP1 != null)
            deviceSelectMenuP1.SetActive(true);

        gameObject.SetActive(false);
    }
}