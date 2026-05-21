using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a single note object in a rhythm game lane, handling movement, hit detection, and hold mechanics.
/// </summary>
/// <remarks>
/// This component controls an individual note as it travels down a lane toward the receptor.
/// It supports both tap notes (duration = 0) and hold notes (duration > 0) with visual tail rendering.
/// 
/// Key Features:
/// - Time-based movement synchronized with song playback via DSP timing
/// - Distance-based miss detection when notes pass the receptor
/// - Hold note logic with tail stretching and release handling
/// - Pause/resume support with position freezing
/// - Visual feedback for misses (fading, graying out)
/// - Color tinting per lane for visual distinction
/// 
/// The note moves from spawn point to receptor based on the difference between current song time
/// and its spawn time, multiplied by note speed. This ensures frame-rate independent movement
/// that stays perfectly synchronized with the audio.
/// </remarks>
public class NoteObject : MonoBehaviour
{
    [Header("Sync Data")]
    /// <summary>Absolute song time (in seconds) when this note should be hit.</summary>
    public double targetTime; 
    
    /// <summary>Song time when this note was spawned.</summary>
    public double spawnTime;  
    
    /// <summary>Movement speed in pixels per second.</summary>
    public float speed = 300f; 
    
    /// <summary>Reference to the main GameplayManager for timing data.</summary>
    public GameplayManager manager; 
    
    /// <summary>Score manager for registering hits and misses.</summary>
    public PlayerScoreManager myScoreManager;
    
    /// <summary>Reference to the lane controller that spawned this note.</summary>
    private LaneController parentLane;

    [Header("Visuals")]
    /// <summary>Transform of the arrow graphic (rotated based on lane direction).</summary>
    public Transform arrowGraphic; 
    
    /// <summary>RectTransform for the hold note tail (stretches based on hold duration).</summary>
    public RectTransform tailRect;
    
    /// <summary>Array of images to color-tint for lane-specific visuals.</summary>
    public Image[] imagesToColor; 
    
    /// <summary>CanvasGroup for controlling note opacity (used for miss effects).</summary>
    private CanvasGroup canvasGroup; 

    [Header("Tail Logic")]
    /// <summary>Marker at the bottom of the hold tail for completion detection.</summary>
    public RectTransform tailEndMarker; 

    [Header("Hit Logic")]
    /// <summary>Whether the note is within the receptor's hit zone and can be hit.</summary>
    public bool canBeHit = false; 
    
    /// <summary>Duration of hold note (0 for tap notes, >0 for hold notes).</summary>
    public float holdDuration = 0f; 
    
    /// <summary>Whether the note is currently being held (for hold notes).</summary>
    public bool isBeingHeld = false; 
    
    /// <summary>Flag indicating if a hold note was dropped before completion.</summary>
    private bool hasBeenDropped = false; 
    
    /// <summary>Flag indicating if the note has already been missed.</summary>
    private bool hasMissed = false; 
    
    /// <summary>Y-coordinate threshold that triggers a miss when exceeded.</summary>
    private float missTriggerY; 
    
    /// <summary>Reference to this object's RectTransform for position manipulation.</summary>
    private RectTransform rectTransform;
    
    /// <summary>Starting Y position of the note (set at spawn).</summary>
    private float startY; 
    
    /// <summary>Flag indicating if note movement is paused.</summary>
    private bool isPaused = false;
    
    /// <summary>Position stored during pause for restoration.</summary>
    private Vector2 frozenPosition;

    /// <summary>
    /// Initializes note components and caches necessary references.
    /// </summary>
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Initializes the note with all necessary gameplay data.
    /// </summary>
    /// <param name="songHitTime">Absolute song time when note should be hit.</param>
    /// <param name="songSpawnTime">Song time when note was spawned.</param>
    /// <param name="noteSpeed">Movement speed in pixels per second.</param>
    /// <param name="gameManager">Reference to GameplayManager.</param>
    /// <param name="scoreMgr">Reference to PlayerScoreManager.</param>
    /// <param name="missY">Y-coordinate threshold for miss detection.</param>
    /// <param name="lane">LaneController that spawned this note.</param>
    public void Init(double songHitTime, double songSpawnTime, float noteSpeed, GameplayManager gameManager, PlayerScoreManager scoreMgr, float missY, LaneController lane)
    {
        targetTime = songHitTime;
        spawnTime = songSpawnTime;
        speed = noteSpeed;
        manager = gameManager;
        myScoreManager = scoreMgr;
        missTriggerY = missY;
        parentLane = lane;
        
        startY = rectTransform.anchoredPosition.y; 
    }

    /// <summary>
    /// Applies a color tint to all visual elements of the note.
    /// </summary>
    /// <param name="c">Color to apply to note images.</param>
    /// <remarks>
    /// Used for lane-specific coloring to help players distinguish different lanes.
    /// Alpha is forced to 1 to ensure full visibility.
    /// </remarks>
    public void SetColor(Color c)
    {
        foreach (Image img in imagesToColor)
        {
            if (img != null)
            {
                img.color = c;
                Color temp = img.color;
                temp.a = 1f;
                img.color = temp;
            }
        }
    }

    /// <summary>
    /// Pauses or resumes note movement, freezing position in place.
    /// </summary>
    /// <param name="paused">True to pause, false to resume.</param>
    /// <remarks>
    /// When pausing, the current position is captured and movement stops.
    /// When resuming, the note is placed exactly at the frozen position
    /// to maintain perfect positioning after unpausing.
    /// </remarks>
    public void SetPaused(bool paused)
    {
        if (paused == isPaused) return;
        
        if (paused)
        {
            // Store current position when pausing
            frozenPosition = rectTransform.anchoredPosition;
            isPaused = true;
        }
        else
        {
            // Restore the exact position when resuming
            rectTransform.anchoredPosition = frozenPosition;
            isPaused = false;
        }
    }

    /// <summary>
    /// Marks a hold note as being actively held by the player.
    /// </summary>
    public void StartHold()
    {
        isBeingHeld = true;
        hasBeenDropped = false;
    }

    /// <summary>
    /// Releases a hold note early, marking it as dropped.
    /// </summary>
    /// <remarks>
    /// When a hold note is released before completion:
    /// - Stops the hold state
    /// - Marks as dropped (prevents additional miss registration)
    /// - Triggers visual feedback for the drop
    /// </remarks>
    public void ReleaseHoldEarly()
    {
        if (isBeingHeld)
        {
            isBeingHeld = false;
            hasBeenDropped = true; 
            TriggerMissVisuals();
        }
    }

    /// <summary>
    /// Configures the note as a hold note, setting up the visual tail.
    /// </summary>
    /// <param name="duration">Hold duration in seconds.</param>
    /// <param name="noteSpeed">Current note speed for length calculation.</param>
    /// <remarks>
    /// The tail length is calculated as (duration * noteSpeed) - 50px offset.
    /// The offset prevents the tail from extending too far past the receptor.
    /// The tail is placed as the first sibling to render behind the arrow graphic.
    /// </remarks>
    public void SetupHold(float duration, float noteSpeed)
    {
        holdDuration = duration;
        if (holdDuration > 0 && tailRect != null)
        {
            tailRect.gameObject.SetActive(true);
            
            float mathLength = duration * noteSpeed;
            float finalLength = mathLength - 50f; 
            if (finalLength < 0) finalLength = 0;

            tailRect.sizeDelta = new Vector2(tailRect.sizeDelta.x, finalLength);
            tailRect.SetAsFirstSibling();
        }
    }

    /// <summary>
    /// Updates note position and checks for miss conditions each frame.
    /// </summary>
    /// <remarks>
    /// Movement calculation:
    /// - Gets current song time adjusted for pause offsets
    /// - Calculates time alive = currentTime - spawnTime
    /// - New Y position = startY + (timeAlive * speed)
    /// 
    /// Miss detection occurs when:
    /// - Note hasn't been missed yet
    /// - Note isn't currently being held
    /// - Note's Y position exceeds the miss threshold
    /// 
    /// Notes are automatically destroyed when they travel 2500 pixels beyond spawn
    /// to prevent memory leaks from off-screen notes.
    /// </remarks>
    void Update()
    {
        // CRITICAL: Don't move or process anything while paused
        if (isPaused || manager == null || manager.IsPaused())
        {
            return;
        }
        
        // Only move if not paused
        double currentSongTime = manager.GetAdjustedSongTime();
        double timeAlive = currentSongTime - spawnTime;
        float newY = startY + (float)(timeAlive * speed);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);

        // MISS DETECTION - only if not being held and not paused
        if (!hasMissed && !isBeingHeld)
        {
            if (newY > missTriggerY)
            {
                TriggerMiss();
            }
        }

        // Clean up off-screen notes
        if (!isBeingHeld && newY > startY + 2500f) 
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the rotation of the arrow graphic.
    /// </summary>
    /// <param name="rotation">Quaternion rotation to apply.</param>
    /// <remarks>
    /// Used to rotate arrows based on lane direction:
    /// - Left: 0 degrees
    /// - Down: 90 degrees
    /// - Right: 180 degrees
    /// - Up: 270 degrees
    /// </remarks>
    public void SetRotation(Quaternion rotation) 
    { 
        if (arrowGraphic != null) 
            arrowGraphic.rotation = rotation; 
    }

    /// <summary>
    /// Triggers miss registration and visual feedback for a missed note.
    /// </summary>
    /// <remarks>
    /// Prevents multiple miss registrations by checking hasMissed flag.
    /// Registers "MISS" with score manager only if the note wasn't dropped.
    /// Applies visual effects: reduces opacity, grays out colors.
    /// </remarks>
    void TriggerMiss()
    {
        if (hasMissed) return;
        hasMissed = true;
        
        if (myScoreManager != null && !hasBeenDropped) 
            myScoreManager.RegisterHit("MISS");

        TriggerMissVisuals();
    }

    /// <summary>
    /// Applies visual feedback for misses or dropped hold notes.
    /// </summary>
    /// <remarks>
    /// Visual changes include:
    /// - Setting alpha to 0.3 (semi-transparent)
    /// - Changing all colored images to gray
    /// 
    /// This provides clear visual feedback that the note was not successfully hit.
    /// </remarks>
    void TriggerMissVisuals()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0.3f; 
        foreach (Image img in imagesToColor)
        {
            if (img != null) img.color = Color.gray;
        }
    }
}