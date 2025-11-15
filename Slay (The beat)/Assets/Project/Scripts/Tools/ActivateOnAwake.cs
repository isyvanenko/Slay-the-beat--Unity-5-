using UnityEngine;

public class ActivateOnAwake : MonoBehaviour
{
      [Header("Object to Activate")]
    public GameObject objectToActivate;

    void Awake()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log(objectToActivate.name + " activated on Awake.");
        }
        else
        {
            Debug.LogWarning("No GameObject assigned to ActivateOnAwake.");
        }
    }
}
