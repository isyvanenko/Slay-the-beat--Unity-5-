using UnityEngine;

public class RandomSoundPlayer : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] randomSounds;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    
    private void Start()
    {
        // Try to get AudioSource if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // Create AudioSource if none exists
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    // This method will be called by the animation event
    public void PlayRandomSound()
    {
        if (randomSounds == null || randomSounds.Length == 0)
        {
            Debug.LogWarning("No random sounds assigned to " + gameObject.name);
            return;
        }
        
        // Select a random sound from the array
        int randomIndex = Random.Range(0, randomSounds.Length);
        AudioClip clipToPlay = randomSounds[randomIndex];
        
        // Play the sound
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }
    
    // Optional: Method to play sound at a specific time in animation
    public void PlayRandomSoundWithDelay(float delay)
    {
        Invoke(nameof(PlayRandomSound), delay);
    }
}