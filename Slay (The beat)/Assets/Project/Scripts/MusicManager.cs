using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

// 1. CHANGE THIS: Inherit from PersistentSingleton<MusicManager>
public class MusicManager : PersistentSingleton<MusicManager>
{
    // 2. DELETE THIS: The base class already has a public "Instance" property.
    // public static MusicManager Instance; 

    [Header("One AudioSource for everything")]
    public AudioSource musicSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip resultsMusic;

    [Header("Mixer Snapshots")]
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot clubSnapshot;

    // --- NEW VARIABLE ---
    // This will store what our "target" volume should be (1f normally, 0.15f when quiet)
    private float targetVolume = 1f;

    // 3. DELETE THIS: The PersistentSingleton base class
    //    handles all of this logic for you!
    /*
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
    */

    // -----------------------------
    //   MUSIC PLAYBACK (ONE AUDIO)
    // -----------------------------

    public void PlayMenu()     => PlayMusic(menuMusic, 0.75f);
    public void PlayGameplay() => PlayMusic(gameplayMusic, 0.75f);
    public void PlayResults()  => PlayMusic(resultsMusic, 0.75f);

    private void PlayMusic(AudioClip clip, float fade)
    {
        if (clip == null) return;

        // --- THIS IS THE FIX ---
        // If we are already playing this exact clip,
        // just return and let it keep playing.
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            // --- FIX ---
            // If we're already playing, we should still
            // make sure the volume matches the target (in case SetQuiet was called)
            musicSource.volume = targetVolume;
            return;
        }
        // --- END FIX ---

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

        // --- Fade In (MODIFIED) ---
        // We now fade from 0f up to our 'targetVolume'
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / fade);
            yield return null;
        }

        // Ensure the volume is exactly the target at the end
        musicSource.volume = targetVolume;
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
        // --- MODIFIED ---
        // Instead of setting the volume directly, we set our target...
        targetVolume = quiet ? 0f : 1f;
        // ...and then apply that target to the current volume.
        musicSource.volume = targetVolume;
    }
}