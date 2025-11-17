using UnityEngine;
using UnityEngine.SceneManagement; // For loading scenes
using TMPro; // For TextMeshPro
using System.Collections;

/// <summary>
/// This script counts down from a specified time.
/// If the timer reaches 0, it loads a specified scene.
/// The display is capped at 99, even if the time is longer.
/// Call StopTimer() from another script when a choice is made.
/// </summary>
public class TimeoutRestarter : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("The total time in seconds to count down from.")]
    public float totalTimeInSeconds = 30f;

    [Tooltip("The name of the scene to load when the timer hits 0.")]
    public string sceneToLoadOnTimeout = "StartScene"; // Make sure this scene is in your Build Settings

    [Header("Display")]
    [Tooltip("The TextMeshPro UI element to display the countdown on.")]
    public TextMeshProUGUI countdownText;
    [Tooltip("A reference to the 'Unlimited Time' UI object.")]
    public GameObject unlimitedtime;
    [Tooltip("The color the text will be when the timer gets low.")]
    public Color32 lowTimeColor = new Color32(255, 29, 0, 255);
    [Tooltip("The time in seconds when the text should turn red.")]
    public float lowTimeThreshold = 10f;

    private float currentTime;
    private bool isTimerRunning = false; // Re-added this missing variable
    private Color32 defaultColor; // To remember the starting color

    void Start()
    {
        // Store the default color
        if (countdownText != null)
        {
            defaultColor = countdownText.color;
        }

        // Automatically start the timer when the script is enabled
        ResetAndStartTimer();

        // Check for unlimited time at start
        if (totalTimeInSeconds > 1000 && unlimitedtime != null)
        {
            unlimitedtime.SetActive(true);
        }
        else if (unlimitedtime != null)
        {
            unlimitedtime.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the timer to the full duration and starts it.
    /// </summary>
    public void ResetAndStartTimer()
    {
        currentTime = totalTimeInSeconds;
        isTimerRunning = true;
        UpdateTimerDisplay(); // Update display on the first frame
    }

    /// <summary>
    /// Stops the timer from counting down.
    /// Call this from your MenuSelector script when the player makes a choice.
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    void Update()
    {
        // If the timer isn't running, do nothing
        if (!isTimerRunning)
            return;

        // Count down
        currentTime -= Time.deltaTime;

        // --- BRACKETING FIX ---
        // This 'if' block was completely broken before.
        // Check if time has run out
        if (currentTime <= 0)
        {
            currentTime = 0;
            isTimerRunning = false;
            
            Debug.Log($"Timer reached 0! Loading scene: {sceneToLoadOnTimeout}");

            // Re-added the missing scene load logic
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.LoadScene(sceneToLoadOnTimeout);
            }
            else
            {
                SceneManager.LoadScene(sceneToLoadOnTimeout);
            }
        } // --- THIS BRACE WAS MISSING ---

        // This logic is now OUTSIDE the block above, so it runs every frame

        // --- COLOR LOGIC FIX ---
        // This now checks 'currentTime' to see if the timer is low.
        if (currentTime < lowTimeThreshold && currentTime > 0)
        {
            countdownText.color = lowTimeColor;
        }
        else if (currentTime > lowTimeThreshold) // Reset color if not low
        {
            countdownText.color = defaultColor;
        }

        // Update the visual display
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (countdownText != null)
        {
            // Calculate the display time, rounding up (so it shows "10" then "9")
            int timeToDisplay = Mathf.CeilToInt(currentTime);
            
            // Clamp the display value to be 99 at most, as requested
            int clampedTime = Mathf.Clamp(timeToDisplay, 0, 99);
            
            // Update the text
            // "D2" formats the number as two digits (e.g., "09", "08")
            countdownText.text = clampedTime.ToString("D2");
        }
    }
}