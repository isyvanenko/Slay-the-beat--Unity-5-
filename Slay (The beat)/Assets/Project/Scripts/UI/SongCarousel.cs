using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class SongCarousel : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform[] songBlocks; // The 5+ blocks
    public float horizontalSpacing = 350f; // Distance between blocks
    public float sideBlockScale = 0.7f; // Scale for non-active blocks
    public float sideBlockAlpha = 0.5f; // Transparency for side blocks

    [Header("Settings")]
    public float lerpSpeed = 10f;
    public int currentIndex = 0;

    [Header("Audio")]
    public AudioClip switchSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Ensure the CanvasGroup or MusicManager logic from before is integrated if needed
    }

    void Update()
    {
        UpdateCarouselPositions();
    }

    private void UpdateCarouselPositions()
    {
        for (int i = 0; i < songBlocks.Length; i++)
        {
            // Calculate the relative index (distance from center)
            // This logic handles the "carousel" wrapping feel
            int relativeIndex = i - currentIndex;

            // Target Position
            Vector2 targetPos = new Vector2(relativeIndex * horizontalSpacing, 0);
            
            // Smoothly move the block
            songBlocks[i].anchoredPosition = Vector2.Lerp(
                songBlocks[i].anchoredPosition, 
                targetPos, 
                Time.deltaTime * lerpSpeed
            );

            // Scale & Visuals based on distance from center
            float dist = Mathf.Abs(relativeIndex);
            float targetScale = (dist == 0) ? 1.2f : sideBlockScale;
            
            songBlocks[i].localScale = Vector3.Lerp(
                songBlocks[i].localScale, 
                Vector3.one * targetScale, 
                Time.deltaTime * lerpSpeed
            );

            // Optional: Dim blocks that are far away
            CanvasGroup group = songBlocks[i].GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = Mathf.Lerp(group.alpha, (dist == 0) ? 1f : sideBlockAlpha, Time.deltaTime * lerpSpeed);
            }
            
            // Re-order sorting (Center block on top)
            if (dist == 0) songBlocks[i].SetAsLastSibling();
        }
    }

    public void Move(int direction)
    {
        // Simple bounds check (or use modulo for infinite wrapping)
        int nextIndex = currentIndex + direction;
        if (nextIndex >= 0 && nextIndex < songBlocks.Length)
        {
            currentIndex = nextIndex;
            if (audioSource && switchSound) audioSource.PlayOneShot(switchSound);
        }
    }
}