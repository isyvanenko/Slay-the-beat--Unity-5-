using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the results screen after gameplay, displaying scores, combos, stars, and determining winners.
/// </summary>
/// <remarks>
/// This component orchestrates the complete results presentation sequence including:
/// - Animated score counting with rainbow effects
/// - Combo bonus calculation and display
/// - Star rating system based on final scores
/// - Winner determination and rainbow highlight effects
/// - Audio feedback for each stage (voice lines, counting sounds, star slams, applause)
/// - Support for both single-player and two-player modes
/// - Auto-progress to next scene with countdown slider
/// - Character silhouette display for the played song
/// 
/// The results sequence is fully scripted with coroutines to ensure proper timing
/// and synchronization with audio cues. Each step builds anticipation and provides
/// satisfying feedback for player achievements.
/// </remarks>
[RequireComponent(typeof(AudioSource))]
public class ResultsManager : MonoBehaviour
{
    [Header("Character Silhouettes")]
    /// <summary>Image component for Player 1's character silhouette.</summary>
    public Image p1Silhouette;
    
    /// <summary>Image component for Player 2's character silhouette.</summary>
    public Image p2Silhouette;

    [Header("Player 1 UI")]
    /// <summary>Panel container for Player 1's results UI.</summary>
    public GameObject p1Panel;
    
    /// <summary>Text component for Player 1's score display.</summary>
    public TextMeshProUGUI p1ScoreText;
    
    /// <summary>Text component for Player 1's max combo display.</summary>
    public TextMeshProUGUI p1ComboText; 
    
    /// <summary>List of star images for Player 1's rating (0-5 stars).</summary>
    public List<Image> p1Stars; 

    [Header("Player 2 UI")]
    /// <summary>Panel container for Player 2's results UI (two-player mode only).</summary>
    public GameObject p2Panel;
    
    /// <summary>Text component for Player 2's score display.</summary>
    public TextMeshProUGUI p2ScoreText;
    
    /// <summary>Text component for Player 2's max combo display.</summary>
    public TextMeshProUGUI p2ComboText; 
    
    /// <summary>List of star images for Player 2's rating (0-5 stars).</summary>
    public List<Image> p2Stars; 

    [Header("Winner UI (Optional)")]
    /// <summary>Text component for displaying winner announcement.</summary>
    public TextMeshProUGUI winnerText; 

    [Header("Sequence Settings")]
    /// <summary>Duration in seconds for counting number animations.</summary>
    public float countDuration = 3.0f; 
    
    /// <summary>Delay between star slam animations.</summary>
    public float starSlamDelay = 0.3f; 
    
    /// <summary>Delay before auto-transition to next scene (seconds).</summary>
    public float autoTransitionDelay = 15.0f; 
    
    /// <summary>Speed of rainbow color cycling effects.</summary>
    public float rainbowSpeed = 2.0f;

    [Header("Bonus Logic")]
    /// <summary>Points awarded per max combo point.</summary>
    public int pointsPerCombo = 1000; 
    
    /// <summary>Bonus points for Easy difficulty mode.</summary>
    public int easyModeBonus = 25000; 

    [Header("Audio Clips (Internal)")]
    /// <summary>Voice clip for Player 1's results announcement.</summary>
    public AudioClip voicePlayer1;
    
    /// <summary>Voice clip for Player 2's results announcement.</summary>
    public AudioClip voicePlayer2;
    
    /// <summary>Looping sound effect for score counting animation.</summary>
    public AudioClip scoreCountingLoop;
    
    /// <summary>Sound effect played when score counting completes.</summary>
    public AudioClip scoreFinished;
    
    /// <summary>Voice clip announcing combo results.</summary>
    public AudioClip comboVoice;
    
    /// <summary>Sound effect for star rating slam animation.</summary>
    public AudioClip starSlam;
    
    /// <summary>Applause sound effect for 5-star achievement.</summary>
    public AudioClip applause;
    
    /// <summary>Voice clip prompting to continue to next song.</summary>
    public AudioClip voiceNextSong;
    
    /// <summary>Thank you voice clip for final scene.</summary>
    public AudioClip voiceThankYou;

    [Header("Colors")]
    /// <summary>Gold color for earned stars.</summary>
    public Color goldColor = new Color(1f, 0.85f, 0f, 1f); 
    
    /// <summary>Default white color for UI elements.</summary>
    public Color defaultWhite = Color.white;

    [Header("Auto Progress UI")]
    /// <summary>Slider showing auto-transition countdown timer.</summary>
    public Slider progressSlider; 

    [Header("Scene Names")]
    /// <summary>Scene name for song selection screen.</summary>
    public string songSelectScene = "SongSelect";
    
    /// <summary>Scene name for final thank you screen.</summary>
    public string thankYouScene = "ThankYouForPlaying";

    /// <summary>Primary audio source for voice clips and one-shot sounds.</summary>
    private AudioSource mainAudioSource;
    
    /// <summary>Secondary looping audio source for counting sounds.</summary>
    private AudioSource loopAudioSource;

    /// <summary>Original scale of Player 1's score text for animation reset.</summary>
    private Vector3 p1ScoreScale, p1ComboScale;
    
    /// <summary>Original scales of Player 1's stars for animation reset.</summary>
    private List<Vector3> p1StarScales = new List<Vector3>();
    
    /// <summary>Original scale of Player 2's score text for animation reset.</summary>
    private Vector3 p2ScoreScale, p2ComboScale;
    
    /// <summary>Original scales of Player 2's stars for animation reset.</summary>
    private List<Vector3> p2StarScales = new List<Vector3>();

    /// <summary>Flag controlling winner silhouette rainbow effect.</summary>
    private bool showWinnerRainbow = false;
    
    /// <summary>Reference to the winning player's silhouette image.</summary>
    private Image winnerSilhouette;

    /// <summary>Flag for Player 1's score rainbow effect.</summary>
    private bool p1ScoreRainbow = false;
    
    /// <summary>Flag for Player 2's score rainbow effect.</summary>
    private bool p2ScoreRainbow = false;

    /// <summary>Player 1's final total score including bonuses.</summary>
    private int p1FinalTotalScore = 0;
    
    /// <summary>Player 2's final total score including bonuses.</summary>
    private int p2FinalTotalScore = 0;

    /// <summary>
    /// Initializes UI elements, caches original scales, and loads character sprites.
    /// </summary>
    /// <remarks>
    /// Setup includes:
    /// - Caching original scales for all animated UI elements
    /// - Hiding star images until earned
    /// - Loading character silhouettes from selected song data
    /// - Hiding progress slider initially
    /// - Hiding winner text initially
    /// </remarks>
    void Awake()
    {
        mainAudioSource = GetComponent<AudioSource>();
        
        p1ScoreScale = p1ScoreText.transform.localScale;
        p1ComboScale = p1ComboText.transform.localScale;
        foreach(Image s in p1Stars) {
            p1StarScales.Add(s.transform.localScale);
            s.gameObject.SetActive(false);
        }

        if (p2ScoreText != null) p2ScoreScale = p2ScoreText.transform.localScale;
        if (p2ComboText != null) p2ComboScale = p2ComboText.transform.localScale;
        if (p2Stars != null) {
            foreach(Image s in p2Stars) {
                p2StarScales.Add(s.transform.localScale);
                s.gameObject.SetActive(false);
            }
        }

        if (GameDataBridge.SelectedSong != null) {
            if(p1Silhouette != null) p1Silhouette.sprite = GameDataBridge.SelectedSong.p1CharacterSprite;
            if(p2Silhouette != null) p2Silhouette.sprite = GameDataBridge.SelectedSong.p2CharacterSprite;
        }

        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (winnerText != null) winnerText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Starts the results presentation sequence.
    /// </summary>
    /// <remarks>
    /// Initializes UI displays, calculates bonus scores based on combo and difficulty,
    /// and launches the main results coroutine.
    /// </remarks>
    void Start()
    {
        p1ScoreText.text = "00000000";
        p1ComboText.text = "";
        if (p2Panel != null) p2Panel.SetActive(GameSessionData.IsTwoPlayer);

        int diffBonus = (GameDataBridge.SelectedDifficulty == 0) ? easyModeBonus : 0;

        int p1Bonus = (GameSessionData.P1MaxCombo * pointsPerCombo) + diffBonus;
        p1FinalTotalScore = GameSessionData.P1Score + p1Bonus;

        int p2Bonus = (GameSessionData.P2MaxCombo * pointsPerCombo) + diffBonus;
        p2FinalTotalScore = GameSessionData.P2Score + p2Bonus;

        StartCoroutine(ResultsSequence());
    }

    /// <summary>
    /// Updates rainbow effects for winner silhouette and scores.
    /// </summary>
    void Update()
    {
        if (showWinnerRainbow && winnerSilhouette != null)
        {
            float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
            winnerSilhouette.color = Color.HSVToRGB(hue, 0.7f, 1f);
        }

        if (p1ScoreRainbow) ApplyRainbowToText(p1ScoreText);
        if (p2ScoreRainbow) ApplyRainbowToText(p2ScoreText);
    }

    /// <summary>
    /// Applies a cycling rainbow color effect to a text component.
    /// </summary>
    /// <param name="textObj">Text component to recolor.</param>
    void ApplyRainbowToText(TextMeshProUGUI textObj)
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
        textObj.color = Color.HSVToRGB(hue, 0.7f, 1f);
    }

    /// <summary>
    /// Main coroutine orchestrating the complete results presentation sequence.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// The results sequence follows this order:
    /// 1. Play results music and wait briefly
    /// 2. Player 1: Voice intro → Score counting → Combo display → Bonus counting → Star rating
    /// 3. Player 2 (if two-player): Same sequence as Player 1
    /// 4. Winner determination and rainbow highlights
    /// 5. Auto-progress countdown and scene transition
    /// 
    /// Each stage includes appropriate audio feedback and visual animations.
    /// </remarks>
    IEnumerator ResultsSequence()
    {
        if (MusicManager.Instance != null) {
            MusicManager.Instance.PlayResults(); 
            MusicManager.Instance.SetClub(1.0f);   
            MusicManager.Instance.SetQuiet(false);
        }

        yield return new WaitForSeconds(0.5f);

        // ==========================================
        // PLAYER 1 SEQUENCE
        // ==========================================
        PlayOneShot(voicePlayer1);
        yield return new WaitForSeconds(1.2f);

        StartLoop(scoreCountingLoop);
        yield return StartCoroutine(CountNumberRoutine(p1ScoreText, 0, GameSessionData.P1Score, "D8"));
        StopLoop();
        PlayOneShot(scoreFinished);
        yield return new WaitForSeconds(0.4f);

        PlayOneShot(comboVoice);
        p1ComboText.text = $"MAX COMBO: {GameSessionData.P1MaxCombo}";
        yield return StartCoroutine(PopText(p1ComboText.transform, p1ComboScale));
        yield return new WaitForSeconds(0.8f);

        if (p1FinalTotalScore > GameSessionData.P1Score)
        {
            StartLoop(scoreCountingLoop);
            yield return StartCoroutine(CountNumberRoutine(p1ScoreText, GameSessionData.P1Score, p1FinalTotalScore, "D8"));
            StopLoop();
            PlayOneShot(scoreFinished);
            yield return new WaitForSeconds(0.5f);
        }

        int p1StarsEarned = CalculateStars(p1FinalTotalScore);
        for (int i = 0; i < p1StarsEarned; i++) {
            PlayOneShot(starSlam);
            yield return StartCoroutine(SlamStar(p1Stars[i], p1StarScales[i]));
            yield return new WaitForSeconds(starSlamDelay);
        }
        if (p1StarsEarned == 5) PlayOneShot(applause);

        if (!GameSessionData.IsTwoPlayer)
        {
            yield return new WaitForSeconds(0.5f);
            winnerSilhouette = p1Silhouette; 
            showWinnerRainbow = true;        
            p1ScoreRainbow = true; 
        }

        // ==========================================
        // PLAYER 2 SEQUENCE
        // ==========================================
        if (GameSessionData.IsTwoPlayer)
        {
            yield return new WaitForSeconds(1.5f);
            PlayOneShot(voicePlayer2);
            yield return new WaitForSeconds(1.2f);

            StartLoop(scoreCountingLoop);
            yield return StartCoroutine(CountNumberRoutine(p2ScoreText, 0, GameSessionData.P2Score, "D8"));
            StopLoop();
            PlayOneShot(scoreFinished);

            PlayOneShot(comboVoice);
            p2ComboText.text = $"MAX COMBO: {GameSessionData.P2MaxCombo}";
            yield return StartCoroutine(PopText(p2ComboText.transform, p2ComboScale));
            yield return new WaitForSeconds(0.8f);

            if (p2FinalTotalScore > GameSessionData.P2Score)
            {
                StartLoop(scoreCountingLoop);
                yield return StartCoroutine(CountNumberRoutine(p2ScoreText, GameSessionData.P2Score, p2FinalTotalScore, "D8"));
                StopLoop();
                PlayOneShot(scoreFinished);
                yield return new WaitForSeconds(0.5f);
            }
            
            int p2StarsEarned = CalculateStars(p2FinalTotalScore);
            for (int i = 0; i < p2StarsEarned; i++) {
                PlayOneShot(starSlam);
                yield return StartCoroutine(SlamStar(p2Stars[i], p2StarScales[i]));
                yield return new WaitForSeconds(starSlamDelay);
            }
            if (p2StarsEarned == 5) PlayOneShot(applause);

            yield return new WaitForSeconds(0.5f);
            if (p1FinalTotalScore > p2FinalTotalScore) {
                p1ScoreRainbow = true;
                winnerSilhouette = p1Silhouette;
            } else if (p2FinalTotalScore > p1FinalTotalScore) {
                p2ScoreRainbow = true;
                winnerSilhouette = p2Silhouette;
            } else {
                p1ScoreRainbow = true;
                p2ScoreRainbow = true;
            }
            showWinnerRainbow = true; 
        }

        // --- WRAP UP ---
        if (MusicManager.Instance != null) {
            MusicManager.Instance.SetNormal(2.0f); 
        }

        yield return new WaitForSeconds(1.0f);

        // Always play "next song" voice since there's no stage limit
        PlayOneShot(voiceNextSong);

        if (progressSlider != null) {
            progressSlider.gameObject.SetActive(true);
            progressSlider.maxValue = autoTransitionDelay;
            float timeLeft = autoTransitionDelay;
            while (timeLeft > 0) {
                timeLeft -= Time.deltaTime;
                progressSlider.value = timeLeft;
                yield return null;
            }
        }
        AutoProgress();
    }

    /// <summary>Plays a one-shot audio clip.</summary>
    /// <param name="clip">Audio clip to play.</param>
    void PlayOneShot(AudioClip clip) { if(clip != null) mainAudioSource.PlayOneShot(clip); }
    
    /// <summary>Starts a looping audio clip for continuous sounds (like counting).</summary>
    /// <param name="clip">Audio clip to loop.</param>
    void StartLoop(AudioClip clip) {
        if (clip == null) return;
        if (loopAudioSource == null) {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.loop = true;
            loopAudioSource.volume = 0.6f;
        }
        loopAudioSource.clip = clip;
        loopAudioSource.Play();
    }
    
    /// <summary>Stops the currently playing looped audio.</summary>
    void StopLoop() { if(loopAudioSource != null) loopAudioSource.Stop(); }

    /// <summary>
    /// Coroutine that animates a number counting from start to target.
    /// </summary>
    /// <param name="textObj">Text component to update.</param>
    /// <param name="start">Starting number.</param>
    /// <param name="target">Target number to count to.</param>
    /// <param name="format">Format string ("D8" for 8-digit padding, otherwise custom prefix).</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Uses SmoothStep interpolation for easing and rainbow coloring during counting.
    /// Format "D8" produces 8-digit zero-padded numbers (e.g., 00000123).
    /// </remarks>
    IEnumerator CountNumberRoutine(TextMeshProUGUI textObj, int start, int target, string format)
    {
        float timer = 0;
        while (timer < countDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, timer / countDuration);
            int current = (int)Mathf.Lerp(start, target, progress);
            
            float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
            textObj.color = Color.HSVToRGB(hue, 0.7f, 1f);
            
            textObj.text = (format == "D8") ? current.ToString("D8") : format + current.ToString();
            yield return null;
        }
        textObj.color = defaultWhite;
        textObj.text = (format == "D8") ? target.ToString("D8") : format + target.ToString();
    }

    /// <summary>
    /// Animates a star slam effect (earned star rating).
    /// </summary>
    /// <param name="star">Star image to animate.</param>
    /// <param name="targetScale">Target scale for the star.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Animation phases:
    /// 1. Star scales down from 6x to target scale (overshoot slam effect)
    /// 2. Star pulses slightly (pulse effect for 0.2 seconds)
    /// 
    /// The star turns gold when earned and remains visible.
    /// </remarks>
    IEnumerator SlamStar(Image star, Vector3 targetScale)
    {
        star.gameObject.SetActive(true);
        star.color = goldColor; 
        float timer = 0;
        float dur = 0.25f;
        Vector3 startScale = targetScale * 6f;
        while (timer < dur)
        {
            timer += Time.deltaTime;
            star.transform.localScale = Vector3.Lerp(startScale, targetScale, timer / dur);
            yield return null;
        }
        float pulseTimer = 0;
        while (pulseTimer < 0.2f)
        {
            pulseTimer += Time.deltaTime;
            float pulse = 1f + Mathf.Sin((pulseTimer / 0.2f) * Mathf.PI) * 0.4f;
            star.transform.localScale = targetScale * pulse;
            yield return null;
        }
        star.transform.localScale = targetScale;
    }

    /// <summary>
    /// Calculates star rating based on final score using song grade thresholds.
    /// </summary>
    /// <param name="score">Final total score to evaluate.</param>
    /// <returns>Number of stars earned (0-5).</returns>
    int CalculateStars(int score)
    {
        if (GameDataBridge.SelectedSong == null) return 0;
        var g = GameDataBridge.SelectedSong;
        if (score >= g.fiveStars) return 5;
        if (score >= g.fourStars) return 4;
        if (score >= g.threeStars) return 3;
        if (score >= g.twoStars) return 2;
        if (score >= g.oneStar) return 1;
        return 0;
    }

    /// <summary>
    /// Animates a text pop effect (scale up then settle).
    /// </summary>
    /// <param name="t">Transform to animate.</param>
    /// <param name="targetScale">Target scale to settle at.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    IEnumerator PopText(Transform t, Vector3 targetScale)
    {
        float timer = 0;
        float dur = 0.2f;
        t.localScale = targetScale * 1.5f;
        while (timer < dur)
        {
            timer += Time.deltaTime;
            t.localScale = Vector3.Lerp(targetScale * 1.5f, targetScale, timer / dur);
            yield return null;
        }
        t.localScale = targetScale;
    }

    /// <summary>
    /// Forces the results sequence to continue immediately (called by UI button).
    /// </summary>
    public void ForceContinue()
    {
        StopAllCoroutines(); 
        AutoProgress();
    }

    /// <summary>
    /// Transitions to the next scene (song selection).
    /// </summary>
    void AutoProgress()
    {
        // Always go back to song selection after results (no stage system)
        TransitionManager.Instance.LoadScene(songSelectScene);
    }
}