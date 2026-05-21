/// <summary>
/// Static data container for passing gameplay results between scenes and managing session state.
/// </summary>
/// <remarks>
/// This class serves as a persistent data bridge between gameplay scenes (like FreestyleManager, GameplayManager)
/// and the ResultsScene. Unlike PlayerPrefs or file-based saves, this keeps data in memory during the game session.
/// 
/// Key Features:
/// - Stores final scores and max combos for both players after level completion
/// - Tracks multiplayer configuration (single vs two-player)
/// - Maintains song metadata for results display
/// - Supports multi-round tournament/competition structures
/// 
/// Important Notes:
/// - All fields are static, so data persists across scene loads without needing DontDestroyOnLoad
/// - Data is NOT saved between game sessions (closing the game resets all values)
/// - Values should be set before loading the ResultsScene and read immediately after
/// - Not thread-safe (intended for single-threaded Unity main thread usage only)
/// </remarks>
public static class GameSessionData
{
    /// <summary>
    /// Player 1's final score achieved during the gameplay session.
    /// </summary>
    /// <remarks>
    /// Set by GameplayManager or FreestyleManager when EndLevel() is called.
    /// Displayed on the results screen and used for grade calculation.
    /// Default value: 0
    /// </remarks>
    public static int P1Score = 0;
    
    /// <summary>
    /// Player 2's final score achieved during the gameplay session.
    /// </summary>
    /// <remarks>
    /// Only relevant when IsTwoPlayer is true. Set during EndLevel() call.
    /// Used for multiplayer results comparison and winner determination.
    /// Default value: 0
    /// </remarks>
    public static int P2Score = 0;
    
    /// <summary>
    /// Player 1's maximum combo streak achieved during the session.
    /// </summary>
    /// <remarks>
    /// Tracks the highest consecutive successful hits without missing.
    /// Used for:
    /// - Combo-based achievement rewards
    /// - Skill rating calculations
    /// - Display on results screen as a performance metric
    /// 
    /// Updated continuously during gameplay and finalized in EndLevel().
    /// Default value: 0
    /// </remarks>
    public static int P1MaxCombo = 0;
    
    /// <summary>
    /// Player 2's maximum combo streak achieved during the session.
    /// </summary>
    /// <remarks>
    /// Only applicable in two-player mode. Tracks Player 2's highest consecutive hits.
    /// Used for comparative performance metrics on results screen.
    /// Default value: 0
    /// </remarks>
    public static int P2MaxCombo = 0;
    
    /// <summary>
    /// Flag indicating whether the session is in two-player mode.
    /// </summary>
    /// <remarks>
    /// Determines UI layout and score display on results screen:
    /// - true: Show both players' scores, combos, and determine winner
    /// - false: Show only Player 1's data in single-player layout
    /// 
    /// Set by GameplayManager during EndLevel() based on SessionConfig.PlayerCount.
    /// Default value: false
    /// </remarks>
    public static bool IsTwoPlayer = false;
    
    /// <summary>
    /// Name of the song that was played during the session.
    /// </summary>
    /// <remarks>
    /// Used for display on results screen and for session tracking.
    /// Should be set when loading the song, typically from SongGradeData.title.
    /// Default value: "Unknown Song" (fallback if not set)
    /// </remarks>
    public static string SongName = "Unknown Song";
    
    /// <summary>
    /// Reference to the grade data for the currently played song.
    /// </summary>
    /// <remarks>
    /// Contains song metadata, difficulty levels, and grade thresholds.
    /// Used by results screen to:
    /// - Display song information (title, artist, difficulty)
    /// - Calculate final letter grade based on player score
    /// - Determine score thresholds for achievements
    /// 
    /// Set during GameplayManager.Start() from GameDataBridge.SelectedSong.
    /// Should never be null when reaching results screen.
    /// </remarks>
    public static SongGradeData CurrentSongGrades;
    
    /// <summary>
    /// Current round number in a multi-round competition or setlist.
    /// </summary>
    /// <remarks>
    /// Useful for:
    /// - Tournament-style gameplay with multiple songs
    /// - Setlist progression (e.g., 2-out-of-3 matches)
    /// - Cumulative score tracking across rounds
    /// 
    /// Starting value: 1 (first round)
    /// Increment after each completed round.
    /// Default value: 1
    /// </remarks>
    public static int CurrentRound = 1;
    
    /// <summary>
    /// Total number of rounds in the current competition or setlist.
    /// </summary>
    /// <remarks>
    /// Defines the maximum rounds before declaring overall winner.
    /// Common configurations:
    /// - Single song: TotalRounds = 1
    /// - Best of 3: TotalRounds = 2 (first to 2 wins)
    /// - Best of 5: TotalRounds = 3 (first to 3 wins)
    /// - Full setlist: TotalRounds = number of songs
    /// 
    /// Used with CurrentRound to determine if the session should continue
    /// or display final results.
    /// Default value: 2
    /// </remarks>
    public static int TotalRounds = 2;
}