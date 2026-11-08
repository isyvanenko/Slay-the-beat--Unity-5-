using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runs the short cool-down screen before each Tag of Slay round, showing the next song and then loading it.
/// </summary>
public class TugOfSlayRoundIntro : MonoBehaviour
{
    [Tooltip("Font used for all the text on this screen.")]
    public TMP_FontAsset font;

    [Tooltip("Seconds before the round starts.")]
    [Min(1)] public int countdownSeconds = 5;

    [Tooltip("Volume of the song preview playing on this screen.")]
    [Range(0f, 1f)] public float previewVolume = 0.5f;

    [Tooltip("Scene to go to if this screen is opened without a match, e.g. directly in the editor.")]
    public string fallbackScene = "SongSelection";

    private const float RainbowSpeed = 0.5f;
    private const float PopInTime = 0.45f;
    private const float PreviewFadeTime = 1f;
    private const float CountdownPopScale = 1.5f;

    private static readonly Color BackgroundTint = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color[] DifficultyColors =
    {
        new Color(0.45f, 1f, 0.55f),
        new Color(1f, 0.9f, 0.4f),
        new Color(1f, 0.4f, 0.45f)
    };

    private RectTransform canvasRect;
    private RectTransform coverRect;
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI countdownText;
    private AudioSource previewSource;
    private TugOfSlaySounds sounds;

    void Start()
    {
        SongGradeData song = GameDataBridge.SelectedSong;

        if (!TugOfSlayMatch.IsActive || song == null)
        {
            LoadScene(fallbackScene);
            return;
        }

        sounds = Resources.Load<TugOfSlaySounds>("TugOfSlaySounds");

        BuildScreen(song);
        StartPreview(song);
        StartCoroutine(RunCountdown(song));
    }

    void Update()
    {
        if (roundText != null)
            roundText.color = Color.HSVToRGB(Mathf.Repeat(Time.time * RainbowSpeed, 1f), 0.8f, 1f);
    }

    private IEnumerator RunCountdown(SongGradeData song)
    {
        StartCoroutine(PopIn(coverRect));
        StartCoroutine(PopIn(roundText.rectTransform));

        for (int secondsLeft = countdownSeconds; secondsLeft > 0; secondsLeft--)
        {
            countdownText.text = secondsLeft.ToString();
            if (sounds != null && sounds.slayTickSound != null)
                previewSource.PlayOneShot(sounds.slayTickSound, sounds.volume);

            for (float elapsed = 0f; elapsed < 1f; elapsed += Time.deltaTime)
            {
                float pop = Mathf.Lerp(CountdownPopScale, 1f, Mathf.Clamp01(elapsed / 0.25f));
                countdownText.rectTransform.localScale = Vector3.one * pop;

                if (secondsLeft == 1)
                    previewSource.volume = previewVolume * (1f - Mathf.Clamp01(elapsed / PreviewFadeTime));

                yield return null;
            }
        }

        LoadScene(song.gameplaySceneName);
    }

    private IEnumerator PopIn(RectTransform target)
    {
        for (float elapsed = 0f; elapsed < PopInTime; elapsed += Time.deltaTime)
        {
            float t = elapsed / PopInTime;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, eased);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void StartPreview(SongGradeData song)
    {
        previewSource = gameObject.AddComponent<AudioSource>();
        previewSource.playOnAwake = false;
        previewSource.volume = previewVolume;

        if (song.songPreviewClip == null) return;

        previewSource.clip = song.songPreviewClip;
        previewSource.Play();
    }

    private void BuildScreen(SongGradeData song)
    {
        GameObject canvasObject = new GameObject("Round Intro Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRect = canvasObject.GetComponent<RectTransform>();

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateBackground(song);
        coverRect = CreateCover(song);

        int difficulty = TugOfSlayMatch.RoundDifficulty;
        string roundName = TugOfSlayMatch.IsFinalRound ? "FINAL ROUND" : $"ROUND {TugOfSlayMatch.Round}";

        roundText = CreateText("Round", roundName, 110f, new Vector2(0.66f, 0.76f));
        CreateText("Song Name", song.songName, 56f, new Vector2(0.66f, 0.58f));

        TextMeshProUGUI difficultyText = CreateText("Difficulty", TugOfSlayMatch.RoundDifficultyName, 48f, new Vector2(0.66f, 0.48f));
        difficultyText.color = DifficultyColors[difficulty];

        CreateText("Wins", $"P1 {TugOfSlayMatch.P1Wins} - {TugOfSlayMatch.P2Wins} P2", 56f, new Vector2(0.66f, 0.36f));
        countdownText = CreateText("Countdown", "", 130f, new Vector2(0.66f, 0.17f));
    }

    private void CreateBackground(SongGradeData song)
    {
        RectTransform rect = CreateRect("Background", Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = song.environmentSprite;
        image.color = song.environmentSprite != null ? BackgroundTint : Color.black;
    }

    private RectTransform CreateCover(SongGradeData song)
    {
        RectTransform rect = CreateRect("Cover Art", new Vector2(0.29f, 0.5f));
        rect.sizeDelta = new Vector2(640f, 640f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = song.songJacketSprite;
        image.preserveAspect = true;
        image.enabled = song.songJacketSprite != null;
        return rect;
    }

    private TextMeshProUGUI CreateText(string objectName, string message, float fontSize, Vector2 anchor)
    {
        RectTransform rect = CreateRect(objectName, anchor);
        rect.sizeDelta = new Vector2(1000f, fontSize * 1.4f);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;

        text.text = message;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateRect(string objectName, Vector2 anchor)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(canvasRect, false);

        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static void LoadScene(string sceneName)
    {
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
