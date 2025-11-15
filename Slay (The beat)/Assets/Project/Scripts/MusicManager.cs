using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("One AudioSource for everything")]
    public AudioSource musicSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip resultsMusic;

    [Header("Mixer Snapshots")]
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot clubSnapshot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // -----------------------------
    //   MUSIC PLAYBACK (ONE AUDIO)
    // -----------------------------

    public void PlayMenu()     => PlayMusic(menuMusic, 0.75f);
    public void PlayGameplay() => PlayMusic(gameplayMusic, 0.75f);
    public void PlayResults()  => PlayMusic(resultsMusic, 0.75f);

    private void PlayMusic(AudioClip clip, float fade)
    {
        if (clip == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeToClip(clip, fade));
    }

    private IEnumerator FadeToClip(AudioClip newClip, float fade)
    {
        float startVol = musicSource.volume;

        // --- Fade Out ---
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fade);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // --- Fade In ---
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, 1f, t / fade);
            yield return null;
        }
    }

    // -----------------------------
    //      MIXER CONTROL
    // -----------------------------

    public void SetNormal(float time = 1f)
    {
        if (normalSnapshot != null)
            normalSnapshot.TransitionTo(time);
    }

    public void SetClub(float time = 1f)
    {
        if (clubSnapshot != null)
            clubSnapshot.TransitionTo(time);
    }

    // -----------------------------
    //      QUIET MODE (optional)
    // -----------------------------
    public void SetQuiet(bool quiet)
    {
        musicSource.volume = quiet ? 0.15f : 1f;
    }
}