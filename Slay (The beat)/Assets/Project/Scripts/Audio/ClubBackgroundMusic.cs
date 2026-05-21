using UnityEngine;

/// <summary>
/// Configures the background music for club/party scenes with club-style audio processing.
/// </summary>
/// <remarks>
/// This simple initialization script sets up the music manager when the club scene loads.
/// It plays menu-appropriate music (which could be upbeat or club-style tracks) and applies
/// the club audio mixer snapshot for enhanced bass, reverb, or other club effects.
/// 
/// Use this component on any GameObject in club-themed scenes, menus, or party areas
/// where you want a more energetic audio atmosphere.
/// </remarks>
public class ClubBackgroundMusic : MonoBehaviour
{
    /// <summary>
    /// Initializes the club music settings when the scene starts.
    /// </summary>
    /// <remarks>
    /// This method performs three configuration steps:
    /// 1. Starts playing the menu music category (suitable for club environments)
    /// 2. Transitions the audio mixer to the club snapshot over 2 seconds
    /// 3. Ensures full music volume (quiet mode disabled)
    /// 
    /// The 2-second transition creates a smooth audio effect as the club
    /// processing fades in rather than abruptly changing.
    /// </remarks>
    void Start()
    {
        MusicManager.Instance.PlayMenu();
        MusicManager.Instance.SetClub(2f); // club mix takes over
        MusicManager.Instance.SetQuiet(false);
    }
}