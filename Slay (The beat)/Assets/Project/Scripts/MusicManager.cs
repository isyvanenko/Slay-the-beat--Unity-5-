using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// Manages music playback, crossfading, mixer snapshots, and volume control throughout the game.
/// </summary>
/// <remarks>
/// This class inherits from PersistentSingleton, ensuring only one instance exists across all scenes
/// and persists between scene loads. It handles seamless music transitions between different game states
/// (menu, gameplay, results) with configurable fade durations.
/// 
/// Key Features:
/// - Singleton pattern for global access
/// - Crossfade between music clips
/// - Audio mixer snapshot transitions
/// - Quiet mode for ducking volume during important events
/// </remarks>
public class MusicManager : PersistentSingleton<MusicManager>
{
    [Header("One AudioSource for everything")]
    /// <summary>Single AudioSource used for all music playback.</summary>
    public AudioSource musicSource;

    [Header("Music Clips")]
    /// <summary>Music played in menu screens.</summary>
    public AudioClip menuMusic;

    /// <summary>Music played during active gameplay.</summary>
    public AudioClip gameplayMusic;

    /// <summary>Music played on results/score screen.</summary>
    public AudioClip resultsMusic;

    [Header("Mixer Snapshots")]
    /// <summary>Normal audio mixer settings for regular gameplay.</summary>
    public AudioMixerSnapshot normalSnapshot;

    /// <summary>Club-style audio mixer settings with enhanced effects.</summary>
    public AudioMixerSnapshot clubSnapshot;

    /// <summary>
    /// Current target volume level for the music source.
    /// </summary>
    /// <remarks>
    /// Used to remember the desired volume level when fading or when quiet mode toggles.
    /// Normal volume = 1f, Quiet mode = 0.15f or 0f.
    /// </remarks>
    private float targetVolume = 1f;

    // -----------------------------
    //   MUSIC PLAYBACK (ONE AUDIO)
    // -----------------------------

    /// <summary>
    /// Plays the menu music with a fade-in duration of 0.75 seconds.
    /// </summary>
    public void PlayMenu() => PlayMusic(menuMusic, 0.75f);

    /// <summary>
    /// Plays the gameplay music with a fade-in duration of 0.75 seconds.
    /// </summary>
    public void PlayGameplay() => PlayMusic(gameplayMusic, 0.75f);

    /// <summary>
    /// Plays the results screen music with a fade-in duration of 0.75 seconds.
    /// </summary>
    public void PlayResults() => PlayMusic(resultsMusic, 0.75f);

    /// <summary>
    /// Transitions to a new music clip with a crossfade effect.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="fade">Fade duration in seconds for both fade-out and fade-in.</param>
    /// <remarks>
    /// If the requested clip is already playing, the method returns without interrupting playback,
    /// but ensures the volume matches the current targetVolume (useful after quiet mode changes).
    /// </remarks>
    private void PlayMusic(AudioClip clip, float fade)
    {
        if (clip == null) return;

        // If we are already playing this exact clip, just return and let it keep playing.
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            // If we're already playing, we should still make sure the volume matches
            // the target (in case SetQuiet was called)
            musicSource.volume = targetVolume;
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeToClip(clip, fade));
    }

    /// <summary>
    /// Coroutine that handles fading from the current clip to a new clip.
    /// </summary>
    /// <param name="newClip">The new AudioClip to fade into.</param>
    /// <param name="fade">Duration in seconds for each fade phase.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// The transition process:
    /// 1. Fade current volume from its starting level to 0
    /// 2. Stop current playback and switch to the new clip
    /// 3. Start playing the new clip
    /// 4. Fade volume from 0 to the current targetVolume
    /// </remarks>
    private IEnumerator FadeToClip(AudioClip newClip, float fade)
    {
        float startVol = musicSource.volume;

        // Fade Out
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fade);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade In - from 0 up to our targetVolume
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

    /// <summary>
    /// Transitions the audio mixer to the normal snapshot.
    /// </summary>
    /// <param name="time">Transition duration in seconds. Default is 1f.</param>
    public void SetNormal(float time = 1f)
    {
        if (normalSnapshot != null)
            normalSnapshot.TransitionTo(time);
    }

    /// <summary>
    /// Transitions the audio mixer to the club/effect-heavy snapshot.
    /// </summary>
    /// <param name="time">Transition duration in seconds. Default is 1f.</param>
    public void SetClub(float time = 1f)
    {
        if (clubSnapshot != null)
            clubSnapshot.TransitionTo(time);
    }

    // -----------------------------
    //      QUIET MODE (optional)
    // -----------------------------

    /// <summary>
    /// Sets quiet mode, reducing music volume for important events or voiceovers.
    /// </summary>
    /// <param name="quiet">If true, reduces volume to 0f; if false, restores to full volume (1f).</param>
    /// <remarks>
    /// This method updates the targetVolume variable which persists through fades and
    /// clip transitions, ensuring volume levels remain consistent across song changes.
    /// </remarks>
    public void SetQuiet(bool quiet)
    {
        // Instead of setting the volume directly, we set our target...
        targetVolume = quiet ? 0f : 1f;
        // ...and then apply that target to the current volume.
        musicSource.volume = targetVolume;
    }
}