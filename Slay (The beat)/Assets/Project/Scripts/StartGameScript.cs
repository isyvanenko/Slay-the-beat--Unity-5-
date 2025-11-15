using UnityEngine;
using UnityEngine.InputSystem;

public class StartGameScript : MonoBehaviour
{
    public string nextSceneName;

    private InputActions inputActions;
    private InputAction startAction;

    void Awake()
    {
        inputActions = new InputActions();
        startAction = inputActions.UI.Start;
    }

    private void OnEnable() => startAction.Enable();
    private void OnDisable() => startAction.Disable();

    void Update()
    {
        if (startAction.triggered)
        {
            TransitionManager.Instance.LoadScene(nextSceneName, 0.5f);
        }
    }
}