using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Menu for selecting single-player or two-player mode with audio feedback and smooth transitions.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PlayerCountMenu : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource globalAudioSource; 
    public AudioClip onePlayerSound;
    public AudioClip twoPlayerSound;

    [Header("Audio Ducking (Smooth Fades)")]
    public float duckedMusicVolume = 0.3f;   
    public float duckDropSpeed = 0.3f;       
    public float duckRestoreSpeed = 1.0f;    

    [Header("Next Menu")]
    public GameObject deviceAssignmentMenuP1;

    [Header("Visual Settings")]
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        isTransitioning = false;
        if (canvasGroup != null) 
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void HandleSelection(int index)
    {
        if (isTransitioning) return;

        // Reset device configurations for fresh session
        SessionConfig.Player1Device = null;
        SessionConfig.Player2Device = null;
        SessionConfig.CurrentStage = 1; 
        SessionConfig.P1Bindings = null;
        SessionConfig.P2Bindings = null;

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

        if (deviceAssignmentMenuP1 != null)
        {
            var deviceAssign = deviceAssignmentMenuP1.GetComponent<DeviceAssignmentMenu>();
            if (deviceAssign != null)
            {
                deviceAssign.currentPlayerIndex = 0;
                //deviceAssign.ResetMenu();
            }
            deviceAssignmentMenuP1.SetActive(true);
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