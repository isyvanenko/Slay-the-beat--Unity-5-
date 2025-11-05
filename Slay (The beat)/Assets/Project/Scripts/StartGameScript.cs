using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;


public class StartGameScript : MonoBehaviour
{

    [Tooltip("The exact name of the scene you want to load")]
    public string nextSceneName;

    private InputActions inputActions; // This is your generated C# class
    private InputAction startAction;

    public GameObject SceneTransManager;
    private Animator anim;

    void Awake()
    {
        // 2. Create a new instance of your InputActions
        inputActions = new InputActions();

        // 3. Find the action we care about (Map: "UI", Action: "Start")
        // Make sure the names "UI" and "Start" match your asset exactly!
        startAction = inputActions.UI.Start;
    }

    private void Start()
    {
        if (SceneTransManager != null)
        {
            anim = SceneTransManager.GetComponent<Animator>();
        }
        else
        {
            Debug.Log("Missing a reference to the Scene Trans Manager");
        }
    }

    private void OnEnable()
    {
        // 4. Enable the Action
        startAction.Enable();
    }

    private void OnDisable()
    {
        // 5. Disable the Action (good for cleanup)
        startAction.Disable();
    }

    void Update()
    {
        // 6. This is the part you were asking for!
        //    We check if the 'Start' button was pressed *this frame*.
        if (startAction.triggered)
        {
            // --- This is your logic ---
            Debug.Log("Start Button Pressed!");

            StartCoroutine(CallNextLevel());
        }
    }
    private IEnumerator CallNextLevel()
    {
        anim.SetTrigger("EndScene");

        

        // --- This is your logic ---
        Debug.Log("Start Button Pressed!");

        // 1. Tell MusicManager to change
        if (MusicManager.instance != null)
        {
            MusicManager.instance.SetMusicToClub(3f);
        }
        yield return new WaitForSeconds(0.8f);


        // 2. Load the next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        // --- End of your logic ---

    }



}

