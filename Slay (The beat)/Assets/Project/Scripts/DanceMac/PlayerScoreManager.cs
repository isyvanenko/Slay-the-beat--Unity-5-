using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Manages player scoring, combo tracking, and judgment display for a rhythm game player.
/// </summary>
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

    [Header("Sprite Spotlight Settings")]
    /// <summary>The sprite image that will flash for the spotlight effect.</summary>
    public Image spotlightSprite;
    
    /// <summary>Duration of the complete spotlight + text animation sequence.</summary>
    public float totalAnimationDuration = 0.8f;
    
    /// <summary>Percentage of total time spent on fade IN (0-1).</summary>
    [Range(0.1f, 0.4f)]
    public float fadeInRatio = 0.25f;
    
    /// <summary>Percentage of total time spent fully visible (0-1).</summary>
    [Range(0.1f, 0.4f)]
    public float stayRatio = 0.25f;
    
    /// <summary>Percentage of total time spent on fade OUT (0-1).</summary>
    [Range(0.1f, 0.6f)]
    public float fadeOutRatio = 0.5f;

    [Header("Add Sprite Settings")]
    /// <summary>The add sprite that scales up quickly on hit.</summary>
    public Image addSprite;
    
    /// <summary>Target X scale for the add sprite (default 0.5).</summary>
    public float addSpriteTargetScaleX = 0.5f;
    
    /// <summary>Duration for the add sprite to scale up.</summary>
    public float addSpriteScaleDuration = 0.1f;
    
    /// <summary>Easing curve for the add sprite scale animation.</summary>
    public AnimationCurve addSpriteScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Streak Lost Settings")]
    /// <summary>Image that appears when combo streak is lost.</summary>
    public Image streakLostImage;
    
    /// <summary>Duration for the streak lost image fade in.</summary>
    public float streakLostFadeInTime = 0.2f;
    
    /// <summary>Duration the streak lost image stays visible.</summary>
    public float streakLostStayTime = 0.6f;
    
    /// <summary>Duration for the streak lost image fade out.</summary>
    public float streakLostFadeOutTime = 0.2f;
    
    /// <summary>Audio clip to play when streak is lost.</summary>
    public AudioClip streakLostAudioClip;
    
    /// <summary>Volume for the streak lost audio.</summary>
    [Range(0f, 1f)]
    public float streakLostAudioVolume = 0.7f;
    
    /// <summary>Minimum combo required to show streak lost effect (avoid showing on every miss).</summary>
    public int minComboForStreakLostEffect = 3;

    [Header("Star Ranking Settings")]
    /// <summary>Permanent star images (1-5 stars) that appear briefly when earned.</summary>
    public Image star1Image;
    public Image star2Image;
    public Image star3Image;
    public Image star4Image;
    public Image star5Image;
    
    /// <summary>Popup star image that appears briefly when earning a star.</summary>
    public Image starPopupImage;
    
    /// <summary>Particle system for star effects.</summary>
    public ParticleSystem starParticleEffect;
    
    /// <summary>Duration for stars to fade in.</summary>
    public float starFadeInTime = 0.2f;
    
    /// <summary>Duration stars stay visible.</summary>
    public float starStayTime = 0.6f;
    
    /// <summary>Duration for stars to fade out.</summary>
    public float starFadeOutTime = 0.2f;
    
    /// <summary>Scale up amount for star effect.</summary>
    public float starScaleAmount = 1.3f;
    
    /// <summary>Gold color for the star flash effect.</summary>
    public Color goldFlashColor = new Color(1f, 0.84f, 0f, 1f);
    
    /// <summary>Audio clip to play when star ranking is achieved.</summary>
    public AudioClip starAchievedAudioClip;
    
    /// <summary>Volume for star achieved audio.</summary>
    [Range(0f, 1f)]
    public float starAchievedAudioVolume = 0.8f;

    [Header("Judgment Colors")]
    /// <summary>Color for PERFECT judgments (only used if rainbow is disabled).</summary>
    public Color perfectColor = new Color(1f, 0.84f, 0f); // Gold color
    
    /// <summary>Color for GOOD judgments.</summary>
    public Color goodColor = Color.green;
    
    /// <summary>Color for MEH judgments.</summary>
    public Color mehColor = Color.yellow;
    
    /// <summary>Color for MISS judgments.</summary>
    public Color missColor = Color.red;

    /// <summary>Player's current total score.</summary>
    public int currentScore = 0; 
    
    /// <summary>Player's current combo count (consecutive successful hits).</summary>
    private int currentCombo = 0;
    
    /// <summary>Coroutine reference for judgment text animation.</summary>
    private Coroutine judgmentRoutine;
    
    /// <summary>Coroutine reference for combo pulse animation.</summary>
    private Coroutine comboRoutine;
    
    /// <summary>Coroutine reference for add sprite animation.</summary>
    private Coroutine addSpriteRoutine;
    
    /// <summary>Coroutine reference for streak lost animation.</summary>
    private Coroutine streakLostRoutine;
    
    /// <summary>Coroutine reference for star animation.</summary>
    private Coroutine starAnimationRoutine;
    
    /// <summary>Original scale of the combo text for animation reset.</summary>
    private Vector3 originalComboScale;
    
    /// <summary>Original scale of the add sprite.</summary>
    private Vector3 originalAddSpriteScale;
    
    /// <summary>Original scales of star images.</summary>
    private Vector3[] originalStarScales;
    
    /// <summary>Original scale of the star popup image.</summary>
    private Vector3 originalStarPopupScale;
    
    /// <summary>Cache for current animation data to allow smooth overwrites.</summary>
    private AnimationData currentAnimation;
    
    /// <summary>Audio source for playing sounds.</summary>
    private AudioSource audioSource;
    
    /// <summary>Current highest star rank achieved.</summary>
    private int currentStarRank = 0;
    
    /// <summary>Reference to SongGradeData for star thresholds.</summary>
    private SongGradeData songGradeData;
    
    /// <summary>Flag to track if stars have been awarded.</summary>
    private bool[] starsAwarded = new bool[5];

    /// <summary>Stores animation timing and state data for smooth transitions.</summary>
    private class AnimationData
    {
        public float startTime;
        public float fadeInTime;
        public float stayTime;
        public float fadeOutTime;
        public string judgmentType;
        public bool isRainbow;
        public Color judgmentColor;
        
        public AnimationData(float duration, float fadeInRatio, float stayRatio, float fadeOutRatio, string type, Color color, bool rainbow)
        {
            fadeInTime = duration * fadeInRatio;
            stayTime = duration * stayRatio;
            fadeOutTime = duration * fadeOutRatio;
            judgmentType = type;
            judgmentColor = color;
            isRainbow = rainbow;
        }
        
        public float TotalTime => fadeInTime + stayTime + fadeOutTime;
    }

    /// <summary>
    /// Initializes UI elements and caches original scales.
    /// </summary>
    void Start()
    {
        if (comboText) originalComboScale = comboText.transform.localScale;
        if (addSprite) originalAddSpriteScale = addSprite.transform.localScale;
        if (starPopupImage) originalStarPopupScale = starPopupImage.transform.localScale;
        
        // Cache original star scales
        originalStarScales = new Vector3[5];
        if (star1Image) originalStarScales[0] = star1Image.transform.localScale;
        if (star2Image) originalStarScales[1] = star2Image.transform.localScale;
        if (star3Image) originalStarScales[2] = star3Image.transform.localScale;
        if (star4Image) originalStarScales[3] = star4Image.transform.localScale;
        if (star5Image) originalStarScales[4] = star5Image.transform.localScale;
        
        // Setup audio source
        if (streakLostAudioClip != null || starAchievedAudioClip != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Start hidden
        if (judgmentText) { judgmentText.text = ""; judgmentText.alpha = 0; }
        
        // Initialize combo text to "0" instead of empty
        if (comboText) 
        { 
            comboText.text = "0"; 
            comboText.alpha = 1;
        }
        
        if (spotlightSprite) 
        { 
            Color c = spotlightSprite.color;
            c.a = 0;
            spotlightSprite.color = c;
        }
        
        // Hide streak lost image
        if (streakLostImage)
        {
            Color c = streakLostImage.color;
            c.a = 0;
            streakLostImage.color = c;
        }
        
        // Hide star popup image
        if (starPopupImage)
        {
            Color c = starPopupImage.color;
            c.a = 0;
            starPopupImage.color = c;
            starPopupImage.transform.localScale = originalStarPopupScale;
        }
        
        // Initialize permanent stars - hide them initially
        InitializePermanentStars();
        
        // Reset add sprite scale to 0
        if (addSprite)
        {
            Vector3 scale = addSprite.transform.localScale;
            scale.x = 0;
            addSprite.transform.localScale = scale;
            Color c = addSprite.color;
            c.a = 1;
            addSprite.color = c;
        }
        
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Initializes permanent stars with alpha 0 and original scale.
    /// </summary>
    private void InitializePermanentStars()
    {
        if (star1Image) { Color c = star1Image.color; c.a = 0; star1Image.color = c; star1Image.transform.localScale = originalStarScales[0]; }
        if (star2Image) { Color c = star2Image.color; c.a = 0; star2Image.color = c; star2Image.transform.localScale = originalStarScales[1]; }
        if (star3Image) { Color c = star3Image.color; c.a = 0; star3Image.color = c; star3Image.transform.localScale = originalStarScales[2]; }
        if (star4Image) { Color c = star4Image.color; c.a = 0; star4Image.color = c; star4Image.transform.localScale = originalStarScales[3]; }
        if (star5Image) { Color c = star5Image.color; c.a = 0; star5Image.color = c; star5Image.transform.localScale = originalStarScales[4]; }
    }

    /// <summary>
    /// Sets the song grade data for star threshold checking.
    /// </summary>
    /// <param name="gradeData">SongGradeData containing score thresholds.</param>
    public void SetSongGradeData(SongGradeData gradeData)
    {
        songGradeData = gradeData;
        starsAwarded = new bool[5];
        currentStarRank = 0;
    }

    /// <summary>
    /// Checks and triggers star ranking animations if score thresholds are met.
    /// </summary>
    private void CheckAndAwardStars()
    {
        if (songGradeData == null) return;
        
        // Check each star threshold (1-5 stars)
        for (int i = 0; i < 5; i++)
        {
            if (!starsAwarded[i])
            {
                int threshold = GetStarThreshold(i + 1);
                if (currentScore >= threshold)
                {
                    AwardStar(i + 1);
                    starsAwarded[i] = true;
                    currentStarRank = i + 1;
                }
            }
        }
    }

    /// <summary>
    /// Gets the score threshold for a specific star rank.
    /// </summary>
    /// <param name="starCount">Number of stars (1-5).</param>
    /// <returns>Score required for that star rank.</returns>
    private int GetStarThreshold(int starCount)
    {
        if (songGradeData == null) return int.MaxValue;
        
        switch (starCount)
        {
            case 1: return songGradeData.oneStar;
            case 2: return songGradeData.twoStars;
            case 3: return songGradeData.threeStars;
            case 4: return songGradeData.fourStars;
            case 5: return songGradeData.fiveStars;
            default: return int.MaxValue;
        }
    }

    /// <summary>
    /// Gets the permanent star image for a specific rank.
    /// </summary>
    /// <param name="starCount">Number of stars.</param>
    /// <returns>The Image component for that star.</returns>
    private Image GetStarImage(int starCount)
    {
        switch (starCount)
        {
            case 1: return star1Image;
            case 2: return star2Image;
            case 3: return star3Image;
            case 4: return star4Image;
            case 5: return star5Image;
            default: return null;
        }
    }

    /// <summary>
    /// Gets the original scale for a specific star.
    /// </summary>
    /// <param name="starCount">Number of stars.</param>
    /// <returns>The original scale of that star.</returns>
    private Vector3 GetOriginalStarScale(int starCount)
    {
        switch (starCount)
        {
            case 1: return originalStarScales[0];
            case 2: return originalStarScales[1];
            case 3: return originalStarScales[2];
            case 4: return originalStarScales[3];
            case 5: return originalStarScales[4];
            default: return Vector3.one;
        }
    }

    /// <summary>
    /// Awards a star rank with animation, particle effect, and sound.
    /// The star fades in, stays for 1 second, then fades out.
    /// </summary>
    /// <param name="starCount">Number of stars achieved.</param>
    private void AwardStar(int starCount)
    {
        // Get the permanent star image
        Image starImage = GetStarImage(starCount);
        if (starImage == null) return;
        
        // Stop any existing animation for this star
        if (starAnimationRoutine != null)
        {
            StopCoroutine(starAnimationRoutine);
        }
        
        // Animate the permanent star (fade in, stay, fade out)
        starAnimationRoutine = StartCoroutine(AnimateStar(starImage, starCount));
        
        // Play popup star effect (extra visual flair)
        if (starPopupImage != null)
        {
            StartCoroutine(AnimateStarPopup());
        }
        
        // Play particle effect
        if (starParticleEffect != null)
        {
            starParticleEffect.Play();
        }
        
        // Play audio
        if (audioSource != null && starAchievedAudioClip != null)
        {
            audioSource.PlayOneShot(starAchievedAudioClip, starAchievedAudioVolume);
        }
    }

    /// <summary>
    /// Animates a star image fading in, staying, and fading out.
    /// </summary>
    /// <param name="starImage">The star image to animate.</param>
    /// <param name="starCount">Star number for scale reference.</param>
    IEnumerator AnimateStar(Image starImage, int starCount)
    {
        Vector3 originalScale = GetOriginalStarScale(starCount);
        
        // Reset to starting state (invisible, small scale)
        starImage.transform.localScale = originalScale * 0.5f;
        Color starColor = starImage.color;
        starColor.a = 0;
        starImage.color = starColor;
        
        // FADE IN + SCALE UP
        float timer = 0;
        while (timer < starFadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / starFadeInTime;
            
            // Scale with overshoot
            float scaleT = t;
            if (scaleT < 0.5f)
            {
                scaleT = scaleT * 2f;
                scaleT = 1f - (1f - scaleT) * (1f - scaleT);
            }
            else
            {
                scaleT = (scaleT - 0.5f) * 2f;
                scaleT = 1f + (scaleT * (starScaleAmount - 1f));
            }
            
            float currentScale = Mathf.Lerp(0.5f, starScaleAmount, scaleT);
            starImage.transform.localScale = originalScale * currentScale;
            
            float alpha = Mathf.Lerp(0, 1, t);
            starColor = starImage.color;
            starColor.a = alpha;
            starImage.color = starColor;
            
            yield return null;
        }
        
        // Ensure full visibility and max scale
        starImage.transform.localScale = originalScale * starScaleAmount;
        starColor = starImage.color;
        starColor.a = 1;
        starImage.color = starColor;
        
        // Quick settle back to normal scale
        timer = 0;
        float settleTime = 0.1f;
        Vector3 startScale = starImage.transform.localScale;
        Vector3 targetScale = originalScale;
        
        while (timer < settleTime)
        {
            timer += Time.deltaTime;
            float t = timer / settleTime;
            starImage.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        starImage.transform.localScale = originalScale;
        
        // STAY visible
        timer = 0;
        while (timer < starStayTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        // FADE OUT
        timer = 0;
        while (timer < starFadeOutTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / starFadeOutTime);
            starColor = starImage.color;
            starColor.a = alpha;
            starImage.color = starColor;
            yield return null;
        }
        
        // Ensure fully transparent
        starColor = starImage.color;
        starColor.a = 0;
        starImage.color = starColor;
        
        // Reset scale
        starImage.transform.localScale = originalScale;
    }

    /// <summary>
    /// Animates the popup star for extra visual flair.
    /// </summary>
    IEnumerator AnimateStarPopup()
    {
        if (starPopupImage == null) yield break;
        
        // Reset popup to starting state
        starPopupImage.color = goldFlashColor;
        starPopupImage.transform.localScale = originalStarPopupScale * 0.5f;
        
        // FADE IN + SCALE UP
        float timer = 0;
        Color starColor = starPopupImage.color;
        starColor.a = 0;
        starPopupImage.color = starColor;
        
        while (timer < starFadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / starFadeInTime;
            
            // Scale with overshoot
            float scaleT = t;
            if (scaleT < 0.5f)
            {
                scaleT = scaleT * 2f;
                scaleT = 1f - (1f - scaleT) * (1f - scaleT);
            }
            else
            {
                scaleT = (scaleT - 0.5f) * 2f;
                scaleT = 1f + (scaleT * (starScaleAmount - 1f));
            }
            
            float currentScale = Mathf.Lerp(0.5f, starScaleAmount, scaleT);
            starPopupImage.transform.localScale = originalStarPopupScale * currentScale;
            
            float alpha = Mathf.Lerp(0, 1, t);
            starColor = starPopupImage.color;
            starColor.a = alpha;
            starPopupImage.color = starColor;
            
            yield return null;
        }
        
        // Ensure full visibility and max scale
        starPopupImage.transform.localScale = originalStarPopupScale * starScaleAmount;
        starColor = starPopupImage.color;
        starColor.a = 1;
        starPopupImage.color = starColor;
        
        // Quick settle back to normal scale
        timer = 0;
        float settleTime = 0.1f;
        Vector3 startScale = starPopupImage.transform.localScale;
        Vector3 targetScale = originalStarPopupScale;
        
        while (timer < settleTime)
        {
            timer += Time.deltaTime;
            float t = timer / settleTime;
            starPopupImage.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        starPopupImage.transform.localScale = originalStarPopupScale;
        
        // STAY visible
        timer = 0;
        while (timer < starStayTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        // FADE OUT
        timer = 0;
        while (timer < starFadeOutTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / starFadeOutTime);
            starColor = starPopupImage.color;
            starColor.a = alpha;
            starPopupImage.color = starColor;
            yield return null;
        }
        
        // Ensure fully transparent
        starColor = starPopupImage.color;
        starColor.a = 0;
        starPopupImage.color = starColor;
    }

    /// <summary>
    /// Updates the combo text display, ensuring it never shows empty string.
    /// </summary>
    private void UpdateComboDisplay()
    {
        if (comboText)
        {
            comboText.text = currentCombo.ToString();
            comboText.alpha = 1;
        }
    }

    /// <summary>
    /// Shows the streak lost image and plays audio if combo was high enough.
    /// </summary>
    private void ShowStreakLostEffect()
    {
        // Only show effect if the combo was at least the minimum threshold
        if (currentCombo >= minComboForStreakLostEffect)
        {
            if (streakLostRoutine != null)
            {
                StopCoroutine(streakLostRoutine);
            }
            streakLostRoutine = StartCoroutine(AnimateStreakLost());
        }
    }

    /// <summary>
    /// Animates the streak lost image fading in and out.
    /// </summary>
    IEnumerator AnimateStreakLost()
    {
        if (streakLostImage == null) yield break;
        
        // Play audio
        if (audioSource != null && streakLostAudioClip != null)
        {
            audioSource.PlayOneShot(streakLostAudioClip, streakLostAudioVolume);
        }
        
        // Fade IN
        float timer = 0;
        Color imageColor = streakLostImage.color;
        imageColor.a = 0;
        streakLostImage.color = imageColor;
        
        while (timer < streakLostFadeInTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / streakLostFadeInTime);
            Color c = streakLostImage.color;
            c.a = alpha;
            streakLostImage.color = c;
            yield return null;
        }
        
        // Ensure fully visible
        imageColor = streakLostImage.color;
        imageColor.a = 1;
        streakLostImage.color = imageColor;
        
        // STAY
        timer = 0;
        while (timer < streakLostStayTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        // FADE OUT
        timer = 0;
        while (timer < streakLostFadeOutTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / streakLostFadeOutTime);
            Color c = streakLostImage.color;
            c.a = alpha;
            streakLostImage.color = c;
            yield return null;
        }
        
        // Ensure fully transparent
        imageColor = streakLostImage.color;
        imageColor.a = 0;
        streakLostImage.color = imageColor;
    }

    /// <summary>
    /// Adds points to the player's current score and updates the display.
    /// </summary>
    /// <param name="amount">Number of points to add.</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreDisplay();
        
        // Check for star achievements
        CheckAndAwardStars();
    }

    /// <summary>
    /// Updates the score UI display with 8-digit zero-padded formatting.
    /// </summary>
    void UpdateScoreDisplay()
    {
        if (scoreText) scoreText.text = currentScore.ToString("D8");
    }

    /// <summary>
    /// Registers a hit judgment, updating score, combo, and visual feedback.
    /// </summary>
    /// <param name="judgmentName">Judgment type: "PERFECT", "GOOD", "MEH", or "MISS".</param>
    public void RegisterHit(string judgmentName)
    {
        // Store previous combo for streak lost detection
        int previousCombo = currentCombo;
        
        // 1. Logic
        if (judgmentName == "MISS")
        {
            // Show streak lost effect if we had a decent combo before missing
            if (previousCombo >= minComboForStreakLostEffect)
            {
                ShowStreakLostEffect();
            }
            
            currentCombo = 0;
            UpdateComboDisplay(); // This will show "0" instead of empty
        }
        else
        {
            currentCombo++;
            if (currentCombo > maxCombo) maxCombo = currentCombo;
            
            UpdateComboDisplay();
            
            if(comboRoutine != null) StopCoroutine(comboRoutine);
            comboRoutine = StartCoroutine(PulseCombo());

            // Scoring Logic
            if (judgmentName == "PERFECT") AddScore(scorePerPerfect);
            else if (judgmentName == "GOOD") AddScore(scorePerGood);
            else if (judgmentName == "MEH") AddScore(scorePerMeh);
        }

        // 2. Visuals - Overwrite current animation if exists
        if (judgmentRoutine != null)
        {
            StopCoroutine(judgmentRoutine);
        }
        
        // Reset add sprite to 0 immediately before starting new animation
        if (addSprite)
        {
            if (addSpriteRoutine != null) 
            {
                StopCoroutine(addSpriteRoutine);
            }
            
            // Instantly reset add sprite scale to 0
            Vector3 resetScale = addSprite.transform.localScale;
            resetScale.x = 0;
            addSprite.transform.localScale = resetScale;
            
            // Start new add sprite animation
            addSpriteRoutine = StartCoroutine(AnimateAddSprite(judgmentName));
        }
        
        judgmentRoutine = StartCoroutine(SynchronizedAnimation(judgmentName));
    }

    /// <summary>
    /// Animates the add sprite scaling up quickly and resets after animation.
    /// </summary>
    /// <param name="judgmentType">Type of judgment for color matching.</param>
    IEnumerator AnimateAddSprite(string judgmentType)
    {
        // Get the color for this judgment type
        bool isRainbow;
        Color judgmentColor = GetJudgmentColor(judgmentType, out isRainbow);
        
        // Set the add sprite color (no rainbow for add sprite, just solid color)
        if (addSprite)
        {
            addSprite.color = judgmentColor;
        }
        
        // Ensure we start from 0
        Vector3 startScale = addSprite.transform.localScale;
        startScale.x = 0;
        addSprite.transform.localScale = startScale;
        
        Vector3 targetScale = originalAddSpriteScale;
        targetScale.x = addSpriteTargetScaleX;
        
        // Scale up animation
        float elapsed = 0;
        while (elapsed < addSpriteScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / addSpriteScaleDuration;
            float curveValue = addSpriteScaleCurve.Evaluate(t);
            
            Vector3 newScale = Vector3.Lerp(startScale, targetScale, curveValue);
            addSprite.transform.localScale = newScale;
            
            yield return null;
        }
        
        // Ensure target scale is reached
        addSprite.transform.localScale = targetScale;
        
        // Wait for the main animation to complete
        yield return new WaitForSeconds(totalAnimationDuration - addSpriteScaleDuration);
        
        // Reset scale back to 0 after main animation finishes
        if (addSprite)
        {
            Vector3 resetScale = addSprite.transform.localScale;
            resetScale.x = 0;
            addSprite.transform.localScale = resetScale;
        }
    }

    /// <summary>
    /// Gets the appropriate color for a judgment type.
    /// </summary>
    /// <param name="judgmentType">Type of judgment.</param>
    /// <param name="isRainbow">Output parameter indicating if this judgment should use rainbow effect.</param>
    /// <returns>The base color for the judgment.</returns>
    Color GetJudgmentColor(string judgmentType, out bool isRainbow)
    {
        isRainbow = false;
        
        switch (judgmentType)
        {
            case "PERFECT":
                isRainbow = true;
                return perfectColor;
            case "GOOD":
                return goodColor;
            case "MEH":
                return mehColor;
            case "MISS":
                return missColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Plays synchronized spotlight and text animation with proper overwrite handling.
    /// </summary>
    /// <param name="judgmentType">Type of judgment to display.</param>
    IEnumerator SynchronizedAnimation(string judgmentType)
    {
        // Determine judgment properties
        bool isRainbow;
        Color judgmentColor = GetJudgmentColor(judgmentType, out isRainbow);
        
        // Calculate animation timings
        AnimationData animData = new AnimationData(
            totalAnimationDuration, 
            fadeInRatio, 
            stayRatio, 
            fadeOutRatio, 
            judgmentType, 
            judgmentColor, 
            isRainbow
        );
        
        currentAnimation = animData;
        float startTime = Time.time;
        
        // Setup text
        judgmentText.text = judgmentType;
        judgmentText.transform.localScale = Vector3.one * baseTextScale;
        
        // Setup sprite with base color (will be overridden by rainbow if needed)
        if (spotlightSprite)
        {
            spotlightSprite.color = judgmentColor;
        }
        
        // Ensure both elements start invisible
        judgmentText.alpha = 0;
        if (spotlightSprite)
        {
            Color c = spotlightSprite.color;
            c.a = 0;
            spotlightSprite.color = c;
        }
        
        // PHASE 1: FADE IN (Sprite and Text together)
        float fadeInEnd = startTime + animData.fadeInTime;
        while (Time.time < fadeInEnd)
        {
            // Check if this animation was overwritten
            if (currentAnimation != animData) yield break;
            
            float elapsed = Time.time - startTime;
            float t = elapsed / animData.fadeInTime;
            float alpha = Mathf.Lerp(0, 1, t);
            
            // Update sprite alpha and color
            if (spotlightSprite)
            {
                if (isRainbow)
                {
                    ApplyRainbowColorToSprite();
                    Color c = spotlightSprite.color;
                    c.a = alpha;
                    spotlightSprite.color = c;
                }
                else
                {
                    Color c = judgmentColor;
                    c.a = alpha;
                    spotlightSprite.color = c;
                }
            }
            
            // Update text alpha and color
            if (isRainbow)
            {
                ApplyRainbowColor();
                Color c = judgmentText.color;
                c.a = alpha;
                judgmentText.color = c;
            }
            else
            {
                judgmentText.color = judgmentColor;
                judgmentText.alpha = alpha;
            }
            
            yield return null;
        }
        
        // Ensure fully visible at end of fade in
        if (currentAnimation == animData)
        {
            if (spotlightSprite)
            {
                if (isRainbow)
                {
                    ApplyRainbowColorToSprite();
                    Color c = spotlightSprite.color;
                    c.a = 1;
                    spotlightSprite.color = c;
                }
                else
                {
                    Color c = judgmentColor;
                    c.a = 1;
                    spotlightSprite.color = c;
                }
            }
            
            if (isRainbow)
            {
                ApplyRainbowColor();
                Color c = judgmentText.color;
                c.a = 1;
                judgmentText.color = c;
            }
            else
            {
                judgmentText.color = judgmentColor;
                judgmentText.alpha = 1;
            }
        }
        
        // PHASE 2: STAY (Both visible)
        float stayEnd = fadeInEnd + animData.stayTime;
        while (Time.time < stayEnd)
        {
            if (currentAnimation != animData) yield break;
            
            // Update rainbow colors during stay
            if (isRainbow)
            {
                if (spotlightSprite) ApplyRainbowColorToSprite();
                ApplyRainbowColor();
            }
            
            yield return null;
        }
        
        // PHASE 3: FADE OUT (Sprite and Text together)
        float fadeOutEnd = stayEnd + animData.fadeOutTime;
        while (Time.time < fadeOutEnd)
        {
            if (currentAnimation != animData) yield break;
            
            float elapsed = Time.time - stayEnd;
            float t = elapsed / animData.fadeOutTime;
            float alpha = Mathf.Lerp(1, 0, t);
            
            // Update sprite alpha and color
            if (spotlightSprite)
            {
                if (isRainbow)
                {
                    ApplyRainbowColorToSprite();
                    Color c = spotlightSprite.color;
                    c.a = alpha;
                    spotlightSprite.color = c;
                }
                else
                {
                    Color c = judgmentColor;
                    c.a = alpha;
                    spotlightSprite.color = c;
                }
            }
            
            // Update text alpha and color
            if (isRainbow)
            {
                ApplyRainbowColor();
                Color c = judgmentText.color;
                c.a = alpha;
                judgmentText.color = c;
            }
            else
            {
                judgmentText.color = judgmentColor;
                judgmentText.alpha = alpha;
            }
            
            yield return null;
        }
        
        // Ensure both are hidden at the end
        if (currentAnimation == animData)
        {
            judgmentText.alpha = 0;
            if (spotlightSprite)
            {
                Color c = spotlightSprite.color;
                c.a = 0;
                spotlightSprite.color = c;
            }
        }
    }

    /// <summary>
    /// Applies a cycling rainbow color effect to the judgment text.
    /// </summary>
    void ApplyRainbowColor()
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f); 
        Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
        rainbowColor.a = judgmentText.color.a;
        judgmentText.color = rainbowColor;
    }
    
    /// <summary>
    /// Applies a cycling rainbow color effect to the spotlight sprite.
    /// </summary>
    void ApplyRainbowColorToSprite()
    {
        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
        Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
        rainbowColor.a = spotlightSprite.color.a;
        spotlightSprite.color = rainbowColor;
    }

    /// <summary>
    /// Animates the combo counter with a quick scale pulse effect.
    /// </summary>
    IEnumerator PulseCombo()
    {
        if (comboText == null) yield break;
        
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