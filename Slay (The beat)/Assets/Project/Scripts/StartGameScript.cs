using UnityEngine;
using UnityEngine.InputSystem;

public class StartGameScript : MonoBehaviour
{
    public string nextSceneName;

    private InputActions inputActions;
    private InputAction startAction;

    // --- NEW ---
    // Flag to prevent spamming the load action while transition is in progress
    private bool isLoading = false;

    void Awake()
    {
        inputActions = new InputActions();
        startAction = inputActions.UI.Start;
    }

    private void OnEnable() => startAction.Enable();
    private void OnDisable() => startAction.Disable();

    void Update()
    {
        // Check if the action was triggered AND we are not already loading
        if (startAction.triggered && !isLoading)
        {
            // --- NEW ---
            // Set the flag to true to prevent this from running again
            isLoading = true;
            
            // Call your transition
            TransitionManager.Instance.LoadScene(nextSceneName, 0.5f);

            // We don't need to set 'isLoading' back to false.
            // This script instance will be destroyed when the new scene loads.
            // The StartGameScript in the *next* scene (if it has one)
            // will have its own 'isLoading' variable, which will be 'false' by default.
        }
    }
}