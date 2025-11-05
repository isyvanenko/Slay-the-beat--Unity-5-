using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    // This 'static' variable is the key. 
    // It lets any other script find this manager easily.
    public static MusicManager instance;

    // Drag your snapshots here in the Inspector
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot clubSnapshot;

    void Awake()
    {
        // --- This is the Singleton pattern ---
        if (instance == null)
        {
            // If 'instance' is empty, this is the first
            // MusicManager. Let's keep it.
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If 'instance' is NOT empty, a MusicManager
            // already exists. Destroy this new, duplicate one.
            Destroy(gameObject);
        }
        // -------------------------------------
    }

    // --- Public Functions for Other Scripts ---

    // Call this to get the "club" sound
    public void SetMusicToClub(float transitionTime = 3f)
    {
        if (clubSnapshot != null)
        {
            clubSnapshot.TransitionTo(transitionTime);
        }
    }

    // Call this to get the "normal" sound
    public void SetMusicToNormal(float transitionTime = 1.5f)
    {
        if (normalSnapshot != null)
        {
            normalSnapshot.TransitionTo(transitionTime);
        }
    }
}