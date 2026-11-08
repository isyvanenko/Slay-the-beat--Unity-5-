using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Runs Tug of Slay: tracks the pull between the players, drives the bar, picks special arrows and ends the match.
/// </summary>
/// <remarks>
/// Every note is a duel: whoever hits it closer to the receptor wins that note's pull. A miss loses to any hit,
/// and an exact tie or a double miss moves nothing.
/// Pull is separate from score, so special arrows never affect star thresholds.
/// The bar is at 1 when Player 1 has pulled it all the way and at 0 when Player 2 has.
/// Do not also assign this slider to GameplayManager.progressSlider, or song time will overwrite it.
/// </remarks>
[RequireComponent(typeof(Slider))]
public class TugOfSlayBar : MonoBehaviour
{
    [Header("Players")]
    public PlayerScoreManager p1ScoreManager;
    public PlayerScoreManager p2ScoreManager;

    [Header("Bar")]
    [Tooltip("How many pull points a player must lead by to pull the bar all the way. A normal note is worth 1 by default.")]
    [Min(1)] public int perfectsForFullPull = 20;

    [Tooltip("How quickly the bar catches up to the pull. Higher is snappier.")]
    [Min(0.1f)] public float smoothSpeed = 8f;

    [Header("Pull")]
    [Tooltip("Pull awarded to the winner of a normal note's duel.")]
    public float notePull = 1f;

    [Tooltip("Pull awarded for finishing a hold note. If both players finish it, this cancels out.")]
    public float holdFinishPull = 0.5f;

    [Header("Special Arrows")]
    [Tooltip("Chance for an eligible tap note to become special.")]
    [Range(0f, 1f)] public float specialChance = 0.15f;

    [Tooltip("Minimum notes between special arrows, so they don't clump together.")]
    [Min(0)] public int minNotesBetweenSpecials = 6;

    [Tooltip("Share of special arrows that are Clash arrows. The rest are Rainbow.")]
    [Range(0f, 1f)] public float clashShare = 0.25f;

    [Tooltip("How many times a normal note's pull a Rainbow arrow is worth.")]
    public float rainbowMultiplier = 2f;

    [Tooltip("Pull awarded to the winner of a Clash arrow's duel.")]
    public float clashPull = 4f;

    [Header("Comeback")]
    [Tooltip("How far the bar must be toward the opponent (0-1) before the trailing player gets the bonus.")]
    [Range(0f, 1f)] public float comebackThreshold = 0.6f;

    public float comebackMultiplier = 1.25f;

    [Header("Slay Finish")]
    [Tooltip("Seconds a player must keep the bar pinned to win early.")]
    [Min(0.1f)] public float slayHoldSeconds = 3f;

    [Tooltip("The countdown cancels if the bar falls below this (0-1) toward the leader's side.")]
    [Range(0f, 1f)] public float slayCancelAt = 0.9f;

    [Tooltip("Seconds of slow motion and fading to black and white before the results screen.")]
    [Min(0.1f)] public float slayEndDelay = 3f;

    [Tooltip("Game speed at the end of the slow motion. 1 is normal speed.")]
    [Range(0.05f, 1f)] public float slayEndTimeScale = 0.25f;

    [Tooltip("Music pitch at the end of the slow motion. 1 is normal, lower is slower and deeper.")]
    [Range(0.1f, 1f)] public float slayEndMusicPitch = 0.35f;

    [Header("Status Text")]
    [Tooltip("Position of the status message, relative to the centre of the canvas.")]
    public Vector2 statusOffset = Vector2.zero;

    [Tooltip("Seconds a status message is on screen. Uses the same fade timing as the judgment text.")]
    [Min(0.1f)] public float statusDuration = 1.2f;

    [Tooltip("Size a status message pops in at, relative to its final size.")]
    [Min(1f)] public float statusPopScale = 1.3f;

    [Header("Match Display")]
    [Tooltip("Position of the round and wins display, relative to the top centre of the canvas.")]
    public Vector2 scoreboardOffset = new Vector2(0f, -45f);

    [Header("Bar Colours")]
    [Tooltip("Colour of Player 1's side of the bar.")]
    public Color p1Color = new Color(1f, 0.2f, 0.6f);

    [Tooltip("Colour of Player 2's side of the bar.")]
    public Color p2Color = new Color(0.1f, 0.8f, 1f);

    [Header("Finish Sound")]
    [Tooltip("Seconds the sound fades out for before the next scene.")]
    [Min(0f)] public float slayFadeOutTime = 0.5f;

    private static readonly Color ClashTextColor = new Color(1f, 0.8f, 0.2f);

    private Slider slider;
    private GameplayManager gameplayManager;
    private TugOfSlaySounds sounds;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI scoreboardText;
    private AudioSource audioSource;
    private float statusStartTime;
    private float statusShownFor;
    private bool statusIsRainbow;
    private bool statusVisible;
    private Vector3 statusScale = Vector3.one;
    private int lastCountdownSecond;

    /// <summary>Current pull from -1 (all the way to Player 2) to 1 (all the way to Player 1).</summary>
    private float pull;

    private float slayTimer;
    private int slayingPlayer;
    private bool matchFinished;
    private int roundWinner;

    /// <summary>Notes waiting on the second player's result, by note id.</summary>
    private readonly Dictionary<int, DuelEntry> pendingDuels = new Dictionary<int, DuelEntry>();

    private struct DuelEntry
    {
        public bool isP1;
        public float distance;
    }

    /// <summary>Tug mode needs two players, except when the scene is opened directly in the editor to test it.</summary>
    /// <summary>True once this level has a result and its finish has started.</summary>
    public bool IsFinished => matchFinished;

    private bool SlayFinishAllowed => SessionConfig.PlayerCount == 2 || GameDataBridge.SelectedSong == null;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.value = 0.5f;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        sounds = Resources.Load<TugOfSlaySounds>("TugOfSlaySounds");
        if (sounds == null) sounds = ScriptableObject.CreateInstance<TugOfSlaySounds>();
    }

    /// <summary>
    /// Connects the bar to the players and colours it to show whose side is whose.
    /// </summary>
    public void Setup(PlayerScoreManager p1, PlayerScoreManager p2)
    {
        p1ScoreManager = p1;
        p2ScoreManager = p2;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
            fill.color = p1Color;

        Transform background = transform.Find("Background");
        if (background != null && background.TryGetComponent(out Image backgroundImage))
            backgroundImage.color = p2Color;
    }

    void Start()
    {
        gameplayManager = FindFirstObjectByType<GameplayManager>();
        CreateStatusText();

        if (TugOfSlayMatch.IsActive) CreateScoreboard();
    }

    void Update()
    {
        UpdateStatusText();

        if (gameplayManager != null && gameplayManager.IsPaused()) return;

        float target = 0.5f + 0.5f * pull;
        slider.value = Mathf.Lerp(slider.value, target, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        UpdateSlayFinish();
    }

    /// <summary>
    /// Randomly marks some tap notes as Rainbow or Clash. Both players spawn from this chart, so their arrows match.
    /// </summary>
    public void AssignSpecialNotes(List<NoteEvent> chart)
    {
        System.Random random = new System.Random();
        int notesSinceSpecial = minNotesBetweenSpecials;

        foreach (NoteEvent note in chart)
        {
            notesSinceSpecial++;

            if (note.holdLength > 0f || notesSinceSpecial <= minNotesBetweenSpecials) continue;
            if (random.NextDouble() >= specialChance) continue;

            note.kind = random.NextDouble() < clashShare ? NoteKind.Clash : NoteKind.Rainbow;
            notesSinceSpecial = 0;
        }
    }

    /// <summary>
    /// Records a player's result on a note, and settles the note's duel once both players have one.
    /// </summary>
    /// <param name="scoreManager">Score manager of the player.</param>
    /// <param name="kind">Kind of note.</param>
    /// <param name="noteId">Chart index of the note, used to pair the two players' results.</param>
    /// <param name="distance">Distance from the receptor when hit, or positive infinity for a miss.</param>
    public void RegisterNoteResult(PlayerScoreManager scoreManager, NoteKind kind, int noteId, float distance)
    {
        if (matchFinished || !IsPlayer(scoreManager)) return;

        bool isP1 = scoreManager == p1ScoreManager;

        // A solo player has no opponent arrow, so the duel resolves against a miss.
        if (SessionConfig.PlayerCount != 2)
        {
            ResolveDuel(kind, isP1, distance, float.PositiveInfinity);
            return;
        }

        if (!pendingDuels.TryGetValue(noteId, out DuelEntry first))
        {
            pendingDuels[noteId] = new DuelEntry { isP1 = isP1, distance = distance };
            return;
        }

        if (first.isP1 == isP1) return;

        pendingDuels.Remove(noteId);
        ResolveDuel(kind, isP1, distance, first.distance);
    }

    /// <summary>Awards pull for finishing a hold note. A dropped hold never reports, so it earns nothing.</summary>
    public void RegisterHoldFinish(PlayerScoreManager scoreManager)
    {
        if (matchFinished || !IsPlayer(scoreManager)) return;

        ApplyPull(scoreManager == p1ScoreManager, holdFinishPull);
    }

    /// <summary>Pushes the bar a quarter of the way toward a player. Used by DevCheats.</summary>
    public void DevPull(bool towardP1)
    {
        ApplyPull(towardP1, perfectsForFullPull * 0.25f);
    }

    /// <summary>Returns the winner of this level: 1, 2, or 0 for a draw or if it has no result yet.</summary>
    public int GetWinner() => roundWinner;

    /// <summary>
    /// Finishes a level that reached the end of its song: the highest score wins, then whoever the bar favours.
    /// </summary>
    public void FinishAtSongEnd()
    {
        if (matchFinished) return;

        int p1Score = p1ScoreManager != null ? p1ScoreManager.currentScore : 0;
        int p2Score = p2ScoreManager != null ? p2ScoreManager.currentScore : 0;

        int winner = 0;
        if (p1Score != p2Score) winner = p1Score > p2Score ? 1 : 2;
        else if (Mathf.Abs(pull) > 0.001f) winner = pull > 0f ? 1 : 2;

        StartCoroutine(FinishMatch(winner, false));
    }

    private bool IsPlayer(PlayerScoreManager scoreManager)
    {
        return scoreManager == p1ScoreManager || scoreManager == p2ScoreManager;
    }

    private void ResolveDuel(NoteKind kind, bool isP1, float distance, float opponentDistance)
    {
        if (distance == opponentDistance)
        {
            if (kind == NoteKind.Clash)
            {
                ShowStatus("CLASH TIE!", ClashTextColor);
                PlaySound(sounds.clashTieSound);
            }
            return;
        }

        bool p1Wins = (distance < opponentDistance) == isP1;

        switch (kind)
        {
            case NoteKind.Rainbow:
                ApplyPull(p1Wins, notePull * rainbowMultiplier);
                ShowStatus($"{PlayerName(p1Wins)} RAINBOW x{rainbowMultiplier:0.##}!", Color.white, statusDuration, true);
                PlaySound(sounds.rainbowSound);
                break;
            case NoteKind.Clash:
                ApplyPull(p1Wins, clashPull);
                ShowStatus($"{PlayerName(p1Wins)} WINS CLASH!", ClashTextColor);
                PlaySound(sounds.clashWinSound);
                break;
            default:
                ApplyPull(p1Wins, notePull);
                break;
        }
    }

    private void ApplyPull(bool towardP1, float amount)
    {
        bool isComeback = towardP1 ? pull <= -comebackThreshold : pull >= comebackThreshold;
        if (isComeback) amount *= comebackMultiplier;

        float direction = towardP1 ? 1f : -1f;
        pull = Mathf.Clamp(pull + direction * amount / perfectsForFullPull, -1f, 1f);
    }

    private void UpdateSlayFinish()
    {
        if (matchFinished || !SlayFinishAllowed) return;

        if (slayingPlayer == 0)
        {
            if (Mathf.Abs(pull) < 1f) return;

            slayingPlayer = pull > 0f ? 1 : 2;
            slayTimer = slayHoldSeconds;
            lastCountdownSecond = 0;
        }

        float leaderPull = slayingPlayer == 1 ? pull : -pull;
        if (leaderPull < slayCancelAt)
        {
            slayingPlayer = 0;
            ShowStatus("SAVED!", Color.white);
            PlaySound(sounds.savedSound);
            return;
        }

        slayTimer -= Time.deltaTime;

        if (slayTimer <= 0f)
        {
            StartCoroutine(FinishMatch(slayingPlayer, true));
            return;
        }

        int countdownSecond = Mathf.CeilToInt(slayTimer);
        if (countdownSecond != lastCountdownSecond)
        {
            lastCountdownSecond = countdownSecond;
            ShowStatus($"{PlayerName(slayingPlayer == 1)} SLAYING {countdownSecond}", Color.white);
            PlaySound(sounds.slayTickSound);
        }
    }

    private IEnumerator FinishMatch(int winner, bool slayed)
    {
        matchFinished = true;
        roundWinner = winner;

        if (TugOfSlayMatch.IsActive)
        {
            TugOfSlayMatch.RecordRoundWinner(winner);
            UpdateScoreboard();
        }

        ShowStatus(GetFinishMessage(winner, slayed), Color.white, slayEndDelay + 1f, winner != 0);
        PlaySound(winner != 0 ? sounds.slayWinSound : sounds.clashTieSound);

        AudioSource music = gameplayManager != null ? gameplayManager.musicSource : null;
        float musicVolume = music != null ? music.volume : 1f;

        if (gameplayManager != null) gameplayManager.BeginEnding();

        Volume blackAndWhite = CreateBlackAndWhiteVolume();
        CanvasGroup p1Area = GetCanvasGroup(p1ScoreManager);
        CanvasGroup p2Area = GetCanvasGroup(p2ScoreManager);

        // Unscaled, because this is the code slowing the game down.
        for (float elapsed = 0f; elapsed < slayEndDelay; elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / slayEndDelay);

            Time.timeScale = Mathf.Lerp(1f, slayEndTimeScale, progress);
            blackAndWhite.weight = progress;
            if (music != null) music.pitch = Mathf.Lerp(1f, slayEndMusicPitch, progress);

            // The gameplay canvas is an overlay, so colour effects can't reach it. Fade the lanes out instead.
            float areaAlpha = 1f - Mathf.Clamp01(progress * 2f);
            if (p1Area != null) p1Area.alpha = areaAlpha;
            if (p2Area != null) p2Area.alpha = areaAlpha;

            // Fade the sound at the very end rather than cutting it off when the scene changes.
            float secondsLeft = slayEndDelay - elapsed;
            float soundFade = slayFadeOutTime > 0f ? Mathf.Clamp01(secondsLeft / slayFadeOutTime) : 1f;
            audioSource.volume = soundFade;
            if (music != null) music.volume = musicVolume * soundFade;

            yield return null;
        }

        if (gameplayManager != null)
        {
            // EndLevel restores normal speed, so stop the stage snapping back during the screen fade.
            if (gameplayManager.director != null) gameplayManager.director.Pause();
            gameplayManager.EndLevel();
        }
    }

    private string GetFinishMessage(int winner, bool slayed)
    {
        if (winner == 0) return "DRAW!";

        string player = PlayerName(winner == 1);
        if (TugOfSlayMatch.IsActive && TugOfSlayMatch.IsOver) return $"{player} WINS MATCH!";
        if (slayed) return $"{player} SLAYS!";
        return TugOfSlayMatch.IsActive ? $"{player} WINS ROUND!" : $"{player} WINS!";
    }

    /// <summary>Creates a global post-processing volume that removes all colour at full weight.</summary>
    private Volume CreateBlackAndWhiteVolume()
    {
        GameObject volumeObject = new GameObject("Slay Finish Volume");

        Volume sceneVolume = FindFirstObjectByType<Volume>();
        if (sceneVolume != null) volumeObject.layer = sceneVolume.gameObject.layer;

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<ColorAdjustments>().saturation.Override(-100f);
        volume.sharedProfile = profile;

        return volume;
    }

    private static CanvasGroup GetCanvasGroup(PlayerScoreManager scoreManager)
    {
        if (scoreManager == null) return null;

        CanvasGroup group = scoreManager.GetComponent<CanvasGroup>();
        return group != null ? group : scoreManager.gameObject.AddComponent<CanvasGroup>();
    }

    private static string PlayerName(bool isP1) => isP1 ? "P1" : "P2";

    /// <summary>Creates the round and wins display under the bar.</summary>
    private void CreateScoreboard()
    {
        scoreboardText = CreateText("Tug Scoreboard Text", new Vector2(0.5f, 1f), scoreboardOffset);
        UpdateScoreboard();
    }

    private void UpdateScoreboard()
    {
        if (scoreboardText == null) return;

        scoreboardText.text =
            $"ROUND {TugOfSlayMatch.Round}   P1 {TugOfSlayMatch.P1Wins} - {TugOfSlayMatch.P2Wins} P2";
    }

    /// <summary>Creates text on the canvas that uses the same font as Player 1's judgment text.</summary>
    private TextMeshProUGUI CreateText(string objectName, Vector2 anchor, Vector2 position)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(transform.parent, false);
        textObject.transform.SetAsLastSibling();

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600f, 100f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = "";

        TextMeshProUGUI judgmentText = p1ScoreManager != null ? p1ScoreManager.judgmentText : null;
        if (judgmentText != null)
        {
            text.font = judgmentText.font;
            text.fontSharedMaterial = judgmentText.fontSharedMaterial;
            text.fontSize = judgmentText.fontSize;
            text.fontStyle = judgmentText.fontStyle;
        }

        return text;
    }

    /// <summary>Creates the status message, sized to match Player 1's judgment text so both look the same.</summary>
    private void CreateStatusText()
    {
        statusText = CreateText("Tug Status Text", new Vector2(0.5f, 0.5f), statusOffset);

        TextMeshProUGUI judgmentText = p1ScoreManager != null ? p1ScoreManager.judgmentText : null;
        if (judgmentText == null) return;

        // Judgment text can sit inside a scaled panel, so convert its size to this canvas.
        float panelScale = judgmentText.transform.parent.lossyScale.x;
        float canvasScale = transform.parent.lossyScale.x;
        float scale = p1ScoreManager.baseTextScale;
        if (panelScale > 0f && canvasScale > 0f) scale *= panelScale / canvasScale;
        statusScale = Vector3.one * scale;
    }

    private void ShowStatus(string message, Color color) => ShowStatus(message, color, statusDuration, false);

    private void ShowStatus(string message, Color color, float duration, bool rainbow)
    {
        if (statusText == null) return;

        statusText.text = message;
        statusText.color = color;
        statusStartTime = Time.unscaledTime;
        statusShownFor = duration;
        statusIsRainbow = rainbow;
        statusVisible = true;
        UpdateStatusText();
    }

    /// <summary>Animates the status message using the judgment text's fade timing, plus a pop in.</summary>
    private void UpdateStatusText()
    {
        if (statusText == null || !statusVisible) return;

        float elapsed = Time.unscaledTime - statusStartTime;
        if (elapsed >= statusShownFor)
        {
            statusVisible = false;
            statusText.text = "";
            return;
        }

        float fadeInTime = statusShownFor * (p1ScoreManager != null ? p1ScoreManager.fadeInRatio : 0.25f);
        float stayTime = statusShownFor * (p1ScoreManager != null ? p1ScoreManager.stayRatio : 0.25f);
        float fadeOutTime = statusShownFor - fadeInTime - stayTime;

        float alpha = 1f;
        if (elapsed < fadeInTime) alpha = elapsed / fadeInTime;
        else if (elapsed > fadeInTime + stayTime) alpha = 1f - (elapsed - fadeInTime - stayTime) / fadeOutTime;

        float pop = fadeInTime > 0f ? Mathf.Lerp(statusPopScale, 1f, elapsed / fadeInTime) : 1f;
        statusText.transform.localScale = statusScale * pop;

        if (statusIsRainbow)
        {
            float rainbowSpeed = p1ScoreManager != null ? p1ScoreManager.rainbowSpeed : 2f;
            statusText.color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * rainbowSpeed, 1f), 1f, 1f);
        }

        statusText.alpha = alpha;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip, sounds.volume);
    }
}
