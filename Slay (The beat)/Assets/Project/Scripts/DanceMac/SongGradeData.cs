using UnityEngine;

[CreateAssetMenu(fileName = "NewSongGrade", menuName = "RhythmGame/SongGradeData")]
public class SongGradeData : ScriptableObject
{
    public string songName;
    
    [Header("Character Silhouettes")]
    public Sprite p1CharacterSprite;
    public Sprite p2CharacterSprite;

    [Header("Star Score Thresholds")]
    public int fiveStars = 900000;
    public int fourStars = 700000;
    public int threeStars = 500000;
    public int twoStars = 300000;
    public int oneStar = 100000;
}