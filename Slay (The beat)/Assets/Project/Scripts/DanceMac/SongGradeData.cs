using UnityEngine;

[CreateAssetMenu(fileName = "NewSongGrade", menuName = "RhythmGame/SongGradeData")]
public class SongGradeData : ScriptableObject
{
    [Header("Song Info")]
    public string songName;
    public Sprite songJacketSprite;
    public string gameplaySceneName = "GameplayScene"; // The scene this song loads into

    [Header("Difficulty Step Counts")]
    public int easySteps;
    public int mediumSteps;
    public int hardSteps;

    [Header("Charts (The .txt files)")]
    public TextAsset easyChart;
    public TextAsset mediumChart;
    public TextAsset hardChart;

    [Header("Audio")]
    public AudioClip songPreviewClip; 

    [Header("Visuals")]
    public Sprite characterSprite;      // Main silhouette
    public Sprite p1CharacterSprite;    // For results/gameplay
    public Sprite p2CharacterSprite;    // For results/gameplay
    public Sprite environmentSprite; 

    [Header("Star Thresholds")]
    public int fiveStars = 900000;
    public int fourStars = 800000;
    public int threeStars = 700000;
    public int twoStars = 600000;
    public int oneStar = 500000;

    /// <summary>
    /// Returns the correct TextAsset based on difficulty index (0=Easy, 1=Med, 2=Hard)
    /// </summary>
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