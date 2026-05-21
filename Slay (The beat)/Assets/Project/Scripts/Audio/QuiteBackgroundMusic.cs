using UnityEngine;

/// <summary>
/// Configures background music in quiet mode for scenes requiring reduced audio presence.
/// </summary>
/// <remarks>
/// This initialization script sets up the music manager with quiet mode enabled, which
/// significantly reduces music volume. This is useful for scenes where background music
/// should be subtle or barely audible, such as:
/// - Dialogue-heavy scenes
/// - Cinematic sequences with voiceovers
/// - Pause menus or settings screens
/// - Tutorial scenes where instructional audio is primary
/// 
/// The quiet mode reduces volume to 0 (or a very low level as configured in MusicManager.SetQuiet()),
/// allowing other audio elements to take priority without completely stopping the music.
/// </remarks>
public class QuiteBackgroundMusic : MonoBehaviour
{
    /// <summary>
    /// Initializes quiet background music settings when the scene starts.
    /// </summary>
    /// <remarks>
    /// This method performs two configuration steps:
    /// 1. Starts playing the menu music (at the quiet volume level)
    /// 2. Enables quiet mode to reduce music volume
    /// 
    /// Note: The SetNormal() or SetClub() mixer snapshots are not called here,
    /// so the previous mixer settings persist. This is intentional, as quiet mode
    /// primarily affects volume while maintaining whatever audio processing
    /// (normal, club, etc.) was already active.
    /// </remarks>
    void Start()
    {
        MusicManager.Instance.PlayMenu();
        MusicManager.Instance.SetQuiet(true);
    }
}