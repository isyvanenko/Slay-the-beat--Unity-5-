using UnityEngine;

public class NormalBackgroundMusic : MonoBehaviour
{
    void Start()
{
    MusicManager.Instance.PlayMenu();
    MusicManager.Instance.SetNormal();
    MusicManager.Instance.SetQuiet(false); 
}

}
