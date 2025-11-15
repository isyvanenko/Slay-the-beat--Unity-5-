using UnityEngine;

public class ClubBackgroundMusic : MonoBehaviour
{
   void Start()
{
    MusicManager.Instance.PlayGameplay();
    MusicManager.Instance.SetClub(2f); // club mix takes over
}


}
