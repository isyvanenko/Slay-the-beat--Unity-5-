using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Manages player scoring, combo tracking, and judgment display for a rhythm game player.
/// </summary>
/// <remarks>
/// This component handles all score-related logic for a single player, including:
/// - Point allocation based on judgment types (PERFECT, GOOD, MEH, MISS)
/// - Combo counting with visual feedback
/// - Hold note scoring (tick points and finish bonuses)
/// - Judgment text animation with pop, stay, and fade effects
/// - Rainbow color cycling for PERFECT judgments
/// - Combo counter pulse animation
/// 
/// The score manager works in conjunction with LaneController and NoteObject to provide
/// comprehensive scoring feedback. It supports both single-player and multiplayer scenarios.
/// </remarks>
public class PlayerScoreManager : MonoBehaviour
{
    [Header("UI References")]
    /// <summary>Text component for displaying judgment results (PERFECT, GOOD, etc.).</summary>
    public TextMeshProUGUI judgmentText; 
    
    /// <summary>Text component for displaying current combo count.</summary>
    public TextMeshProUGUI comboText;    
    
    /// <summary>Text component for displaying current score (8-digit format).</summary>
    public TextMeshProUGUI scoreText;    

    [Header("Scoring Values")]
    /// <summary>Points awarded for a PERFECT judgment.</summary>
    public int scorePerPerfect = 100;
    
    /// <summary>Points awarded for a GOOD judgment.</summary>
    public int scorePerGood = 50;
    
    /// <summary>Points awarded for a MEH judgment.</summary>
    public int scorePerMeh = 10;         
    
    /// <summary>Points awarded per tick while holding a hold note.</summary>
    public int scorePerHoldTick = 10;    
    
    /// <summary>Bonus points awarded for successfully completing a hold note.</summary>
    public int scorePerHoldFinish = 200; 
    
    /// <summary>Highest combo achieved during the session.</summary>
    public int maxCombo = 0;

    [Header("Visual Settings")]
    /// <summary>Base scale factor for judgment text pop animation.</summary>
    public float baseTextScale = 2.0f;
    
    /// <summary>Duration in seconds for the judgment text pop-in animation.</summary>
    public float popSpeed = 0.1f;
    
    /// <summary>Duration in seconds that judgment text stays fully visible.</summary>
    public float stayTime = 0.2f;
    
    /// <summary>Duration in seconds for judgment text fade-out animation.</summary>
    public float fadeTime = 0.5f;
    
    /// <summary>Speed of rainbow color cycling for PERFECT judgments (cycles per second).</summary>
    public float rainbowSpeed = 2.0f; 

    /// <summary>Player's current total score.</summary>
    public int currentScore = 0; 
    
    /// <summary>Player's current combo count (consecutive successful hits).</summary>
    private int currentCombo = 0;
    
    /// <summary>Coroutine reference for judgment text animation.</summary>
    private Coroutine judgmentRoutine;
    
    /// <summary>Coroutine reference for combo pulse animation.</summary>
    private Coroutine comboRoutine;
    
    /// <summary>Original scale of the combo text for animation reset.</summary>
    private Vector3 originalComboScale;
    
    /// <summary>Color for GOOD judgments.</summary>
    private Color colorGood = Color.green;
    
    /// <summary>Color for MEH judgments.</summary>
    private Color colorMeh = Color.yellow; 
    
    /// <summary>Color for MISS judgments.</summary>
    private Color colorMiss = Color.red;

    /// <summary>
    /// Initializes UI elements and caches original scales.
    /// </summary>
    /// <remarks>
    /// Setup includes:
    /// - Caching original combo text scale for animation reset
    /// - Clearing and hiding judgment text
    /// - Clearing combo display
    /// - Initializing score display to 0
    /// </remarks>
    void Start()
    {
        if (comboText) originalComboScale = comboText.transform.localScale;
        
        // Start hidden
        if (judgmentText) { judgmentText.text = ""; judgmentText.alpha = 0; }
        if (comboText) { comboText.text = ""; comboText.alpha = 0; }
        
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Adds points to the player's current score and updates the display.
    /// </summary>
    /// <param name="amount">Number of points to add.</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Updates the score UI display with 8-digit zero-padded formatting.
    /// </summary>
    /// <remarks>
    /// Uses "D8" format specifier to ensure scores always display as 8 digits
    /// (e.g., 00000123 instead of 123). This maintains consistent UI layout.
    /// </remarks>
    void UpdateScoreDisplay()
    {
        if (scoreText) scoreText.text = currentScore.ToString("D8");
    }

    /// <summary>
    /// Registers a hit judgment, updating score, combo, and visual feedback.
    /// </summary>
    /// <param name="judgmentName">Judgment type: "PERFECT", "GOOD", "MEH", or "MISS".</param>
    /// <remarks>
    /// Processing logic:
    /// - MISS: Resets combo to 0, hides combo display
    /// - Other judgments: Increments combo, updates max combo, adds appropriate points
    /// - Always triggers judgment text animation
    /// - PULSES combo display on successful hits
    /// 
    /// Note: "MEHH" appears to be a typo in the original code; likely meant "MEH".
    /// The judgment name should match exactly what's passed from LaneController.
    /// </remarks>
    public void RegisterHit(string judgmentName)
    {
        // 1. Logic
        if (judgmentName == "MISS")
        {
            currentCombo = 0;
            if (comboText) comboText.text = ""; 
        }
        else
        {
            currentCombo++;
            if (currentCombo > maxCombo) maxCombo = currentCombo;
            if (comboText) 
            {
                comboText.text = currentCombo.ToString();
                comboText.alpha = 1; 
                if(comboRoutine != null) StopCoroutine(comboRoutine);
                comboRoutine = StartCoroutine(PulseCombo());
            }

            // Scoring Logic
            if (judgmentName == "PERFECT") AddScore(scorePerPerfect);
            else if (judgmentName == "GOOD") AddScore(scorePerGood);
            else if (judgmentName == "MEHH") AddScore(scorePerMeh);
        }

        // 2. Visuals
        if (judgmentText)
        {
            if (judgmentRoutine != null) StopCoroutine(judgmentRoutine);
            judgmentRoutine = StartCoroutine(AnimateJudgment(judgmentName));
        }
    }

    /// <summary>
    /// Animates the judgment text with pop-in, hold, and fade-out effects.
    /// </summary>
    /// <param name="type">Judgment type determining color and rainbow effect.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Animation sequence:
    /// 1. Pop-in phase (popSpeed duration): Text scales from 0 to baseTextScale * 1.2
    /// 2. Settle: Text returns to exact baseTextScale
    /// 3. Stay phase (stayTime duration): Text remains visible
    /// 4. Fade-out phase (fadeTime duration): Text alpha fades to 0
    /// 
    /// Special effects:
    /// - PERFECT judgments: Rainbow color cycling throughout animation
    /// - Other judgments: Static colors (GREEN for GOOD, YELLOW for MEH, RED for MISS)
    /// 
    /// The overshoot (1.2x) creates a satisfying pop effect before settling.
    /// </remarks>
    IEnumerator AnimateJudgment(string type)
    {
        judgmentText.text = type;
        judgmentText.alpha = 1f;
        judgmentText.transform.localScale = Vector3.zero;

        // Set Target Vector based on your new variable
        Vector3 targetScale = Vector3.one * baseTextScale;

        bool isRainbow = (type == "PERFECT");
        
        // Set Color if NOT rainbow
        if (!isRainbow)
        {
            if (type == "GOOD") judgmentText.color = colorGood;
            else if (type == "MEH") judgmentText.color = colorMeh;
            else if (type == "MISS") judgmentText.color = colorMiss;
        }

        // POP IN
        float timer = 0f;
        while (timer < popSpeed)
        {
            timer += Time.deltaTime;
            float progress = timer / popSpeed;
            
            // Logic: Go from 0 to (Target * 1.2) for the overshoot pop
            float currentScaleVal = Mathf.Lerp(0f, baseTextScale * 1.2f, progress); 
            judgmentText.transform.localScale = Vector3.one * currentScaleVal;
            
            if (isRainbow) ApplyRainbowColor();
            yield return null;
        }
        
        // Settle at target size
        judgmentText.transform.localScale = targetScale; 

        // STAY
        timer = 0f;
        while (timer < stayTime)
        {
            timer += Time.deltaTime;
            if (isRainbow) ApplyRainbowColor();
            yield return null;
        }

        // FADE OUT
        timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
            
            if (isRainbow)
            {
                ApplyRainbowColor(); 
                Color c = judgmentText.color;
                c.a = alpha;
                judgmentText.color = c;
            }
            else
            {
                judgmentText.alpha = alpha;
            }
            yield return null;
        }
        judgmentText.alpha = 0;
    }

    /// <summary>
    /// Applies a cycling rainbow color effect to the judgment text.
    /// </summary>
    /// <remarks>
    /// Uses HSV color space where hue cycles from 0 to 1 based on Time.time.
    /// Saturation and value are both set to 1 (fully saturated, fully bright).
    /// Rainbow speed determines how many full cycles occur per second.
    /// </remarks>
    void ApplyRainbowColor()
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f); 
        judgmentText.color = Color.HSVToRGB(hue, 1f, 1f); 
    }

    /// <summary>
    /// Animates the combo counter with a quick scale pulse effect.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Animation:
    /// - Quickly scales combo text to 1.5x original size
    /// - Returns to original scale over 0.1 seconds
    /// 
    /// This provides immediate visual feedback when combo increases,
    /// making the UI feel more responsive and satisfying.
    /// </remarks>
    IEnumerator PulseCombo()
    {
        comboText.transform.localScale = originalComboScale * 1.5f;
        float t = 0;
        while(t < 0.1f)
        {
            t += Time.deltaTime;
            comboText.transform.localScale = Vector3.Lerp(originalComboScale * 1.5f, originalComboScale, t/0.1f);
            yield return null;
        }
        comboText.transform.localScale = originalComboScale;
    }
}