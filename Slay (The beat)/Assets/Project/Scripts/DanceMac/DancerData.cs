using UnityEngine;

/// <summary>
/// Everything about one dancer: how it looks on the character select screen and what dances in the level.
/// </summary>
/// <remarks>
/// Songs point at the dancer their level was built with (<see cref="SongGradeData.dancer"/>),
/// song select lists the dancers players can pick, and GameplayManager puts the picked one in the level.
/// A dancer with no prefab is the "no dancer" choice: the level plays without one.
/// </remarks>
[CreateAssetMenu(fileName = "New Dancer", menuName = "Slay/Dancer")]
public class DancerData : ScriptableObject
{
    [Header("Select Screen")]
    public string displayName = "DANCER";

    [Tooltip("Big picture shown while the dancer is highlighted, where the song's character usually is")]
    public Sprite portrait;

    [Tooltip("Picture on the dancer's card in the row, like a song cover. Uses the portrait when empty")]
    public Sprite cardImage;

    [Header("In the Level")]
    [Tooltip("The dancer model with its Animator. Leave empty to make this the 'no dancer' choice")]
    public GameObject prefab;

    [Tooltip("Optional. Replaces the prefab's animator controller. Needs Idle, Left1-3, Right1-3, Up1-3 and Down1-3 states like the other dancers")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("Size compared to the level's own dancer. 1 makes them the same height")]
    public float sizeMultiplier = 1f;

    /// <summary>True for the "no dancer" choice.</summary>
    public bool IsNoDancer => prefab == null;
}
