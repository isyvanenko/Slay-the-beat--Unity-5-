using UnityEngine;

public class QuiteBackgroundMusic : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         MusicManager.Instance.PlayMenu();
         MusicManager.Instance.SetQuiet(true); 
    }

}
