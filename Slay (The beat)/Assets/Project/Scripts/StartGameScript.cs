using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles game startup by loading the next scene when the start button is pressed.
/// </summary>
/// <remarks>
/// This script listens for the UI "Start" input action (typically the Enter key, Spacebar,
/// or Start button on a gamepad) and triggers a scene transition. It prevents multiple
/// rapid scene loads by using an isLoading flag.
/// 
/// The script automatically destroys itself when the new scene loads, so no cleanup
/// or flag resetting is required.
/// </remarks>
public class StartGameScript : MonoBehaviour
{
    /// <summary>
    /// Name of the scene to load when start is pressed.
    /// </summary>
    /// <remarks>
    /// This scene name must match exactly with a scene added in Build Settings.
    /// Common values include "Gameplay", "MainMenu", or "Level1".
    /// </remarks>
    public string nextSceneName;

    /// <summary>
    /// InputActions instance for accessing UI input bindings.
    /// </summary>
    private InputActions inputActions;

    /// <summary>
    /// Reference to the "Start" input action from the UI action map.
    /// </summary>
    private InputAction startAction;

    /// <summary>
    /// Flag to prevent multiple scene load attempts while a transition is in progress.
    /// </summary>
    /// <remarks>
    /// This prevents spamming the load action and ensures the transition effect
    /// (fade, animation, etc.) completes before loading the next scene.
    /// Once set to true, scene loading cannot be triggered again.
    /// </remarks>
    private bool isLoading = false;

    /// <summary>
    /// Initializes input actions and retrieves the Start action reference.
    /// </summary>
    /// <remarks>
    /// Called automatically when the GameObject is initialized.
    /// Creates a new InputActions instance and stores a reference to the
    /// UI > Start action for performance.
    /// </remarks>
    void Awake()
    {
        inputActions = new InputActions();
        startAction = inputActions.UI.Start;
    }

    /// <summary>
    /// Enables the Start input action when this component becomes active.
    /// </summary>
    private void OnEnable() => startAction.Enable();

    /// <summary>
    /// Disables the Start input action when this component becomes inactive.
    /// </summary>
    /// <remarks>
    /// Proper cleanup prevents input detection when the script is disabled
    /// or the GameObject is deactivated.
    /// </remarks>
    private void OnDisable() => startAction.Disable();

    /// <summary>
    /// Checks for Start button input and triggers scene loading.
    /// </summary>
    /// <remarks>
    /// Called once per frame. When the Start action is triggered and a transition
    /// is not already in progress (isLoading == false), it:
    /// 1. Sets the loading flag to prevent repeated triggers
    /// 2. Requests scene loading through the TransitionManager
    /// 
    /// The isLoading flag is never reset because this GameObject will be destroyed
    /// when the new scene loads, preventing any unwanted behavior.
    /// </remarks>
    void Update()
    {
        // Check if the action was triggered AND we are not already loading
        if (startAction.triggered && !isLoading)
        {
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