/// <summary>
/// Static bridge class for passing game data between different scenes and components.
/// </summary>
/// <remarks>
/// This class serves as a global data container for storing temporary game state information,
/// particularly for song selection and difficulty settings. Unlike MonoBehaviour-based solutions,
/// this static class persists across scene loads without requiring a GameObject.
/// 
/// Data stored here should be set before scene transitions and retrieved after loading.
/// Values are not automatically cleared and will persist until explicitly changed.
/// </remarks>
public static class GameDataBridge
{
    /// <summary>
    /// The song data for the currently selected song.
    /// </summary>
    /// <remarks>
    /// Contains all relevant information about the selected song including title,
    /// artist, audio clips, and chart data. Set this before transitioning to
    /// the gameplay scene.
    /// </remarks>
    public static SongGradeData SelectedSong; // The song selected

    /// <summary>
    /// The difficulty level for the selected song.
    /// </summary>
    /// <remarks>
    /// Difficulty mapping:
    /// - 0 = Easy mode - Basic patterns and slower tempo
    /// - 1 = Medium mode - Moderate patterns and tempo
    /// - 2 = Hard mode - Complex patterns and faster tempo
    /// 
    /// Default value is 0 (Easy) if not explicitly set.
    /// </remarks>
    public static int SelectedDifficulty;    // 0 = Easy, 1 = Med, 2 = Hard
}