using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a selectable song block in a song selection menu, displaying the song's jacket art.
/// </summary>
/// <remarks>
/// This component is typically attached to a UI button or panel representing a song choice.
/// It receives a SongGradeData asset from a manager (e.g., song selection grid) and updates
/// its jacket image accordingly. The separation allows dynamic population of song lists.
/// </remarks>
public class SongBlock : MonoBehaviour
{
    [Header("Assign the Image child here")]
    /// <summary>
    /// The UI Image component that displays the song's jacket/album art.
    /// </summary>
    /// <remarks>
    /// Should be assigned in the inspector, typically pointing to an Image child GameObject.
    /// </remarks>
    public Image jacketDisplay;

    /// <summary>
    /// Updates the block's visual data with the provided song information.
    /// </summary>
    /// <param name="data">SongGradeData containing the jacket sprite to display.</param>
    /// <remarks>
    /// This method is called by a manager (e.g., SongSelectionManager) to populate
    /// the UI element with the correct song art. If data is null or jacketDisplay is missing,
    /// the method safely does nothing.
    /// </remarks>
    public void UpdateData(SongGradeData data)
    {
        if (data != null && jacketDisplay != null)
        {
            jacketDisplay.sprite = data.songJacketSprite;
        }
    }
}