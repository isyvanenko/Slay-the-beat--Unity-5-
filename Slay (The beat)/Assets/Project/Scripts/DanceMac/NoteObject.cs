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

    [Header("Visuals")]
    public Transform arrowGraphic; 
    public RectTransform tailRect;
    // Drag all images (Arrows and Tail) into this array in the Inspector
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

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(double songHitTime, double songSpawnTime, float noteSpeed, GameplayManager gameManager, PlayerScoreManager scoreMgr, float missY)
    {
        targetTime = songHitTime;
        spawnTime = songSpawnTime;
        speed = noteSpeed;
        manager = gameManager;
        myScoreManager = scoreMgr;
        missTriggerY = missY;
        
        // Cache the Y position from the SpawnPoint where it was instantiated
        startY = rectTransform.anchoredPosition.y; 
    }

    // This is called by the LaneController to set the color
    public void SetColor(Color c)
    {
        foreach (Image img in imagesToColor)
        {
            if (img != null)
            {
                img.color = c;
                // Ensure Alpha is 1 in case it was previously ghosted
                Color temp = img.color;
                temp.a = 1f;
                img.color = temp;
            }
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
            
            // Calculate total math length
            float mathLength = duration * noteSpeed;
            
            // Offset Adjustment for your -50px design
            float finalLength = mathLength - 50f; 
            if (finalLength < 0) finalLength = 0;

            tailRect.sizeDelta = new Vector2(tailRect.sizeDelta.x, finalLength);
            tailRect.SetAsFirstSibling(); // Put behind the arrow
        }
    }

    void Update()
    {
        if (manager == null) return;

        // 1. MOVEMENT
        double timeAlive = manager.GetSongTime() - spawnTime;
        float newY = startY + (float)(timeAlive * speed);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);

        // 2. MISS DETECTION
        if (!hasMissed && !isBeingHeld)
        {
            if (newY > missTriggerY)
            {
                TriggerMiss();
            }
        }

        // 3. CLEANUP
        if (!isBeingHeld && newY > startY + 2500f) 
        {
            Destroy(gameObject);
        }
    }

    public void SetRotation(Quaternion rotation) { if (arrowGraphic != null) arrowGraphic.rotation = rotation; }

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
        // Make the note faint and gray when missed or dropped
        if (canvasGroup != null) canvasGroup.alpha = 0.3f; 
        foreach (Image img in imagesToColor)
        {
            if (img != null) img.color = Color.gray;
        }
    }
}