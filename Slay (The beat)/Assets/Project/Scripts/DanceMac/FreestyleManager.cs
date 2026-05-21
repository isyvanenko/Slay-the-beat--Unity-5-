using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

/// <summary>
/// Core gameplay manager for freestyle rhythm game mode with beat detection and scoring.
/// </summary>
/// <remarks>
/// This class handles the complete freestyle gameplay experience including:
/// - Chart parsing and beat timeline management
/// - Real-time input processing with timing windows
/// - Score calculation with combo and style multipliers
/// - Visual feedback (pulse effects, judgment text, hit arrows)
/// - Two-player support with separate input devices
/// - Rainbow text effects for perfect judgments
/// 
/// The system uses audio DSP time for precise synchronization between music and gameplay.
/// </remarks>
[RequireComponent(typeof(AudioSource))]
public class FreestyleManager : MonoBehaviour
{
    [Header("Freestyle Settings")]
    /// <summary>Delay in seconds before music starts, allowing players to prepare.</summary>
    public float startDelay = 2.0f;

    [Header("Timing Windows (Seconds)")]
    /// <summary>Perfect timing window (±0.06 seconds) - Tightened for pro feel.</summary>
    public float perfectWindow = 0.06f;

    /// <summary>Good timing window (±0.12 seconds) - Decent accuracy.</summary>
    public float goodWindow = 0.12f;

    /// <summary>Meh timing window (±0.20 seconds) - Barely acceptable.</summary>
    public float mehWindow = 0.20f;

    [Header("Scoring Settings")]
    /// <summary>Base points awarded for PERFECT judgments.</summary>
    public int perfectBasePoints = 10219;

    /// <summary>Base points awarded for GOOD judgments.</summary>
    public int goodBasePoints = 5322;

    /// <summary>Base points awarded for MEH judgments.</summary>
    public int mehBasePoints = 1101;

    [Header("Visualizer Settings")]
    /// <summary>Maximum scale factor for beat pulse effects.</summary>
    public float maxPulseScale = 1.5f;

    /// <summary>Time in seconds to anticipate upcoming beats for pulse effect.</summary>
    public float pulseAnticipation = 0.5f;

    /// <summary>Time in seconds for pulse effect to decay after a beat.</summary>
    public float pulseDecayTime = 0.3f;

    [Header("Animation Settings")]
    /// <summary>Base scale factor for judgment text pop effect.</summary>
    public float baseTextScale = 2.0f;

    /// <summary>Duration of the pop animation in seconds.</summary>
    public float popSpeed = 0.1f;

    /// <summary>Time in seconds judgment text stays visible before fading.</summary>
    public float stayTime = 0.2f;

    /// <summary>Duration of fade-out animation in seconds.</summary>
    public float fadeTime = 0.5f;

    /// <summary>Speed of rainbow color cycling for perfect judgments.</summary>
    public float rainbowSpeed = 2.0f;

    [Header("Arrow Sprite (MUST BE LEFT-FACING)")]
    /// <summary>Sprite used for hit arrow displays. Must be left-facing by default.</summary>
    public Sprite arrowSprite;

    [Header("Player 1 Setup")]
    /// <summary>Player 1's input controller.</summary>
    public PlayerInput p1Input;

    /// <summary>UI Text component for Player 1's score display.</summary>
    public TextMeshProUGUI p1ScoreText;

    /// <summary>UI Text component for Player 1's combo display.</summary>
    public TextMeshProUGUI p1ComboText;

    /// <summary>UI Text component for Player 1's judgment messages.</summary>
    public TextMeshProUGUI p1JudgmentText;

    /// <summary>Transform for Player 1's beat pulse circle.</summary>
    public Transform p1BeatCircle;

    /// <summary>Image component for Player 1's beat circle coloring.</summary>
    public Image p1BeatCircleImage;

    /// <summary>Display for Player 1's directional arrow hits.</summary>
    public Image p1HitArrowDisplay;

    [Header("Player 2 Setup")]
    /// <summary>UI Panel container for Player 2's interface.</summary>
    public GameObject p2Panel;

    /// <summary>Player 2's input controller.</summary>
    public PlayerInput p2Input;

    /// <summary>UI Text component for Player 2's score display.</summary>
    public TextMeshProUGUI p2ScoreText;

    /// <summary>UI Text component for Player 2's combo display.</summary>
    public TextMeshProUGUI p2ComboText;

    /// <summary>UI Text component for Player 2's judgment messages.</summary>
    public TextMeshProUGUI p2JudgmentText;

    /// <summary>Transform for Player 2's beat pulse circle.</summary>
    public Transform p2BeatCircle;

    /// <summary>Image component for Player 2's beat circle coloring.</summary>
    public Image p2BeatCircleImage;

    /// <summary>Display for Player 2's directional arrow hits.</summary>
    public Image p2HitArrowDisplay;

    /// <summary>List of beat timestamps parsed from the chart file.</summary>
    private List<float> beatTimestamps = new List<float>();

    /// <summary>AudioSource component for music playback.</summary>
    private AudioSource musicSource;

    /// <summary>DSP time when the song should start playing.</summary>
    private double dspSongStartTime;

    /// <summary>Flag indicating if gameplay has started.</summary>
    private bool hasStarted = false;

    /// <summary>Player 1's current score.</summary>
    private int p1Score = 0;

    /// <summary>Player 1's current combo count.</summary>
    private int p1Combo = 0;

    /// <summary>Player 1's maximum combo achieved.</summary>
    private int p1MaxCombo = 0;

    /// <summary>Player 1's current style multiplier (consecutive unique arrows).</summary>
    private int p1Style = 1;

    /// <summary>Player 2's current score.</summary>
    private int p2Score = 0;

    /// <summary>Player 2's current combo count.</summary>
    private int p2Combo = 0;

    /// <summary>Player 2's maximum combo achieved.</summary>
    private int p2MaxCombo = 0;

    /// <summary>Player 2's current style multiplier.</summary>
    private int p2Style = 1;

    /// <summary>Last arrow pressed by Player 1 (for style calculation).</summary>
    private string p1LastArrow = "";

    /// <summary>Last arrow pressed by Player 2 (for style calculation).</summary>
    private string p2LastArrow = "";

    /// <summary>Set of beat indices already hit by Player 1.</summary>
    private HashSet<int> p1HitIndices = new HashSet<int>();

    /// <summary>Set of beat indices already hit by Player 2.</summary>
    private HashSet<int> p2HitIndices = new HashSet<int>();

    /// <summary>Timestamp of Player 1's last input for debouncing.</summary>
    private float p1LastInputTime = 0f;

    /// <summary>Timestamp of Player 2's last input for debouncing.</summary>
    private float p2LastInputTime = 0f;

    /// <summary>Debounce cooldown to prevent multiple rapid inputs.</summary>
    private float debounceCooldown = 0.05f;

    /// <summary>Coroutine references for animation management.</summary>
    private Coroutine p1JudgRoutine, p2JudgRoutine;
    private Coroutine p1ComboRoutine, p2ComboRoutine;
    private Coroutine p1ArrowRoutine, p2ArrowRoutine;

    /// <summary>Original scales for UI elements before animation.</summary>
    private Vector3 originalP1ComboScale, originalP2ComboScale;
    private Vector3 p1OriginalScale, p2OriginalScale;

    /// <summary>Original colors for beat circles.</summary>
    private Color p1OriginalColor, p2OriginalColor;

    /// <summary>Original scales for arrow displays.</summary>
    private Vector3 p1ArrowOriginalScale, p2ArrowOriginalScale;

    /// <summary>
    /// Initializes the freestyle manager, loads chart data, and sets up gameplay.
    /// </summary>
    /// <remarks>
    /// Setup sequence:
    /// 1. Cache original UI element properties (scales, colors)
    /// 2. Load and parse the selected song chart
    /// 3. Configure player input devices
    /// 4. Initialize UI displays and session data
    /// 5. Schedule music playback with start delay
    /// 6. Set timer to end session when song completes
    /// </remarks>
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

    /// <summary>
    /// Sets up player input devices and action bindings.
    /// </summary>
    /// <remarks>
    /// Configures input for both single-player and two-player modes:
    /// - Pairs physical devices with PlayerInput components
    /// - Enables action maps
    /// - Subscribes to directional input events
    /// - Activates/deactivates Player 2 UI based on session configuration
    /// </remarks>
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

    /// <summary>
    /// Parses a freestyle chart text file to extract beat timestamps.
    /// </summary>
    /// <param name="chart">TextAsset containing the chart data.</param>
    /// <remarks>
    /// Chart format expects one timestamp per line (in seconds).
    /// Blank lines and lines without valid floats are ignored.
    /// Timestamps are sorted chronologically after parsing.
    /// </remarks>
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

    /// <summary>
    /// Updates beat pulse visualization each frame.
    /// </summary>
    /// <remarks>
    /// Calculates pulse intensity based on:
    /// - Anticipation: Time until next beat (scales up as beat approaches)
    /// - Decay: Time since last beat (scales down after beat)
    /// 
    /// Applies pulse to both players' beat circles (if two-player mode active).
    /// Uses quadratic easing (Mathf.Pow) for smoother pulse feel.
    /// </remarks>
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

    /// <summary>
    /// Processes arrow input from players and calculates hit judgment.
    /// </summary>
    /// <param name="playerIndex">1 for Player 1, 2 for Player 2.</param>
    /// <param name="arrowDirection">Direction of the arrow pressed (Left, Right, Up, Down).</param>
    /// <remarks>
    /// Hit detection process:
    /// 1. Debounce check to prevent multiple rapid inputs
    /// 2. Find closest beat timestamp to current song time
    /// 3. Check if beat hasn't been hit before
    /// 4. Compare time difference against timing windows (Perfect, Good, Meh)
    /// 5. Calculate style multiplier and award points
    /// 6. Update combo, score, and UI
    /// </remarks>
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

    /// <summary>
    /// Displays a temporary visual indicator of the arrow hit.
    /// </summary>
    /// <param name="playerIndex">Target player (1 or 2).</param>
    /// <param name="arrowDirection">Direction to display.</param>
    /// <remarks>
    /// Rotates the arrow sprite based on direction:
    /// - Left: 0 degrees
    /// - Down: 90 degrees
    /// - Right: 180 degrees
    /// - Up: 270 degrees
    /// 
    /// The animation scales up and fades out the arrow display.
    /// </remarks>
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

    /// <summary>
    /// Calculates and updates the style multiplier based on arrow variety.
    /// </summary>
    /// <param name="playerIndex">Target player (1 or 2).</param>
    /// <param name="arrowDirection">Direction just pressed.</param>
    /// <remarks>
    /// Style mechanic rewards alternating arrow patterns:
    /// - If the new arrow is different from the last arrow, style multiplier increases
    /// - If the same arrow is pressed twice in a row, style resets to 1
    /// 
    /// This encourages players to vary their inputs for higher scores.
    /// </remarks>
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

    /// <summary>
    /// Registers a successful hit and updates all related game state.
    /// </summary>
    /// <param name="playerIndex">Player who hit (1 or 2).</param>
    /// <param name="judgmentType">Text display for judgment (PERFECT, GOOD, MEH).</param>
    /// <param name="points">Points awarded for this hit.</param>
    /// <param name="color">Color for judgment text.</param>
    /// <remarks>
    /// Updates:
    /// - Player score and combo
    /// - Session data for results screen
    /// - Triggers UI animations (judgment pop, combo pulse)
    /// - Displays style multiplier if > 1
    /// </remarks>
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

    /// <summary>
    /// Animates the hit arrow display with scale-up and fade-out effects.
    /// </summary>
    /// <param name="arrowImg">Image component to animate.</param>
    /// <param name="baseCircleColor">Original color for transparency blending.</param>
    /// <param name="originalScale">Original scale to return to.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
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

    /// <summary>
    /// Animates judgment text with pop, hold, and fade phases.
    /// </summary>
    /// <param name="textObj">Text component to animate.</param>
    /// <param name="text">Text content to display.</param>
    /// <param name="baseColor">Base color for non-rainbow animations.</param>
    /// <param name="isRainbow">If true, applies rainbow color cycling.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
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

    /// <summary>
    /// Applies a cycling rainbow color effect to text.
    /// </summary>
    /// <param name="textObj">Text component to recolor.</param>
    /// <remarks>Uses HSV color space cycling based on Time.time and rainbowSpeed.</remarks>
    void ApplyRainbowColor(TextMeshProUGUI textObj)
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
        textObj.color = Color.HSVToRGB(hue, 1f, 1f);
    }

    /// <summary>
    /// Animates the combo counter with a quick scale pulse.
    /// </summary>
    /// <param name="comboObj">Text component to animate.</param>
    /// <param name="originalScale">Original scale to return to.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    IEnumerator PulseCombo(TextMeshProUGUI comboObj, Vector3 originalScale)
    {
        comboObj.transform.localScale = originalScale * 1.5f;
        float t = 0;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            comboObj.transform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, t / 0.1f);
            yield return null;
        }
        comboObj.transform.localScale = originalScale;
    }

    /// <summary>
    /// Updates UI text displays for score and combo.
    /// </summary>
    /// <remarks>Score is formatted with 8-digit padding (e.g., 00012345).</remarks>
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

    /// <summary>
    /// Ends the freestyle session and transitions to results scene.
    /// </summary>
    private void EndFreestyle()
    {
        TransitionManager.Instance.LoadScene("ResultsScene");
    }
}