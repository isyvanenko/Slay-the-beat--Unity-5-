using UnityEngine;

/// <summary>
/// ScriptableObject container for all song-specific data including charts, metadata, and grade thresholds.
/// </summary>
/// <remarks>
/// This asset serves as the complete data package for a single song in the rhythm game.
/// It stores everything needed to represent a song across different game systems:
/// 
/// - UI Display: Song name, jacket art, character sprites, environment visuals
/// - Difficulty Management: Separate charts for Easy, Medium, and Hard modes
/// - Scoring: Star thresholds for grade calculation (1 to 5 stars)
/// - Audio: Song preview clip for selection screen
/// - Visual Effects: Custom gradients for beat pulse effects
/// 
/// Create instances via: Right-click → Create → RhythmGame → SongGradeData
/// 
/// The isRandomOption flag allows special handling for a "Random Song" picker item,
/// which shouldn't represent an actual playable song but rather triggers random selection.
/// </remarks>
[CreateAssetMenu(fileName = "NewSongGrade", menuName = "RhythmGame/SongGradeData")]
public class SongGradeData : ScriptableObject
{
    // --- NEW: Toggle this ON for your "Random" item ---
    [Header("Special Feature")]
    /// <summary>
    /// Flag indicating this is a placeholder for "Random Song" selection rather than a real song.
    /// </summary>
    /// <remarks>
    /// When true, this asset represents the random option in song selection UI.
    /// It should not be playable directly; instead, selecting it triggers random
    /// selection from available non-random songs.
    /// </remarks>
    public bool isRandomOption = false; 

    [Header("Song Info")]
    /// <summary>Display name of the song shown in UI.</summary>
    public string songName;
    
    /// <summary>Sprite used as the song's jacket/album art in selection menus.</summary>
    public Sprite songJacketSprite;
    
    /// <summary>Name of the scene to load when this song is played.</summary>
    /// <remarks>Typically "GameplayScene" or a scene configured for specific gameplay modes.</remarks>
    public string gameplaySceneName = "GameplayScene"; 

    [Header("Difficulty Step Counts")]
    /// <summary>Number of notes/steps in Easy difficulty chart.</summary>
    public int easySteps;
    
    /// <summary>Number of notes/steps in Medium difficulty chart.</summary>
    public int mediumSteps;
    
    /// <summary>Number of notes/steps in Hard difficulty chart.</summary>
    public int hardSteps;

    [Header("Charts (The .txt files)")]
    /// <summary>Text asset containing the chart data for Easy difficulty.</summary>
    public TextAsset easyChart;
    
    /// <summary>Text asset containing the chart data for Medium difficulty.</summary>
    public TextAsset mediumChart;
    
    /// <summary>Text asset containing the chart data for Hard difficulty.</summary>
    public TextAsset hardChart;

    [Header("Audio")]
    /// <summary>Short audio clip played in song selection menu as a preview.</summary>
    /// <remarks>Should be a loopable section (typically 30-60 seconds) representing the song.</remarks>
    public AudioClip songPreviewClip; 

    [Header("Visuals")]
    /// <summary>Primary character sprite used for this song (legacy).</summary>
    public Sprite characterSprite;      
    
    /// <summary>Character sprite displayed for Player 1 during this song.</summary>
    public Sprite p1CharacterSprite;    
    
    /// <summary>Character sprite displayed for Player 2 during this song (two-player mode).</summary>
    public Sprite p2CharacterSprite;    
    
    /// <summary>Background/environment sprite associated with this song's theme.</summary>
    public Sprite environmentSprite; 

    // --- NEW: Gradient for visual effects ---
    [Header("Visual Effects")]
    /// <summary>
    /// Gradient controlling color pulsing and visual effects synchronized with this song's beat.
    /// </summary>
    /// <remarks>
    /// Used by visualizer systems to create song-specific color moods and pulse effects.
    /// The gradient is typically sampled based on beat intensity or song section.
    /// </remarks>
    [Tooltip("Gradient that controls color pulsing for this song")]
    public Gradient visualGradient;

    [Header("Star Thresholds")]
    /// <summary>Minimum score required to earn 5 stars (highest rating).</summary>
    public int fiveStars = 900000;
    
    /// <summary>Minimum score required to earn 4 stars.</summary>
    public int fourStars = 800000;
    
    /// <summary>Minimum score required to earn 3 stars (passing grade).</summary>
    public int threeStars = 700000;
    
    /// <summary>Minimum score required to earn 2 stars.</summary>
    public int twoStars = 600000;
    
    /// <summary>Minimum score required to earn 1 star (lowest passing grade).</summary>
    public int oneStar = 500000;

    /// <summary>
    /// Retrieves the chart text asset for the specified difficulty index.
    /// </summary>
    /// <param name="difficultyIndex">
    /// Difficulty level index:
    /// 0 = Easy,
    /// 1 = Medium,
    /// 2 = Hard
    /// </param>
    /// <returns>TextAsset containing the chart data for the requested difficulty, or Medium if invalid.</returns>
    /// <remarks>
    /// Defaults to Medium chart if an invalid index is provided, ensuring graceful
    /// fallback during development and preventing null reference errors.
    /// </remarks>
    public TextAsset GetChart(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0: return easyChart;
            case 1: return mediumChart;
            case 2: return hardChart;
            default: return mediumChart;
        }
    }
}