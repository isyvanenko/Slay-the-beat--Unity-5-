using UnityEngine;

/// <summary>
/// Sounds shared by every Tug of Slay bar, since a bar can be added to any level at runtime.
/// </summary>
/// <remarks>
/// The asset must stay at Resources/TugOfSlaySounds so the bar can load it.
/// </remarks>
[CreateAssetMenu(fileName = "TugOfSlaySounds", menuName = "RhythmGame/TugOfSlaySounds")]
public class TugOfSlaySounds : ScriptableObject
{
    public AudioClip rainbowSound;
    public AudioClip clashWinSound;
    public AudioClip clashTieSound;
    public AudioClip slayTickSound;
    public AudioClip savedSound;
    public AudioClip slayWinSound;

    [Range(0f, 1f)] public float volume = 0.8f;
}
