using UnityEngine;
using UnityEngine.UI;

public class SongBlock : MonoBehaviour
{
    [Header("Assign the Image child here")]
    public Image jacketDisplay;

    // The Manager calls this name specifically
    public void UpdateData(SongGradeData data)
    {
        if (data != null && jacketDisplay != null)
        {
            jacketDisplay.sprite = data.songJacketSprite;
        }
    }
}