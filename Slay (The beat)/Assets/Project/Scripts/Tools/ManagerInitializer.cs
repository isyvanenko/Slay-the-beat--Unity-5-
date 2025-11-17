using UnityEngine;

/// <summary>
/// This script's only job is to "touch" the Instance property
/// of all your persistent singletons during the Awake() phase.
/// 
/// This forces them to be created from their prefabs immediately
/// when the scene loads, instead of "lazily" waiting for
/// another script to call them later.
/// </summary>
public class ManagerInitializer : MonoBehaviour
{
    void Awake()
    {
        // By "getting" the Instance, we force the PersistentSingleton
        // script to run its logic, check if an instance exists,
        // and create one from the Resources/Singletons/ folder if not.
        
        
        var transition = TransitionManager.Instance;

        var sfx = SFXManager.Instance;
        
        // If you had an SFXManager, you'd add it here too:
        // var sfx = SFXManager.Instance;

        // We don't need to *do* anything with the variables,
        // just accessing them is enough to wake them up.
    }
}