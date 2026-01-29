using UnityEngine;
using UnityEngine.UI;

public class NoteObject : MonoBehaviour
{
    [Header("Sync Data")]
    public double spawnTime; 
    public float speed = 300f; 
    public GameplayManager manager; 
    public PlayerScoreManager myScoreManager;

    [Header("Visuals")]
    public Transform arrowGraphic; 
    public RectTransform tailRect;
    public Image[] imagesToColor; 
    public CanvasGroup canvasGroup; 

    [Header("Hit Logic")]
    public bool canBeHit = false; 
    public float holdDuration = 0f; 
    public bool isBeingHeld = false; 
    
    private bool hasMissed = false; 
    private float missTriggerY; 
    private float offScreenY = 1500f; 

    private RectTransform rectTransform;
    private float startY; 

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(double songSpawnTime, float noteSpeed, GameplayManager gameManager, PlayerScoreManager scoreMgr, float missY)
    {
        spawnTime = songSpawnTime;
        speed = noteSpeed;
        manager = gameManager;
        myScoreManager = scoreMgr;
        missTriggerY = missY;
        
        startY = rectTransform.anchoredPosition.y; 
    }

    public void SetRotation(Quaternion rotation) { if (arrowGraphic != null) arrowGraphic.rotation = rotation; }
    
    public void SetColor(Color c)
    {
        foreach(Image img in imagesToColor) if(img != null) img.color = c;
    }

    public void SetupHold(float duration, float noteSpeed)
    {
        holdDuration = duration;
        if (holdDuration > 0 && tailRect != null)
        {
            tailRect.gameObject.SetActive(true);
            float mathLength = duration * speed;
            float yOffset = Mathf.Abs(tailRect.anchoredPosition.y);
            float finalLength = mathLength + yOffset - 25f; 
            if (finalLength < 0) finalLength = 0;
            tailRect.sizeDelta = new Vector2(tailRect.sizeDelta.x, finalLength);
        }
    }

    void Update()
    {
        if (manager == null) return;

        // 1. Movement
        double timeAlive = manager.GetSongTime() - spawnTime;
        float newY = startY + (float)(timeAlive * speed);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);

        // 2. MISS CHECK (Use Logic Threshold)
        if (!hasMissed && !isBeingHeld)
        {
            if (newY > missTriggerY)
            {
                TriggerMiss();
            }
        }

        // 3. VISUAL DESTROY
        // Only auto-destroy if not being held
        if (!isBeingHeld) 
        {
             if (newY > offScreenY) Destroy(gameObject);
        }
    }

    void TriggerMiss()
    {
        hasMissed = true;
        if (myScoreManager != null) myScoreManager.RegisterHit("MISS");
        if (canvasGroup != null) canvasGroup.alpha = 0.5f; 
        SetColor(Color.gray); 
    }
}