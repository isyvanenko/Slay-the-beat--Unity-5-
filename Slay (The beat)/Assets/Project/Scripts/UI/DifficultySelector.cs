using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class DifficultySelector : MonoBehaviour
{
    [Header("Song Header Info")]
    public TextMeshProUGUI songNameText;

    [Header("UI Box Elements")]
    public RectTransform[] difficultyBoxes; 
    public Image[] outlines;
    public TextMeshProUGUI[] stepCountTexts; 

    [Header("Visual Settings")]
    public float selectedScale = 1.15f;
    public float animSpeed = 10f;

    private int index = 1; 
    private bool hasSelected = false;
    private CanvasGroup canvasGroup;
    private SongGradeData currentSong;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    public void ShowMenu()
    {
        currentSong = GameDataBridge.SelectedSong;
        if (songNameText != null && currentSong != null) songNameText.text = currentSong.songName;
        UpdateAllStepDisplays();

        index = 1;
        hasSelected = false;
        
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    void Update()
    {
        if (hasSelected || canvasGroup.alpha < 0.9f) return;

        // --- HARDWIRED POLLING ---
        // This checks the hardware directly, bypassing your InputActions asset.
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || 
            (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame))
        {
            Move(-1);
        }
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || 
            (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame))
        {
            Move(1);
        }
        if (Keyboard.current.enterKey.wasPressedThisFrame || 
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            Confirm();
        }

        // --- VISUALS ---
        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            bool isCurrent = (i == index);
            difficultyBoxes[i].localScale = Vector3.Lerp(difficultyBoxes[i].localScale, 
                Vector3.one * (isCurrent ? selectedScale : 1f), Time.unscaledDeltaTime * animSpeed);

            if (outlines[i] != null)
                outlines[i].color = isCurrent ? Color.yellow : Color.black;
        }
    }

    void Move(int dir)
    {
        int next = Mathf.Clamp(index + dir, 0, difficultyBoxes.Length - 1);
        if (next != index)
        {
            index = next;
            // Play sound here if you have an AudioSource
        }
    }

    void Confirm()
    {
        hasSelected = true;
        GameDataBridge.SelectedDifficulty = index;
        SceneManager.LoadScene("GameplayScene");
    }

    void UpdateAllStepDisplays()
    {
        if (currentSong == null) return;
        stepCountTexts[0].text = currentSong.easySteps.ToString();
        stepCountTexts[1].text = currentSong.mediumSteps.ToString();
        stepCountTexts[2].text = currentSong.hardSteps.ToString();
    }

    IEnumerator FadeInRoutine()
    {
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime * 4f;
            yield return null;
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}