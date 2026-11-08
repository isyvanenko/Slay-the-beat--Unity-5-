using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// The Tag of Slay screen in song select: explains the mode and asks the players if they want to play it.
/// </summary>
/// <remarks>
/// Lives in the SongSelection scene, so its layout and styling are edited there.
/// Song select shows and hides it, so it doesn't matter whether it's left active in the scene.
/// </remarks>
public class TugOfSlayPanel : MonoBehaviour
{
    private const float PromptPulseSpeed = 4f;
    private const float ShakeDuration = 0.3f;
    private const float ShakeDistance = 18f;

    [Header("Parts")]
    [Tooltip("Fades the whole panel in and out")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI promptText;

    [Header("Title")]
    [Tooltip("Cycle the title through rainbow colours. Untick to keep the colour set on the text")]
    public bool rainbowTitle = true;
    public float rainbowSpeed = 0.5f;

    [Header("Prompt")]
    public string readyMessage = "PRESS SELECT TO PLAY";
    public Color readyColor = Color.white;
    [Tooltip("Shown when only one player has joined, since a match needs two")]
    public string needsPlayersMessage = "NEEDS 2 PLAYERS";
    public Color needsPlayersColor = new Color(1f, 0.45f, 0.45f);
    [Tooltip("How much the prompt grows and shrinks. 0 keeps it still")]
    public float promptPulseAmount = 0.06f;

    private Coroutine shakeRoutine;
    private Vector2 promptPosition;
    private Vector3 promptScale = Vector3.one;

    /// <summary>Sets the prompt to match whether a match can be started.</summary>
    /// <param name="hasTwoPlayers">Whether two players have joined.</param>
    public void SetHasTwoPlayers(bool hasTwoPlayers)
    {
        if (promptText == null) return;

        promptText.text = hasTwoPlayers ? readyMessage : needsPlayersMessage;
        promptText.color = hasTwoPlayers ? readyColor : needsPlayersColor;
    }

    /// <summary>Sets how visible the panel is, from 0 to 1. It's switched off completely at 0.</summary>
    public void SetVisibility(float visibility)
    {
        if (canvasGroup != null) canvasGroup.alpha = visibility;
        gameObject.SetActive(visibility > 0f);
    }

    /// <summary>Shakes the prompt to show that the mode can't be started.</summary>
    public void ShakePrompt()
    {
        if (promptText == null || !isActiveAndEnabled) return;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    void Awake()
    {
        if (promptText == null) return;

        promptPosition = promptText.rectTransform.anchoredPosition;
        promptScale = promptText.rectTransform.localScale;
    }

    void Update()
    {
        if (titleText != null && rainbowTitle)
        {
            titleText.color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * rainbowSpeed, 1f), 0.8f, 1f);
        }

        if (promptText != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * PromptPulseSpeed) * promptPulseAmount;
            promptText.rectTransform.localScale = promptScale * pulse;
        }
    }

    void OnDisable()
    {
        if (promptText != null) promptText.rectTransform.anchoredPosition = promptPosition;
    }

    private IEnumerator ShakeRoutine()
    {
        for (float elapsed = 0f; elapsed < ShakeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float strength = 1f - elapsed / ShakeDuration;
            float offset = Mathf.Sin(elapsed * 60f) * ShakeDistance * strength;
            promptText.rectTransform.anchoredPosition = promptPosition + new Vector2(offset, 0f);
            yield return null;
        }

        promptText.rectTransform.anchoredPosition = promptPosition;
    }
}
