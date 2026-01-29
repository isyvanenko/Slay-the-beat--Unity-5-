using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerScoreManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI judgmentText; 
    public TextMeshProUGUI comboText;    
    public TextMeshProUGUI scoreText;    

    [Header("Scoring Values")]
    public int scorePerPerfect = 100;
    public int scorePerGood = 50;
    public int scorePerMeh = 10;         
    public int scorePerHoldTick = 10;    
    public int scorePerHoldFinish = 200; 

    [Header("Visual Settings")]
    public float baseTextScale = 2.0f; // <--- NEW! Default size (1.0 = Normal, 2.0 = Double)
    public float popSpeed = 0.1f;      // How fast it scales up
    public float stayTime = 0.2f;      // How long it stays solid
    public float fadeTime = 0.5f;      // How long it takes to fade out
    public float rainbowSpeed = 2.0f; 

    // Internal State
    public int currentScore = 0; 
    private int currentCombo = 0;
    
    private Coroutine judgmentRoutine;
    private Coroutine comboRoutine;
    private Vector3 originalComboScale;
    
    // Default Colors
    private Color colorGood = Color.green;
    private Color colorMeh = Color.yellow; 
    private Color colorMiss = Color.red;

    void Start()
    {
        if (comboText) originalComboScale = comboText.transform.localScale;
        
        // Start hidden
        if (judgmentText) { judgmentText.text = ""; judgmentText.alpha = 0; }
        if (comboText) { comboText.text = ""; comboText.alpha = 0; }
        
        UpdateScoreDisplay();
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreDisplay();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText) scoreText.text = currentScore.ToString("D8");
    }

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

    // --- ANIMATION WITH CUSTOM SIZE ---
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

    void ApplyRainbowColor()
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f); 
        judgmentText.color = Color.HSVToRGB(hue, 1f, 1f); 
    }

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