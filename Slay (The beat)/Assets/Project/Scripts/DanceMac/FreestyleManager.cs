using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; 
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class FreestyleManager : MonoBehaviour
{
    [Header("Freestyle Settings")]
    public float startDelay = 2.0f;
    
    [Header("Timing Windows (Seconds)")]
    public float perfectWindow = 0.06f; // Tightened for a more pro feel!
    public float goodWindow = 0.12f;
    public float mehWindow = 0.20f; 

    [Header("Scoring Settings")]
    public int perfectBasePoints = 10219
; // <--- NEW: Huge points!
    public int goodBasePoints = 5322;
    public int mehBasePoints = 1101;

    [Header("Visualizer Settings")]
    public float maxPulseScale = 1.5f; 
    public float pulseAnticipation = 0.5f; 
    public float pulseDecayTime = 0.3f;    

    [Header("Animation Settings")]
    public float baseTextScale = 2.0f; 
    public float popSpeed = 0.1f;      
    public float stayTime = 0.2f;      
    public float fadeTime = 0.5f;      
    public float rainbowSpeed = 2.0f; 

    [Header("Arrow Sprite (MUST BE LEFT-FACING)")]
    public Sprite arrowSprite;

    [Header("Player 1 Setup")]
    public PlayerInput p1Input;
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p1ComboText;
    public TextMeshProUGUI p1JudgmentText;
    public Transform p1BeatCircle;     
    public Image p1BeatCircleImage;    
    public Image p1HitArrowDisplay; 

    [Header("Player 2 Setup")]
    public GameObject p2Panel; 
    public PlayerInput p2Input;
    public TextMeshProUGUI p2ScoreText;
    public TextMeshProUGUI p2ComboText;
    public TextMeshProUGUI p2JudgmentText;
    public Transform p2BeatCircle;     
    public Image p2BeatCircleImage;    
    public Image p2HitArrowDisplay; 

    private List<float> beatTimestamps = new List<float>();

    private AudioSource musicSource;
    private double dspSongStartTime;
    private bool hasStarted = false;
    
    private int p1Score = 0, p1Combo = 0, p1MaxCombo = 0, p1Style = 1;
    private int p2Score = 0, p2Combo = 0, p2MaxCombo = 0, p2Style = 1;
    private string p1LastArrow = "", p2LastArrow = "";

    private HashSet<int> p1HitIndices = new HashSet<int>();
    private HashSet<int> p2HitIndices = new HashSet<int>();

    private float p1LastInputTime = 0f;
    private float p2LastInputTime = 0f;
    private float debounceCooldown = 0.05f; 

    private Coroutine p1JudgRoutine, p2JudgRoutine;
    private Coroutine p1ComboRoutine, p2ComboRoutine;
    private Coroutine p1ArrowRoutine, p2ArrowRoutine; 

    private Vector3 originalP1ComboScale, originalP2ComboScale;
    private Vector3 p1OriginalScale, p2OriginalScale;
    private Color p1OriginalColor, p2OriginalColor;
    private Vector3 p1ArrowOriginalScale, p2ArrowOriginalScale; 

    void Start()
    {
        musicSource = GetComponent<AudioSource>();
        
        if (p1ComboText) originalP1ComboScale = p1ComboText.transform.localScale;
        if (p2ComboText) originalP2ComboScale = p2ComboText.transform.localScale;

        if (p1BeatCircle != null) p1OriginalScale = p1BeatCircle.localScale;
        if (p1BeatCircleImage != null) p1OriginalColor = p1BeatCircleImage.color;
        if (p1HitArrowDisplay != null) 
        {
            p1ArrowOriginalScale = p1HitArrowDisplay.rectTransform.localScale;
            p1HitArrowDisplay.gameObject.SetActive(false); 
        }

        if (p2BeatCircle != null) p2OriginalScale = p2BeatCircle.localScale;
        if (p2BeatCircleImage != null) p2OriginalColor = p2BeatCircleImage.color;
        if (p2HitArrowDisplay != null) 
        {
            p2ArrowOriginalScale = p2HitArrowDisplay.rectTransform.localScale;
            p2HitArrowDisplay.gameObject.SetActive(false);
        }

        if (GameDataBridge.SelectedSong != null)
        {
            TextAsset chartToLoad = GameDataBridge.SelectedSong.GetChart(GameDataBridge.SelectedDifficulty);
            ParseFreestyleChart(chartToLoad);
        }

        SetupPlayerInputs();

        if (p1JudgmentText) { p1JudgmentText.text = ""; p1JudgmentText.alpha = 0; }
        if (p2JudgmentText) { p2JudgmentText.text = ""; p2JudgmentText.alpha = 0; }
        
        GameSessionData.P1Score = 0; GameSessionData.P1MaxCombo = 0;
        GameSessionData.P2Score = 0; GameSessionData.P2MaxCombo = 0;
        
        UpdateUI();

        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.PlayScheduled(dspSongStartTime);
            hasStarted = true;
            
            float totalTime = startDelay + musicSource.clip.length;
            Invoke("EndFreestyle", totalTime);
        }
    }

    void SetupPlayerInputs()
    {
        if (p1Input != null && SessionConfig.Player1Device != null)
        {
            p1Input.user.UnpairDevices(); 
            InputUser.PerformPairingWithDevice(SessionConfig.Player1Device, p1Input.user);
            p1Input.actions.Enable(); 
            
            p1Input.actions["Left"].performed += ctx => OnArrowInput(1, "Left");
            p1Input.actions["Down"].performed += ctx => OnArrowInput(1, "Down");
            p1Input.actions["Up"].performed += ctx => OnArrowInput(1, "Up");
            p1Input.actions["Right"].performed += ctx => OnArrowInput(1, "Right");
        }

        if (SessionConfig.PlayerCount == 2)
        {
            if (p2Input != null && SessionConfig.Player2Device != null)
            {
                if (p2Panel != null) p2Panel.SetActive(true);
                p2Input.gameObject.SetActive(true); 
                p2Input.user.UnpairDevices();
                InputUser.PerformPairingWithDevice(SessionConfig.Player2Device, p2Input.user);
                p2Input.actions.Enable(); 

                p2Input.actions["Left"].performed += ctx => OnArrowInput(2, "Left");
                p2Input.actions["Down"].performed += ctx => OnArrowInput(2, "Down");
                p2Input.actions["Up"].performed += ctx => OnArrowInput(2, "Up");
                p2Input.actions["Right"].performed += ctx => OnArrowInput(2, "Right");
            }
        }
        else
        {
            if (p2Panel != null) p2Panel.SetActive(false);
            if (p2Input != null) p2Input.gameObject.SetActive(false);
            GameSessionData.IsTwoPlayer = false; 
        }
    }

    private void ParseFreestyleChart(TextAsset chart)
    {
        if (chart == null) return;

        string[] lines = chart.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;

            string[] parts = trimmedLine.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float timestamp))
                {
                    if (!beatTimestamps.Contains(timestamp)) beatTimestamps.Add(timestamp);
                }
            }
        }
        beatTimestamps.Sort();
    }

    void Update()
    {
        if (!hasStarted || beatTimestamps.Count == 0) return;

        double currentSongTime = AudioSettings.dspTime - dspSongStartTime;
        if (currentSongTime < 0) return; 

        float nextBeatTime = -1f;
        float lastBeatTime = -1f;

        for (int i = 0; i < beatTimestamps.Count; i++)
        {
            if (beatTimestamps[i] > currentSongTime)
            {
                nextBeatTime = beatTimestamps[i];
                if (i > 0) lastBeatTime = beatTimestamps[i - 1];
                break;
            }
        }
        
        if (nextBeatTime < 0 && beatTimestamps.Count > 0) 
        {
            lastBeatTime = beatTimestamps[beatTimestamps.Count - 1];
        }

        float anticipationPulse = 0f;
        float decayPulse = 0f;

        if (nextBeatTime >= 0) 
        {
            float timeUntilBeat = nextBeatTime - (float)currentSongTime;
            if (timeUntilBeat <= pulseAnticipation)
            {
                anticipationPulse = 1f - (timeUntilBeat / pulseAnticipation);
                anticipationPulse = Mathf.Pow(anticipationPulse, 2f); 
            }
        }

        if (lastBeatTime >= 0)
        {
            float timeSinceLastBeat = (float)currentSongTime - lastBeatTime;
            if (timeSinceLastBeat <= pulseDecayTime)
            {
                decayPulse = 1f - (timeSinceLastBeat / pulseDecayTime);
                decayPulse = Mathf.Pow(decayPulse, 2f); 
            }
        }

        float pulseAmount = Mathf.Max(anticipationPulse, decayPulse);

        if (p1BeatCircle != null && p1BeatCircleImage != null)
        {
            p1BeatCircle.localScale = Vector3.Lerp(p1OriginalScale, p1OriginalScale * maxPulseScale, pulseAmount);
            Color p1PulseWhite = new Color(1f, 1f, 1f, p1OriginalColor.a);
            p1BeatCircleImage.color = Color.Lerp(p1OriginalColor, p1PulseWhite, pulseAmount);
        }

        if (GameSessionData.IsTwoPlayer && p2BeatCircle != null && p2BeatCircleImage != null)
        {
            p2BeatCircle.localScale = Vector3.Lerp(p2OriginalScale, p2OriginalScale * maxPulseScale, pulseAmount);
            Color p2PulseWhite = new Color(1f, 1f, 1f, p2OriginalColor.a);
            p2BeatCircleImage.color = Color.Lerp(p2OriginalColor, p2PulseWhite, pulseAmount);
        }
    }

    public void OnArrowInput(int playerIndex, string arrowDirection)
    {
        if (playerIndex == 1)
        {
            if (Time.unscaledTime - p1LastInputTime < debounceCooldown) return;
            p1LastInputTime = Time.unscaledTime;
        }
        else
        {
            if (Time.unscaledTime - p2LastInputTime < debounceCooldown) return;
            p2LastInputTime = Time.unscaledTime;
        }

        if (beatTimestamps.Count == 0) return;

        double currentSongTime = AudioSettings.dspTime - dspSongStartTime;
        if (currentSongTime < 0) return; 

        ShowHitArrow(playerIndex, arrowDirection);

        float closestTimeDiff = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < beatTimestamps.Count; i++)
        {
            float diff = Mathf.Abs((float)currentSongTime - beatTimestamps[i]);
            if (diff < closestTimeDiff)
            {
                closestTimeDiff = diff;
                closestIndex = i;
            }
        }

        HashSet<int> hitIndices = (playerIndex == 1) ? p1HitIndices : p2HitIndices;

        if (hitIndices.Contains(closestIndex)) return;

        // --- UPDATED: Uses the massive base points assigned in the Inspector ---
        if (closestTimeDiff <= perfectWindow)
        {
            hitIndices.Add(closestIndex);
            CalculateStyle(playerIndex, arrowDirection);
            int mult = (playerIndex == 1) ? p1Style : p2Style;
            RegisterHit(playerIndex, "PERFECT", perfectBasePoints * mult, Color.cyan);
        }
        else if (closestTimeDiff <= goodWindow)
        {
            hitIndices.Add(closestIndex);
            CalculateStyle(playerIndex, arrowDirection);
            int mult = (playerIndex == 1) ? p1Style : p2Style;
            RegisterHit(playerIndex, "GOOD", goodBasePoints * mult, Color.green);
        }
        else if (closestTimeDiff <= mehWindow)
        {
            hitIndices.Add(closestIndex);
            CalculateStyle(playerIndex, arrowDirection);
            int mult = (playerIndex == 1) ? p1Style : p2Style;
            RegisterHit(playerIndex, "MEH", mehBasePoints * mult, Color.yellow);
        }
    }

    private void ShowHitArrow(int playerIndex, string arrowDirection)
    {
        float zRotation = 0f;
        switch (arrowDirection)
        {
            case "Left": zRotation = 0f; break;
            case "Down": zRotation = 90f; break;
            case "Right": zRotation = 180f; break;
            case "Up": zRotation = 270f; break;
        }

        if (playerIndex == 1 && p1HitArrowDisplay != null)
        {
            p1HitArrowDisplay.sprite = arrowSprite;
            p1HitArrowDisplay.rectTransform.localEulerAngles = new Vector3(0, 0, zRotation);
            
            if (p1ArrowRoutine != null) StopCoroutine(p1ArrowRoutine);
            p1ArrowRoutine = StartCoroutine(AnimateHitArrow(p1HitArrowDisplay, p1OriginalColor, p1ArrowOriginalScale));
        }
        else if (playerIndex == 2 && p2HitArrowDisplay != null)
        {
            p2HitArrowDisplay.sprite = arrowSprite;
            p2HitArrowDisplay.rectTransform.localEulerAngles = new Vector3(0, 0, zRotation);
            
            if (p2ArrowRoutine != null) StopCoroutine(p2ArrowRoutine);
            p2ArrowRoutine = StartCoroutine(AnimateHitArrow(p2HitArrowDisplay, p2OriginalColor, p2ArrowOriginalScale));
        }
    }

    private void CalculateStyle(int playerIndex, string arrowDirection)
    {
        if (playerIndex == 1)
        {
            if (arrowDirection != p1LastArrow) p1Style++; else p1Style = 1; 
            p1LastArrow = arrowDirection;
        }
        else
        {
            if (arrowDirection != p2LastArrow) p2Style++; else p2Style = 1; 
            p2LastArrow = arrowDirection;
        }
    }

    private void RegisterHit(int playerIndex, string judgmentType, int points, Color color)
    {
        int styleMult = 1;

        if (playerIndex == 1)
        {
            p1Score += points; p1Combo++; if (p1Combo > p1MaxCombo) p1MaxCombo = p1Combo;
            GameSessionData.P1Score = p1Score;
            GameSessionData.P1MaxCombo = p1MaxCombo;
            styleMult = p1Style;

            string fullText = judgmentType + (styleMult > 1 ? $"\n<size=50%>{styleMult}x STYLE!</size>" : "");
            
            if (p1ComboRoutine != null) StopCoroutine(p1ComboRoutine);
            if (p1ComboText) p1ComboRoutine = StartCoroutine(PulseCombo(p1ComboText, originalP1ComboScale));

            if (p1JudgRoutine != null) StopCoroutine(p1JudgRoutine);
            if (p1JudgmentText) p1JudgRoutine = StartCoroutine(AnimateJudgment(p1JudgmentText, fullText, color, judgmentType == "PERFECT"));
        }
        else
        {
            p2Score += points; p2Combo++; if (p2Combo > p2MaxCombo) p2MaxCombo = p2Combo;
            GameSessionData.P2Score = p2Score;
            GameSessionData.P2MaxCombo = p2MaxCombo;
            styleMult = p2Style;

            string fullText = judgmentType + (styleMult > 1 ? $"\n<size=50%>{styleMult}x STYLE!</size>" : "");

            if (p2ComboRoutine != null) StopCoroutine(p2ComboRoutine);
            if (p2ComboText) p2ComboRoutine = StartCoroutine(PulseCombo(p2ComboText, originalP2ComboScale));

            if (p2JudgRoutine != null) StopCoroutine(p2JudgRoutine);
            if (p2JudgmentText) p2JudgRoutine = StartCoroutine(AnimateJudgment(p2JudgmentText, fullText, color, judgmentType == "PERFECT"));
        }

        UpdateUI();
    }
    
    IEnumerator AnimateHitArrow(Image arrowImg, Color baseCircleColor, Vector3 originalScale)
    {
        arrowImg.gameObject.SetActive(true);

        Color opaqueColor = new Color(baseCircleColor.r, baseCircleColor.g, baseCircleColor.b, 1f);
        Color transparentColor = new Color(baseCircleColor.r, baseCircleColor.g, baseCircleColor.b, 0f);
        
        arrowImg.color = opaqueColor;

        float popDuration = 0.1f;
        float fadeDuration = 0.3f;
        float t = 0;

        Vector3 poppedScale = originalScale * maxPulseScale;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            arrowImg.rectTransform.localScale = Vector3.Lerp(originalScale, poppedScale, t / popDuration);
            yield return null;
        }

        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = t / fadeDuration;

            arrowImg.rectTransform.localScale = Vector3.Lerp(poppedScale, originalScale, progress);
            arrowImg.color = Color.Lerp(opaqueColor, transparentColor, progress);

            yield return null;
        }

        arrowImg.gameObject.SetActive(false);
    }

    IEnumerator AnimateJudgment(TextMeshProUGUI textObj, string text, Color baseColor, bool isRainbow)
    {
        textObj.text = text;
        textObj.alpha = 1f;
        textObj.transform.localScale = Vector3.zero;

        Vector3 targetScale = Vector3.one * baseTextScale;
        if (!isRainbow) textObj.color = baseColor;

        float timer = 0f;
        while (timer < popSpeed)
        {
            timer += Time.deltaTime;
            float progress = timer / popSpeed;
            float currentScaleVal = Mathf.Lerp(0f, baseTextScale * 1.2f, progress); 
            textObj.transform.localScale = Vector3.one * currentScaleVal;
            
            if (isRainbow) ApplyRainbowColor(textObj);
            yield return null;
        }
        
        textObj.transform.localScale = targetScale; 

        timer = 0f;
        while (timer < stayTime)
        {
            timer += Time.deltaTime;
            if (isRainbow) ApplyRainbowColor(textObj);
            yield return null;
        }

        timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
            
            if (isRainbow)
            {
                ApplyRainbowColor(textObj); 
                Color c = textObj.color;
                c.a = alpha;
                textObj.color = c;
            }
            else
            {
                textObj.alpha = alpha;
            }
            yield return null;
        }
        textObj.alpha = 0;
    }

    void ApplyRainbowColor(TextMeshProUGUI textObj)
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f); 
        textObj.color = Color.HSVToRGB(hue, 1f, 1f); 
    }

    IEnumerator PulseCombo(TextMeshProUGUI comboObj, Vector3 originalScale)
    {
        comboObj.transform.localScale = originalScale * 1.5f;
        float t = 0;
        while(t < 0.1f)
        {
            t += Time.deltaTime;
            comboObj.transform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, t / 0.1f);
            yield return null;
        }
        comboObj.transform.localScale = originalScale;
    }

    private void UpdateUI()
    {
        if (p1ScoreText) p1ScoreText.text = p1Score.ToString("D8");
        if (p1ComboText) p1ComboText.text = p1Combo > 0 ? p1Combo.ToString() : "";

        if (GameSessionData.IsTwoPlayer)
        {
            if (p2ScoreText) p2ScoreText.text = p2Score.ToString("D8");
            if (p2ComboText) p2ComboText.text = p2Combo > 0 ? p2Combo.ToString() : "";
        }
    }

    private void EndFreestyle()
    {
        TransitionManager.Instance.LoadScene("ResultsScene"); 
    }
}