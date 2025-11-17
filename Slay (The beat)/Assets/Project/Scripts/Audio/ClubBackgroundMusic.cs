using UnityEngine;

public class ClubBackgroundMusic : MonoBehaviour
{
   void Start()
{
    MusicManager.Instance.PlayMenu();
    MusicManager.Instance.SetClub(2f); // club mix takes over
    MusicManager.Instance.SetQuiet(false); 
}


}
