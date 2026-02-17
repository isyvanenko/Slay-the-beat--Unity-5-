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

    // --- NEW: ANIMATION CONNECTION ---
    [Header("Character Animation")]
    public CharacterAnimationController charAnimator; // Drag the Player Character here
    public string laneDirectionName; // Set this to "Left", "Right", "Up", or "Down" in Inspector
    // ---------------------------------

    [Header("Visuals")]
    public Color targetColor = Color.white;
    public GameObject holdFlashObject; 
    public Transform holdMaskContainer; 

    [Header("Hit Logic")]
    public float perfectThreshold = 30f;
    public float goodThreshold = 90f;   
    public float mehThreshold = 160f;   

    private InputActionMap laneMap;
    private InputAction laneAction;
    private List<NoteObject> activeNotes = new List<NoteObject>();
    private NoteObject currentHoldNote = null;
    private Coroutine hitEffectCoroutine; 
    private float holdScoreTimer = 0f; 

    public void Setup(InputDevice device, string[] actionBindings)
    {
        laneMap = new InputActionMap("LaneMap");
        laneAction = laneMap.AddAction("Hit");

        foreach (string binding in actionBindings)
        {
            laneAction.AddBinding(binding);
        }

        if (device != null && !(device is Keyboard))
        {
            laneMap.devices = new InputDevice[] { device };
        }

        laneAction.performed += ctx => OnPress();
        laneAction.canceled += ctx => OnRelease();
        laneMap.Enable();
    }

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

            // --- NEW: TRIGGER CHARACTER ANIMATION ---
            if (charAnimator != null && !string.IsNullOrEmpty(laneDirectionName))
            {
                // This sends "Left" (or whatever you set) to the character script
                charAnimator.TriggerAnimation(laneDirectionName);
            }
            // ----------------------------------------

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
    void OnDisable() { if (laneMap != null) { laneMap.Disable(); laneMap.Dispose(); } }
}