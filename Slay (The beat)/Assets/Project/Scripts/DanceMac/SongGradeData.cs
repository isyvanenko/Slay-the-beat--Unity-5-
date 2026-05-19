using UnityEngine;

[CreateAssetMenu(fileName = "NewSongGrade", menuName = "RhythmGame/SongGradeData")]
public class SongGradeData : ScriptableObject
{
    // --- NEW: Toggle this ON for your "Random" item ---
    [Header("Special Feature")]
    public bool isRandomOption = false; 

    [Header("Song Info")]
    public string songName;
    public Sprite songJacketSprite;
    public string gameplaySceneName = "GameplayScene"; 

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
    public Sprite characterSprite;      
    public Sprite p1CharacterSprite;    
    public Sprite p2CharacterSprite;    
    public Sprite environmentSprite; 

    // --- NEW: Gradient for visual effects ---
    [Header("Visual Effects")]
    [Tooltip("Gradient that controls color pulsing for this song")]
    public Gradient visualGradient;

    [Header("Star Thresholds")]
    public int fiveStars = 900000;
    public int fourStars = 800000;
    public int threeStars = 700000;
    public int twoStars = 600000;
    public int oneStar = 500000;

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