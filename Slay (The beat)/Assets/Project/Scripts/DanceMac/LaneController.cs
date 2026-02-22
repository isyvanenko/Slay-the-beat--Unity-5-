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

    // Changed from InputActionMap to a single InputAction reference
    private InputAction laneAction; 
    private List<NoteObject> activeNotes = new List<NoteObject>();
    private NoteObject currentHoldNote = null;
    private Coroutine hitEffectCoroutine; 
    private float holdScoreTimer = 0f; 

    private float lastPressTime;
    public float inputCooldown = 0.05f; // 50ms buffer

    /// <summary>
    /// REPLACES SETUP: This is called by GameplayManager to link the 
    /// PlayerInput actions to this specific lane.
    /// </summary>
    public void Initialize(InputAction action)
    {
        // Unsubscribe from old action if it exists to prevent memory leaks
        if (laneAction != null)
        {
            laneAction.performed -= OnActionTriggered;
            laneAction.canceled -= OnActionCanceled;
        }

        laneAction = action;

        if (laneAction != null)
        {
            laneAction.Enable();
            // We use these wrapper methods to call your existing logic
            laneAction.performed += OnActionTriggered;
            laneAction.canceled += OnActionCanceled;
        }
    }

    // Wrapper to match InputAction signature
    private void OnActionTriggered(InputAction.CallbackContext context) => OnPress();
    private void OnActionCanceled(InputAction.CallbackContext context) => OnRelease();

    public void SpawnNote(float duration, float noteTime, GameplayManager manager)
    {
        if (notePrefab == null) return;
        
        GameObject newNoteObj = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity, transform.parent);
        newNoteObj.transform.SetAsFirstSibling(); 
        NoteObject newNote = newNoteObj.GetComponent<NoteObject>();

        if (newNote != null)
        {
            double effectiveStartTime = noteTime - manager.spawnOffset;
            float receptorY = 0f;
            if (receptorImage != null) receptorY = receptorImage.rectTransform.anchoredPosition.y;
            else receptorY = transform.localPosition.y;
            float missY = receptorY + mehThreshold; 

            newNote.Init(effectiveStartTime, noteSpeed, manager, scoreManager, missY); 
            newNote.SetRotation(transform.rotation); 
            newNote.SetupHold(duration, noteSpeed);
            newNote.SetColor(targetColor);
        }
    }

    private void OnPress()
    {
        transform.localScale = Vector3.one * 1.7f; 

        // Ignore input if it happened too fast (debounce)
        if (Time.time - lastPressTime < inputCooldown) return;
        lastPressTime = Time.time;

        transform.localScale = Vector3.one * 1.7f;
        if (activeNotes.Count == 0) return;

        NoteObject targetNote = activeNotes[0];
        float distance = Mathf.Abs(targetNote.transform.position.y - transform.position.y);

        if (distance < mehThreshold)
        {
            if (sfxSource != null && hitSound != null) sfxSource.PlayOneShot(hitSound, hitVolume);

            string judgment = "MEH"; 
            if (distance < goodThreshold) judgment = "GOOD";
            if (distance < perfectThreshold) judgment = "PERFECT";

            if (scoreManager != null) scoreManager.RegisterHit(judgment);

            PlayHitEffect();

            if (charAnimator != null && !string.IsNullOrEmpty(laneDirectionName))
            {
                charAnimator.TriggerAnimation(laneDirectionName);
            }

            if (targetNote.holdDuration <= 0)
            {
                DestroyNote(targetNote);
            }
            else
            {
                currentHoldNote = targetNote;
                currentHoldNote.isBeingHeld = true;
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
            currentHoldNote.isBeingHeld = false;
            currentHoldNote.transform.SetParent(transform.parent, true); 
            if (currentHoldNote.TryGetComponent(out Image img)) img.color = Color.gray;
            currentHoldNote = null;
            if (holdFlashObject != null) holdFlashObject.SetActive(false);
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

    void Update()
    {
        if (currentHoldNote == null)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);
            if (holdFlashObject != null && holdFlashObject.activeSelf) holdFlashObject.SetActive(false);
            return; 
        }

        if (currentHoldNote.gameObject == null)
        {
            currentHoldNote = null;
            if (holdFlashObject != null) holdFlashObject.SetActive(false);
            return;
        }

        if (holdFlashObject != null) holdFlashObject.transform.Rotate(0, 0, 300 * Time.deltaTime);
        transform.localScale = Vector3.one * 1.1f;

        if (scoreManager != null)
        {
            holdScoreTimer += Time.deltaTime;
            if (holdScoreTimer >= 0.2f) 
            {
                holdScoreTimer = 0f;
                scoreManager.AddScore(scoreManager.scorePerHoldTick);
            }
        }

        float tailY_Actual = currentHoldNote.transform.position.y - (currentHoldNote.holdDuration * noteSpeed);
        if (tailY_Actual > transform.position.y)
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

    private void DestroyNote(NoteObject note) { activeNotes.Remove(note); Destroy(note.gameObject); }
    void OnTriggerEnter2D(Collider2D other) { if (other.TryGetComponent(out NoteObject note)) { activeNotes.Add(note); note.canBeHit = true; } }
    void OnTriggerExit2D(Collider2D other) { if (other.TryGetComponent(out NoteObject note)) { activeNotes.Remove(note); note.canBeHit = false; } }
    
    // Clean up listeners when the lane is disabled or destroyed
    void OnDisable() 
    { 
        if (laneAction != null) 
        { 
            laneAction.performed -= OnActionTriggered;
            laneAction.canceled -= OnActionCanceled;
        } 
    }
}