using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections; 
using System.Collections.Generic;

public class LaneController : MonoBehaviour
{
    [Header("Configuration")]
    public string inputActionName;
    public GameObject notePrefab;
    public Transform spawnPoint;
    public float noteSpeed = 300f; 

    [Header("Audio")]
    public AudioSource sfxSource;   
    public AudioClip hitSound;      
    [Range(0f, 1f)]
    public float hitVolume = 1.0f; 

    [Header("Visual Feedback")]
    public Image receptorImage;     
    public Sprite defaultSprite;    
    public Sprite hitSprite;        
    public float hitEffectDuration = 0.15f; 

    [Header("Scoring")]
    public PlayerScoreManager scoreManager; 

    [Header("Character Animation")]
    public CharacterAnimationController charAnimator; 
    public string laneDirectionName; 

    [Header("Visuals")]
    public Color targetColor = Color.white;
    public GameObject holdFlashObject; 
    public Transform holdMaskContainer; 

    [Header("Hit Logic")]
    public float perfectThreshold = 30f;
    public float goodThreshold = 90f;   
    public float mehThreshold = 160f;   

    private InputAction laneAction; 
    private List<NoteObject> activeNotes = new List<NoteObject>();
    private NoteObject currentHoldNote = null;
    private Coroutine hitEffectCoroutine; 
    private float holdScoreTimer = 0f; 
    private float lastPressTime;
    public float inputCooldown = 0.05f; 

    private RectTransform rectTransform;
    
    // Pause state - simple boolean to ignore updates
    private bool isPaused = false;

    void Awake() => rectTransform = GetComponent<RectTransform>();

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

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!isPaused && context.ReadValueAsButton()) OnPress();
    }

    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        if (!isPaused) OnRelease();
    }

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

    void PlayHitEffect()
    {
        if (receptorImage == null || hitSprite == null) return;
        if (hitEffectCoroutine != null) StopCoroutine(hitEffectCoroutine);
        hitEffectCoroutine = StartCoroutine(HitEffectRoutine());
    }

    IEnumerator HitEffectRoutine()
    {
        receptorImage.sprite = hitSprite;
        yield return new WaitForSeconds(hitEffectDuration);
        receptorImage.sprite = defaultSprite;
    }

    private void DestroyNote(NoteObject note) 
    { 
        if(activeNotes.Contains(note)) activeNotes.Remove(note); 
        Destroy(note.gameObject); 
    }

    void OnTriggerEnter2D(Collider2D other) 
    { 
        if (other.TryGetComponent(out NoteObject note)) 
        { 
            activeNotes.Add(note); 
            note.canBeHit = true; 
        } 
    }

    void OnTriggerExit2D(Collider2D other) 
    { 
        if (other.TryGetComponent(out NoteObject note)) 
        { 
            activeNotes.Remove(note); 
            note.canBeHit = false; 
        } 
    }
    
    void OnDisable() 
    { 
        if (laneAction != null) 
        { 
            laneAction.performed -= OnActionTriggered;
            laneAction.canceled -= OnActionCanceled;
        } 
    }
}