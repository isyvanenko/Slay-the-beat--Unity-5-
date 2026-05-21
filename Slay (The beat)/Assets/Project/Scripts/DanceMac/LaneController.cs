using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections; 
using System.Collections.Generic;

/// <summary>
/// Manages a single note lane, handling note spawning, input detection, scoring, and visual feedback.
/// </summary>
/// <remarks>
/// This component controls a lane in a rhythm game, processing note objects as they travel
/// toward a receptor. It handles both standard notes and hold notes, with configurable
/// timing windows for different judgment levels (PERFECT, GOOD, MEH).
/// 
/// Key Features:
/// - Input detection via Unity's Input System with press/release handling
/// - Note spawning with configurable speed and timing offsets
/// - Hit detection with distance-based timing windows
/// - Hold note mechanics with continuous scoring during holds
/// - Visual feedback (receptor flash, scaling, hold effects)
/// - Character animation triggering on successful hits
/// - Pause/resume support for all lane activity
/// - Collision-based note tracking using trigger zones
/// 
/// The lane uses a simple list-based note management system where notes are processed
/// in order of arrival. Perfect for rhythm games with scrolling note highways.
/// </remarks>
public class LaneController : MonoBehaviour
{
    [Header("Configuration")]
    /// <summary>Name of the Input Action to bind to this lane (e.g., "Left", "Right").</summary>
    public string inputActionName;
    
    /// <summary>Prefab used for spawning note objects in this lane.</summary>
    public GameObject notePrefab;
    
    /// <summary>Transform where new notes are instantiated.</summary>
    public Transform spawnPoint;
    
    /// <summary>Speed at which notes travel down the lane (pixels per second).</summary>
    public float noteSpeed = 300f; 

    [Header("Audio")]
    /// <summary>AudioSource for playing hit sounds.</summary>
    public AudioSource sfxSource;   
    
    /// <summary>Sound effect played when a note is successfully hit.</summary>
    public AudioClip hitSound;      
    
    /// <summary>Volume level for hit sounds (0-1 range).</summary>
    [Range(0f, 1f)]
    public float hitVolume = 1.0f; 

    [Header("Visual Feedback")]
    /// <summary>Image component for the receptor at the bottom of the lane.</summary>
    public Image receptorImage;     
    
    /// <summary>Default sprite shown on the receptor when idle.</summary>
    public Sprite defaultSprite;    
    
    /// <summary>Sprite shown briefly when a note is successfully hit.</summary>
    public Sprite hitSprite;        
    
    /// <summary>Duration in seconds that the hit sprite is displayed.</summary>
    public float hitEffectDuration = 0.15f; 

    [Header("Scoring")]
    /// <summary>Reference to the score manager for registering hits and points.</summary>
    public PlayerScoreManager scoreManager; 

    [Header("Character Animation")]
    /// <summary>Animation controller for triggering character reactions on hits.</summary>
    public CharacterAnimationController charAnimator; 
    
    /// <summary>Direction name passed to character animator (e.g., "Left", "Up").</summary>
    public string laneDirectionName; 

    [Header("Visuals")]
    /// <summary>Color tint applied to notes spawned in this lane.</summary>
    public Color targetColor = Color.white;
    
    /// <summary>GameObject that rotates during hold notes for visual feedback.</summary>
    public GameObject holdFlashObject; 
    
    /// <summary>Transform that holds the mask container for hold note visuals.</summary>
    public Transform holdMaskContainer; 

    [Header("Hit Logic")]
    /// <summary>Distance threshold (pixels) for PERFECT judgment.</summary>
    public float perfectThreshold = 30f;
    
    /// <summary>Distance threshold (pixels) for GOOD judgment.</summary>
    public float goodThreshold = 90f;   
    
    /// <summary>Distance threshold (pixels) for MEH judgment (maximum hit distance).</summary>
    public float mehThreshold = 160f;   

    /// <summary>Input action bound to this lane (Left, Right, Up, Down, etc.).</summary>
    private InputAction laneAction; 
    
    /// <summary>List of active notes currently within the lane's trigger zone.</summary>
    private List<NoteObject> activeNotes = new List<NoteObject>();
    
    /// <summary>Currently held note (for hold note mechanics).</summary>
    private NoteObject currentHoldNote = null;
    
    /// <summary>Coroutine reference for hit effect animation.</summary>
    private Coroutine hitEffectCoroutine; 
    
    /// <summary>Timer for accumulating hold note scoring ticks.</summary>
    private float holdScoreTimer = 0f; 
    
    /// <summary>Timestamp of the last button press for cooldown detection.</summary>
    private float lastPressTime;
    
    /// <summary>Minimum time between input registrations to prevent double-hits.</summary>
    public float inputCooldown = 0.05f; 

    /// <summary>Reference to the lane's RectTransform for position calculations.</summary>
    private RectTransform rectTransform;
    
    /// <summary>Flag indicating if movement and input are paused.</summary>
    private bool isPaused = false;

    /// <summary>
    /// Initializes the lane component and caches RectTransform reference.
    /// </summary>
    void Awake() => rectTransform = GetComponent<RectTransform>();

    /// <summary>
    /// Initializes the lane with an InputAction and subscribes to its events.
    /// </summary>
    /// <param name="action">InputAction to bind to this lane.</param>
    /// <remarks>
    /// Automatically disables any existing action before binding the new one.
    /// Subscribes to both performed (press) and canceled (release) events
    /// to handle both tap notes and hold notes.
    /// </remarks>
    public void Initialize(InputAction action)
    {
        if (laneAction != null)
        {
            laneAction.performed -= OnActionTriggered;
            laneAction.canceled -= OnActionCanceled;
        }
        laneAction = action;
        if (laneAction != null)
        {
            laneAction.Enable();
            laneAction.performed += OnActionTriggered;
            laneAction.canceled += OnActionCanceled;
        }
    }

    /// <summary>
    /// Handles button press events for this lane.
    /// </summary>
    /// <param name="context">Input action callback context.</param>
    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!isPaused && context.ReadValueAsButton()) OnPress();
    }

    /// <summary>
    /// Handles button release events for this lane (used for hold notes).
    /// </summary>
    /// <param name="context">Input action callback context.</param>
    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        if (!isPaused) OnRelease();
    }

    /// <summary>
    /// Spawns a new note in this lane.
    /// </summary>
    /// <param name="duration">Hold duration (0 for tap notes).</param>
    /// <param name="noteTime">Absolute song time when note should be hit.</param>
    /// <param name="manager">Reference to the GameplayManager for timing data.</param>
    /// <remarks>
    /// Notes are instantiated at the spawn point and configured with:
    /// - Spawn time based on note time minus spawn offset
    /// - Movement speed
    /// - Miss threshold position (for automatic miss detection)
    /// - Hold duration and scaling (if applicable)
    /// - Lane-specific color tint
    /// 
    /// Hold notes are visually distinct and handled differently during gameplay.
    /// </remarks>
    public void SpawnNote(float duration, float noteTime, GameplayManager manager)
    {
        if (notePrefab == null) return;
        
        GameObject newNoteObj = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity, transform.parent);
        newNoteObj.transform.SetAsFirstSibling(); 
        
        NoteObject newNote = newNoteObj.GetComponent<NoteObject>();

        if (newNote != null)
        {
            double songSpawnTime = noteTime - manager.spawnOffset;
            float receptorY = receptorImage != null ? receptorImage.rectTransform.anchoredPosition.y : rectTransform.anchoredPosition.y;
            float missY = receptorY + mehThreshold; 

            newNote.Init(noteTime, songSpawnTime, noteSpeed, manager, scoreManager, missY, this); 
            newNote.SetRotation(transform.rotation); 
            newNote.SetupHold(duration, noteSpeed);
            
            newNote.SetColor(targetColor);
        }
    }

    /// <summary>
    /// Pauses all note movement and input processing in this lane.
    /// </summary>
    /// <remarks>
    /// Used during game pause to freeze note positions and prevent input.
    /// All active notes and the current hold note are notified to pause their movement.
    /// </remarks>
    public void PauseMovement()
    {
        isPaused = true;
        
        // Tell all notes they are paused (they will freeze position)
        foreach (NoteObject note in activeNotes)
        {
            if (note != null)
                note.SetPaused(true);
        }
        
        if (currentHoldNote != null)
            currentHoldNote.SetPaused(true);
    }

    /// <summary>
    /// Resumes note movement and input processing after pause.
    /// </summary>
    public void ResumeMovement()
    {
        isPaused = false;
        
        // Tell all notes to resume (they will stay at frozen position)
        foreach (NoteObject note in activeNotes)
        {
            if (note != null)
                note.SetPaused(false);
        }
        
        if (currentHoldNote != null)
            currentHoldNote.SetPaused(false);
    }

    /// <summary>
    /// Processes a button press, evaluating the closest note for hit detection.
    /// </summary>
    /// <remarks>
    /// Hit detection process:
    /// 1. Apply input cooldown check to prevent rapid double-hits
    /// 2. Visual feedback: scale up the lane
    /// 3. Check if there are any active notes
    /// 4. Calculate distance from receptor for the first note in queue
    /// 5. Determine judgment based on distance thresholds
    /// 6. Register hit with score manager
    /// 7. Trigger visual and audio feedback
    /// 8. Handle hold notes vs tap notes differently
    /// 
    /// For hold notes: Note enters hold mode but remains active for duration
    /// For tap notes: Note is destroyed immediately
    /// </remarks>
    private void OnPress()
    {
        if (Time.time - lastPressTime < inputCooldown) return;
        lastPressTime = Time.time;
        
        transform.localScale = Vector3.one * 1.7f;
        if (activeNotes.Count == 0) return;

        NoteObject targetNote = activeNotes[0];
        if (targetNote == null) 
        {
            activeNotes.RemoveAt(0);
            return;
        }
        
        float distance = Mathf.Abs(targetNote.GetComponent<RectTransform>().anchoredPosition.y - rectTransform.anchoredPosition.y);

        if (distance < mehThreshold)
        {
            if (sfxSource != null && hitSound != null) sfxSource.PlayOneShot(hitSound, hitVolume);

            string judgment = "MEH"; 
            if (distance < goodThreshold) judgment = "GOOD";
            if (distance < perfectThreshold) judgment = "PERFECT";

            if (scoreManager != null) scoreManager.RegisterHit(judgment);
            PlayHitEffect();

            if (charAnimator != null && !string.IsNullOrEmpty(laneDirectionName))
                charAnimator.TriggerAnimation(laneDirectionName);

            targetNote.StartHold(); 

            if (targetNote.holdDuration <= 0)
            {
                DestroyNote(targetNote);
            }
            else
            {
                currentHoldNote = targetNote;
                activeNotes.Remove(targetNote); 
                
                if (holdFlashObject != null) holdFlashObject.SetActive(true);
                if (holdMaskContainer != null) currentHoldNote.transform.SetParent(holdMaskContainer, true);
            }
        }
    }

    /// <summary>
    /// Processes button release, primarily used for ending hold notes.
    /// </summary>
    /// <remarks>
    /// When a hold note is released, it's destroyed and visual effects are disabled.
    /// The note is returned to the normal parent transform before destruction.
    /// </remarks>
    private void OnRelease()
    {
        if (currentHoldNote != null)
        {
            currentHoldNote.ReleaseHoldEarly(); 
            currentHoldNote.transform.SetParent(transform.parent, true); 
            currentHoldNote = null; 
            if (holdFlashObject != null) holdFlashObject.SetActive(false);
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Updates hold note scoring and visual effects each frame.
    /// </summary>
    /// <remarks>
    /// For hold notes:
    /// - Scales down the lane visual while holding
    /// - Rotates the hold flash object for visual feedback
    /// - Awards tick points every 0.15 seconds while held
    /// - Checks if the hold tail has reached the receptor for completion scoring
    /// 
    /// For non-hold states: Smoothly returns lane scale to normal.
    /// </remarks>
    void Update()
    {
        if (isPaused) return;
        
        if (currentHoldNote == null)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);
            return; 
        }

        if (laneAction != null && !laneAction.IsPressed())
        {
            OnRelease();
            return;
        }

        if (currentHoldNote == null || currentHoldNote.gameObject == null) 
        { 
            currentHoldNote = null; 
            return; 
        }

        if (holdFlashObject != null) holdFlashObject.transform.Rotate(0, 0, 300 * Time.deltaTime);
        transform.localScale = Vector3.one * 1.1f;

        if (scoreManager != null)
        {
            holdScoreTimer += Time.deltaTime;
            if (holdScoreTimer >= 0.15f) 
            {
                holdScoreTimer = 0f;
                scoreManager.AddScore(scoreManager.scorePerHoldTick);
            }
        }

        if (currentHoldNote.tailEndMarker != null)
        {
            float tailBottomY = currentHoldNote.tailEndMarker.position.y;
            float receptorY = receptorImage.transform.position.y;

            if (tailBottomY >= receptorY)
            {
                if (scoreManager != null)
                {
                    scoreManager.AddScore(scoreManager.scorePerHoldFinish);
                    scoreManager.RegisterHit("PERFECT"); 
                }
                
                Destroy(currentHoldNote.gameObject);
                currentHoldNote = null;
                if (holdFlashObject != null) holdFlashObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Plays visual hit effect on the receptor (sprite flash).
    /// </summary>
    void PlayHitEffect()
    {
        if (receptorImage == null || hitSprite == null) return;
        if (hitEffectCoroutine != null) StopCoroutine(hitEffectCoroutine);
        hitEffectCoroutine = StartCoroutine(HitEffectRoutine());
    }

    /// <summary>
    /// Coroutine that temporarily changes receptor sprite on hit.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    IEnumerator HitEffectRoutine()
    {
        receptorImage.sprite = hitSprite;
        yield return new WaitForSeconds(hitEffectDuration);
        receptorImage.sprite = defaultSprite;
    }

    /// <summary>
    /// Destroys a note and removes it from the active notes list.
    /// </summary>
    /// <param name="note">Note object to destroy.</param>
    private void DestroyNote(NoteObject note) 
    { 
        if(activeNotes.Contains(note)) activeNotes.Remove(note); 
        Destroy(note.gameObject); 
    }

    /// <summary>
    /// Called when a note enters the lane's trigger zone.
    /// </summary>
    /// <param name="other">Collider that entered the trigger.</param>
    /// <remarks>
    /// Adds the note to the active notes list and marks it as hittable.
    /// Notes are processed in FIFO order.
    /// </remarks>
    void OnTriggerEnter2D(Collider2D other) 
    { 
        if (other.TryGetComponent(out NoteObject note)) 
        { 
            activeNotes.Add(note); 
            note.canBeHit = true; 
        } 
    }

    /// <summary>
    /// Called when a note exits the lane's trigger zone.
    /// </summary>
    /// <param name="other">Collider that exited the trigger.</param>
    /// <remarks>
    /// Removes the note from active tracking and marks it as unhittable.
    /// Notes that exit without being hit will be considered misses.
    /// </remarks>
    void OnTriggerExit2D(Collider2D other) 
    { 
        if (other.TryGetComponent(out NoteObject note)) 
        { 
            activeNotes.Remove(note); 
            note.canBeHit = false; 
        } 
    }
    
    /// <summary>
    /// Cleans up input event subscriptions when the component is disabled.
    /// </summary>
    void OnDisable() 
    { 
        if (laneAction != null) 
        { 
            laneAction.performed -= OnActionTriggered;
            laneAction.canceled -= OnActionCanceled;
        } 
    }
}