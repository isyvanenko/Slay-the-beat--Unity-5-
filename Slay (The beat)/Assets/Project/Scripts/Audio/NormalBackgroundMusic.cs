using UnityEngine;

/// <summary>
/// Configures the background music for normal scenes with standard audio processing.
/// </summary>
/// <remarks>
/// This initialization script sets up the music manager for scenes that don't require
/// special audio effects (like club-style processing). It plays standard menu music
/// and applies the normal audio mixer snapshot for clean, unmodified playback.
/// 
/// Use this component on any GameObject in main menus, settings screens, or any scenes
/// where you want standard, neutral audio processing without additional effects.
/// </remarks>
public class NormalBackgroundMusic : MonoBehaviour
{
    /// <summary>
    /// Initializes the normal music settings when the scene starts.
    /// </summary>
    /// <remarks>
    /// This method performs three configuration steps:
    /// 1. Starts playing the standard menu music
    /// 2. Transitions the audio mixer to the normal snapshot (default transition time of 1 second)
    /// 3. Ensures full music volume (quiet mode disabled)
    /// 
    /// Unlike ClubBackgroundMusic which uses a 2-second transition to club effects,
    /// this uses the default 1-second transition time defined in the MusicManager.SetNormal() method.
    /// This creates a smooth but relatively quick transition to normal audio processing.
    /// </remarks>
    void Start()
    {
        MusicManager.Instance.PlayMenu();
        MusicManager.Instance.SetNormal();
        MusicManager.Instance.SetQuiet(false);
    }
}