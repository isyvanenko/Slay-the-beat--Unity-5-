using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays and handles difficulty selection for a song before starting gameplay.
/// </summary>
/// <remarks>
/// This component presents three difficulty options (Easy, Medium, Hard) with visual feedback,
/// step count displays, and song metadata (name and jacket art). It uses hardwired input polling
/// for left/right navigation and confirmation.
/// 
/// Key Features:
/// - Fades in the menu when shown
/// - Displays song name and jacket sprite from the selected SongGradeData
/// - Shows step counts for each difficulty (loaded from SongGradeData)
/// - Scales the currently selected difficulty box and highlights its outline
/// - Supports keyboard (arrow keys, Enter) and gamepad (d-pad, South button) input
/// - Loads the GameplayScene after difficulty confirmation
/// 
/// The menu is designed to be shown after a song is selected, allowing the player to choose
/// the difficulty level before starting the game.
/// </remarks>
[RequireComponent(typeof(CanvasGroup))]
public class DifficultySelector : MonoBehaviour
{
    [Header("Song Header Info")]
    /// <summary>Text component that displays the name of the selected song.</summary>
    public TextMeshProUGUI songNameText;
    
    /// <summary>Image component that shows the jacket/album art of the selected song.</summary>
    public Image jacketImageDisplay;

    [Header("UI Box Elements")]
    /// <summary>Array of RectTransforms for the three difficulty boxes (0=Easy, 1=Medium, 2=Hard).</summary>
    public RectTransform[] difficultyBoxes; 
    
    /// <summary>Array of outline Images for each difficulty box (used for selection highlighting).</summary>
    public Image[] outlines;
    
    /// <summary>Array of TextMeshProUGUI components that display step counts for each difficulty.</summary>
    public TextMeshProUGUI[] stepCountTexts; 

    [Header("Visual Settings")]
    /// <summary>Scale multiplier for the selected difficulty box.</summary>
    public float selectedScale = 1.15f;
    
    /// <summary>Animation speed for scaling the difficulty boxes (Lerp factor per second).</summary>
    public float animSpeed = 10f;

    /// <summary>Currently selected difficulty index (0=Easy, 1=Medium, 2=Hard).</summary>
    private int index = 1; 
    
    /// <summary>Flag indicating if a difficulty has been confirmed (prevents double selection).</summary>
    private bool hasSelected = false;
    
    /// <summary>CanvasGroup for controlling fade-in and interaction.</summary>
    private CanvasGroup canvasGroup;
    
    /// <summary>Currently selected song data (from GameDataBridge).</summary>
    private SongGradeData currentSong;

    /// <summary>
    /// Initializes the component and caches the CanvasGroup reference with alpha 0.
    /// </summary>
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Shows the difficulty selection menu with data from the currently selected song.
    /// </summary>
    /// <remarks>
    /// This method should be called after a song is selected (e.g., from a song selection menu).
    /// It:
    /// 1. Retrieves the selected song from GameDataBridge
    /// 2. Updates the UI with song name and jacket art
    /// 3. Displays step counts for each difficulty
    /// 4. Resets selection to Medium (index 1)
    /// 5. Starts the fade-in coroutine
    /// </remarks>
    public void ShowMenu()
    {
        currentSong = GameDataBridge.SelectedSong;
        
        if (currentSong != null)
        {
            // Update Text
            if (songNameText != null) 
                songNameText.text = currentSong.songName;
                
            // Update jacket art from the song's sprite
            if (jacketImageDisplay != null)
            {
                jacketImageDisplay.sprite = currentSong.songJacketSprite; 
            }
        }

        UpdateAllStepDisplays();

        index = 1;
        hasSelected = false;
        
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    /// <summary>
    /// Handles input polling and visual updates each frame.
    /// </summary>
    /// <remarks>
    /// Input is polled directly from Keyboard and Gamepad using the old Input System.
    /// - Left/Right arrows or D-pad: Move selection
    /// - Enter or South button (A on Xbox, Cross on PlayStation): Confirm selection
    /// 
    /// Visual updates:
    /// - Scales the selected difficulty box based on selectedScale
    /// - Changes outline color to yellow for the selected box, black for others
    /// 
    /// Input is only processed when the menu is fully faded in (alpha > 0.9) and no selection has been made.
    /// </remarks>
    void Update()
    {
        if (hasSelected || canvasGroup.alpha < 0.9f) return;

        // Hardwired input polling for navigation
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || 
            (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame))
        {
            Move(-1);
        }
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || 
            (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame))
        {
            Move(1);
        }
        if (Keyboard.current.enterKey.wasPressedThisFrame || 
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            Confirm();
        }

        // Visual updates for all difficulty boxes
        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            bool isCurrent = (i == index);
            difficultyBoxes[i].localScale = Vector3.Lerp(difficultyBoxes[i].localScale, 
                Vector3.one * (isCurrent ? selectedScale : 1f), Time.unscaledDeltaTime * animSpeed);

            if (outlines[i] != null)
                outlines[i].color = isCurrent ? Color.yellow : Color.black;
        }
    }

    /// <summary>
    /// Moves the selection left or right.
    /// </summary>
    /// <param name="dir">-1 for left (decrease index), +1 for right (increase index).</param>
    /// <remarks>
    /// The index is clamped between 0 and difficultyBoxes.Length - 1.
    /// A sound effect could be played here (placeholder comment).
    /// </remarks>
    void Move(int dir)
    {
        int next = Mathf.Clamp(index + dir, 0, difficultyBoxes.Length - 1);
        if (next != index)
        {
            index = next;
            // Play sound here if you have an AudioSource
        }
    }

    /// <summary>
    /// Confirms the selected difficulty and loads the gameplay scene.
    /// </summary>
    /// <remarks>
    /// Sets hasSelected to true to prevent multiple confirmations, stores the selected difficulty
    /// index in GameDataBridge.SelectedDifficulty, and immediately loads the "GameplayScene".
    /// </remarks>
    void Confirm()
    {
        hasSelected = true;
        GameDataBridge.SelectedDifficulty = index;
        SceneManager.LoadScene("GameplayScene");
    }

    /// <summary>
    /// Updates the step count displays for all difficulties from the current song data.
    /// </summary>
    /// <remarks>
    /// Assumes stepCountTexts array indices correspond to difficulty indices (0=Easy, 1=Medium, 2=Hard).
    /// </remarks>
    void UpdateAllStepDisplays()
    {
        if (currentSong == null) return;
        stepCountTexts[0].text = currentSong.easySteps.ToString();
        stepCountTexts[1].text = currentSong.mediumSteps.ToString();
        stepCountTexts[2].text = currentSong.hardSteps.ToString();
    }

    /// <summary>
    /// Coroutine that smoothly fades in the menu over time.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Increases the CanvasGroup alpha by 4 per second using unscaledDeltaTime.
    /// After reaching full alpha, enables interactivity and raycast blocking.
    /// </remarks>
    IEnumerator FadeInRoutine()
    {
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime * 4f;
            yield return null;
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}