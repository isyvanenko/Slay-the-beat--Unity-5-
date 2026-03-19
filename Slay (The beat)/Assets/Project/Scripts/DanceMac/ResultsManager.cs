using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ResultsManager : MonoBehaviour
{
    [Header("Character Silhouettes")]
    public Image p1Silhouette;
    public Image p2Silhouette;

    [Header("Player 1 UI")]
    public GameObject p1Panel;
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p1ComboText; 
    public List<Image> p1Stars; 

    [Header("Player 2 UI")]
    public GameObject p2Panel;
    public TextMeshProUGUI p2ScoreText;
    public TextMeshProUGUI p2ComboText; 
    public List<Image> p2Stars; 

    [Header("Winner UI")]
    public TextMeshProUGUI winnerText;

    [Header("Sequence Settings")]
    public float countDuration = 3.0f; 
    public float starSlamDelay = 0.3f; 
    public float autoTransitionDelay = 15.0f; 
    public float rainbowSpeed = 2.0f;

    [Header("Audio Clips (Internal)")]
    public AudioClip voicePlayer1;
    public AudioClip voicePlayer2;
    public AudioClip scoreCountingLoop;
    public AudioClip scoreFinished;
    public AudioClip comboVoice;
    public AudioClip starSlam;
    public AudioClip applause;
    public AudioClip voiceNextSong;
    public AudioClip voiceThankYou;

    [Header("Colors")]
    public Color goldColor = new Color(1f, 0.85f, 0f, 1f); 
    public Color defaultWhite = Color.white;

    [Header("Auto Progress UI")]
    public Slider progressSlider; 

    [Header("Scene Names")]
    public string songSelectScene = "SongSelect";
    public string thankYouScene = "ThankYouForPlaying";

    private AudioSource mainAudioSource;
    private AudioSource loopAudioSource;

    private Vector3 p1ScoreScale, p1ComboScale;
    private List<Vector3> p1StarScales = new List<Vector3>();
    private Vector3 p2ScoreScale, p2ComboScale;
    private List<Vector3> p2StarScales = new List<Vector3>();

    private bool showWinnerRainbow = false;
    private Image winnerSilhouette;

    void Awake()
    {
        mainAudioSource = GetComponent<AudioSource>();
        
        // Capture Scales
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

        // Setup Characters
        if (GameSessionData.CurrentSongGrades != null) {
            if(p1Silhouette != null) p1Silhouette.sprite = GameSessionData.CurrentSongGrades.p1CharacterSprite;
            if(p2Silhouette != null) p2Silhouette.sprite = GameSessionData.CurrentSongGrades.p2CharacterSprite;
        }

        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (winnerText != null) winnerText.gameObject.SetActive(false);
    }

    void Start()
    {
        p1ScoreText.text = "00000000";
        p1ComboText.text = "";
        if (p2Panel != null) p2Panel.SetActive(GameSessionData.IsTwoPlayer);

        StartCoroutine(ResultsSequence());
    }

    void Update()
    {
        if (showWinnerRainbow && winnerSilhouette != null)
        {
            float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
            winnerSilhouette.color = Color.HSVToRGB(hue, 0.7f, 1f);
        }
    }

    IEnumerator ResultsSequence()
    {
        // --- 1. SETUP MUSIC FOR RESULTS ---
        if (MusicManager.Instance != null) {
            MusicManager.Instance.PlayResults(); 
            MusicManager.Instance.SetClub(1.0f);   
            MusicManager.Instance.SetQuiet(false);
        }

        yield return new WaitForSeconds(0.5f);

        // --- PLAYER 1 REVEAL ---
        PlayOneShot(voicePlayer1);
        yield return new WaitForSeconds(1.2f);

        StartLoop(scoreCountingLoop);
        yield return StartCoroutine(CountNumberRoutine(p1ScoreText, 0, GameSessionData.P1Score, "D8"));
        StopLoop();
        PlayOneShot(scoreFinished);
        yield return new WaitForSeconds(0.4f);

        PlayOneShot(comboVoice);
        p1ComboText.text = "MAX COMBO: " + GameSessionData.P1MaxCombo;
        yield return StartCoroutine(PopText(p1ComboText.transform, p1ComboScale));
        yield return new WaitForSeconds(0.5f);

        int p1StarsEarned = CalculateStars(GameSessionData.P1Score);
        for (int i = 0; i < p1StarsEarned; i++) {
            PlayOneShot(starSlam);
            yield return StartCoroutine(SlamStar(p1Stars[i], p1StarScales[i]));
            yield return new WaitForSeconds(starSlamDelay);
        }
        if (p1StarsEarned == 5) PlayOneShot(applause);

        if (!GameSessionData.IsTwoPlayer && p1StarsEarned == 5)
        {
            yield return new WaitForSeconds(0.5f);
            winnerSilhouette = p1Silhouette; 
            showWinnerRainbow = true;        
            winnerText.gameObject.SetActive(true);
            winnerText.text = "PERFECT PERFORMANCE!";
            yield return StartCoroutine(PopText(winnerText.transform, winnerText.transform.localScale));
        }

        // --- PLAYER 2 REVEAL ---
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
            p2ComboText.text = "MAX COMBO: " + GameSessionData.P2MaxCombo;
            yield return StartCoroutine(PopText(p2ComboText.transform, p2ComboScale));
            
            int p2StarsEarned = CalculateStars(GameSessionData.P2Score);
            for (int i = 0; i < p2StarsEarned; i++) {
                PlayOneShot(starSlam);
                yield return StartCoroutine(SlamStar(p2Stars[i], p2StarScales[i]));
                yield return new WaitForSeconds(starSlamDelay);
            }
            if (p2StarsEarned == 5) PlayOneShot(applause);

            yield return new WaitForSeconds(0.5f);
            winnerText.gameObject.SetActive(true);
            if (GameSessionData.P1Score > GameSessionData.P2Score) {
                winnerText.text = "PLAYER 1 ATE!";
                winnerSilhouette = p1Silhouette;
            } else if (GameSessionData.P2Score > GameSessionData.P1Score) {
                winnerText.text = "PLAYER 2 ATE!";
                winnerSilhouette = p2Silhouette;
            } else {
                winnerText.text = "IT'S A DRAW!";
            }
            showWinnerRainbow = true; 
            yield return StartCoroutine(PopText(winnerText.transform, winnerText.transform.localScale));
        }

        // --- 2. SWITCH BACK TO NORMAL MUSIC ---
        if (MusicManager.Instance != null) {
            MusicManager.Instance.SetNormal(2.0f); 
        }

        yield return new WaitForSeconds(1.0f);

        // --- UPDATED: Uses SessionConfig Stage System ---
        if (SessionConfig.CurrentStage < SessionConfig.MaxStages)
            PlayOneShot(voiceNextSong);
        else
            PlayOneShot(voiceThankYou);

        // Transition Countdown
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

    // --- Audio Helpers ---
    void PlayOneShot(AudioClip clip) { if(clip != null) mainAudioSource.PlayOneShot(clip); }
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
    void StopLoop() { if(loopAudioSource != null) loopAudioSource.Stop(); }

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

    int CalculateStars(int score)
    {
        if (GameSessionData.CurrentSongGrades == null) return 0;
        var g = GameSessionData.CurrentSongGrades;
        if (score >= g.fiveStars) return 5;
        if (score >= g.fourStars) return 4;
        if (score >= g.threeStars) return 3;
        if (score >= g.twoStars) return 2;
        if (score >= g.oneStar) return 1;
        return 0;
    }

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

    // --- NEW: Attach this to your Continue UI Button! ---
    public void ForceContinue()
    {
        StopAllCoroutines(); 
        AutoProgress();
    }

    // --- UPDATED: Arcade Progression Logic ---
    void AutoProgress()
    {
        if (SessionConfig.CurrentStage < SessionConfig.MaxStages) 
        {
            // Add 1 to the stage, then go back to Song Select
            SessionConfig.CurrentStage++;
            TransitionManager.Instance.LoadScene(songSelectScene);
        } 
        else 
        {
            // End of playthrough!
            TransitionManager.Instance.LoadScene(thankYouScene);
        }
    }
}