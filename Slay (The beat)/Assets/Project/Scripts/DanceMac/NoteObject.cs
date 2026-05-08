using UnityEngine;
using UnityEngine.UI;

public class NoteObject : MonoBehaviour
{
    [Header("Sync Data")]
    public double targetTime; 
    public double spawnTime;  
    public float speed = 300f; 
    public GameplayManager manager; 
    public PlayerScoreManager myScoreManager;
    private LaneController parentLane;

    [Header("Visuals")]
    public Transform arrowGraphic; 
    public RectTransform tailRect;
    public Image[] imagesToColor; 
    private CanvasGroup canvasGroup; 

    [Header("Tail Logic")]
    public RectTransform tailEndMarker; 

    [Header("Hit Logic")]
    public bool canBeHit = false; 
    public float holdDuration = 0f; 
    public bool isBeingHeld = false; 
    private bool hasBeenDropped = false; 
    private bool hasMissed = false; 
    private float missTriggerY; 
    private RectTransform rectTransform;
    private float startY; 
    
    // Pause state
    private bool isPaused = false;
    private Vector2 frozenPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

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

    public void StartHold()
    {
        isBeingHeld = true;
        hasBeenDropped = false;
    }

    public void ReleaseHoldEarly()
    {
        if (isBeingHeld)
        {
            isBeingHeld = false;
            hasBeenDropped = true; 
            TriggerMissVisuals();
        }
    }

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

    public void SetRotation(Quaternion rotation) 
    { 
        if (arrowGraphic != null) 
            arrowGraphic.rotation = rotation; 
    }

    void TriggerMiss()
    {
        if (hasMissed) return;
        hasMissed = true;
        
        if (myScoreManager != null && !hasBeenDropped) 
            myScoreManager.RegisterHit("MISS");

        TriggerMissVisuals();
    }

    void TriggerMissVisuals()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0.3f; 
        foreach (Image img in imagesToColor)
        {
            if (img != null) img.color = Color.gray;
        }
    }
}