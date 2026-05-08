using UnityEngine;

public class SceneFromMenuOpener : MonoBehaviour
{
    public string statsscene;
    public string gameplayscene;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenStats()
    {
        TransitionManager.Instance.LoadScene(statsscene, 0.5f);
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void StartTheGame()
    {
     
            TransitionManager.Instance.LoadScene("PlayerSelection", 0.5f);
        
    }
}
