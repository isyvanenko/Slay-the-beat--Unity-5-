using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelAutoLoad : MonoBehaviour
{
    [Header("Timer Settings")]
    public float countdownTime = 15f;

    public GameObject SceneTransManager;
    private Animator anim;

    [Header("UI")]
    public Slider timerSlider;

    private float timer;

    void Start()
    {
        timer = countdownTime;

        if (timerSlider != null)
        {
            timerSlider.minValue = 0;
            timerSlider.maxValue = 1;
            timerSlider.value = 0; // start at 0 (full time)
        }

        if (SceneTransManager != null)
        {
            anim = SceneTransManager.GetComponent<Animator>();
        }
        else
        {
            Debug.Log("Missing a reference to the Scene Trans Manager");
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;

        // Update slider: 0 -> full time, 1 -> time done
        if (timerSlider != null)
        {
            float progress = 1 - (timer / countdownTime);
            timerSlider.value = Mathf.Clamp01(progress);
        }

        if (timer <= 0)
        {
            StartCoroutine(CallSpawner());
        }
    }

    public IEnumerator CallSpawner()
    {
        anim.SetTrigger("EndScene");
        yield return new WaitForSeconds(0.8f);
        LoadNextLevel();
   }

    void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextSceneIndex);
    }
}