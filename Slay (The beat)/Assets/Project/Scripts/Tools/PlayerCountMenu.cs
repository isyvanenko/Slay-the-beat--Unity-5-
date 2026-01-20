using UnityEngine;

public class PlayerCountMenu : MonoBehaviour
{
    [Header("Next Menu")]
    public GameObject deviceSelectMenuP1;

    public void HandleSelection(int index)
{
    if (index == 0)
        SessionConfig.PlayerCount = 1;
    else if (index == 1)
        SessionConfig.PlayerCount = 2;
    else
    {
        Debug.LogError("Invalid player index: " + index);
        return;
    }

    Debug.Log("Player Count Selected: " + SessionConfig.PlayerCount);

    if(deviceSelectMenuP1 != null) deviceSelectMenuP1.SetActive(true);
    if(gameObject != null)gameObject.SetActive(false);
}


    

    
}
