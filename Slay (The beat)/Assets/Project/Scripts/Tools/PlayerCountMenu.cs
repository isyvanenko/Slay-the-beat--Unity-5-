using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem.Users;

[RequireComponent(typeof(CanvasGroup))]
public class PlayerCountMenu : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource globalAudioSource; 
    public AudioClip onePlayerSound;
    public AudioClip twoPlayerSound;

    [Header("Audio Ducking (Smooth Fades)")]
    public float duckedMusicVolume = 0.1f;   // How quiet the music gets
    public float duckDropSpeed = 0.3f;       // Time it takes to lower the volume
    public float duckRestoreSpeed = 1.0f;    // Time it takes to bring volume back to normal

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

    void OnEnable()
    {
        // --- ARCADE RESET LOGIC ---
        SessionConfig.Player1Device = null;
        SessionConfig.Player2Device = null;
        SessionConfig.CurrentStage = 1; 

        var allUsers = InputUser.all;
        for (int i = 0; i < allUsers.Count; i++)
        {
            allUsers[i].UnpairDevices();
        }
        // --------------------------

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

        // 1. Logic
        int playerCount = (index == 0) ? 1 : 2;
        SessionConfig.PlayerCount = playerCount;
        Debug.Log("Player Count Selected: " + playerCount);

        // 2. Play Audio 
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
        StartCoroutine(TransitionSequence(clipToPlay));
    }

    IEnumerator TransitionSequence(AudioClip playedClip)
    {
        isTransitioning = true;
        
        // Grab the MusicManager's AudioSource directly so we can manipulate it smoothly
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

        // --- 1. SMOOTHLY DUCK MUSIC VOLUME ---
        if (musicSource != null)
        {
            StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, duckedMusicVolume, duckDropSpeed));
        }

        // --- 2. FADE UI OUT ---
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

        // --- 3. TURN ON NEXT MENU ---
        if (deviceSelectMenuP1 != null)
        {
            deviceSelectMenuP1.SetActive(true);
        }

        // --- 4. WAIT FOR AUDIO CLIP TO FINISH ---
        if (playedClip != null)
        {
            float remainingWait = playedClip.length - fadeDuration;
            if (remainingWait > 0) 
            {
                yield return new WaitForSecondsRealtime(remainingWait);
            }
        }

        // --- 5. SMOOTHLY RESTORE MUSIC VOLUME ---
        if (musicSource != null)
        {
            // We YIELD this coroutine so the script waits for the fade to finish before shutting off!
            yield return StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, originalMusicVolume, duckRestoreSpeed));
        }

        // --- 6. TURN OFF GAMEOBJECT ---
        gameObject.SetActive(false);
    }

    // --- CUSTOM SMOOTH FADER ---
    IEnumerator FadeMusicVolume(AudioSource source, float startVol, float endVol, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, t / duration);
            yield return null;
        }
        source.volume = endVol; // Ensure it snaps exactly to the target value at the end
    }
}