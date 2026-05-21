using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Implements a horizontally scrolling carousel for song selection UI.
/// </summary>
/// <remarks>
/// This component arranges a set of RectTransform blocks (song thumbnails) in a horizontal line,
/// with the currently selected block centered and scaled up. Adjacent blocks are smaller and dimmer,
/// creating a 3D carousel effect. Movement is smooth with lerp-based animation.
/// 
/// Key Features:
/// - Smooth interpolation of position and scale using Lerp
/// - Configurable spacing, side block scale, and alpha
/// - Optional CanvasGroup support for fading side blocks
/// - Z-ordering: center block rendered on top (SetAsLastSibling)
/// - Audio feedback on navigation
/// - Simple bounds checking (non‑wrapping; can be changed to modulo)
/// </remarks>
public class SongCarousel : MonoBehaviour
{
    [Header("UI Elements")]
    /// <summary>Array of RectTransform blocks representing song items (e.g., 5+ blocks).</summary>
    public RectTransform[] songBlocks;
    
    /// <summary>Horizontal distance (in pixels) between the centers of adjacent blocks.</summary>
    public float horizontalSpacing = 350f;
    
    /// <summary>Scale factor applied to blocks that are not the center (active) block.</summary>
    public float sideBlockScale = 0.7f;
    
    /// <summary>Alpha transparency (0–1) applied to side blocks when a CanvasGroup is present.</summary>
    public float sideBlockAlpha = 0.5f;

    [Header("Settings")]
    /// <summary>Speed of the lerp interpolation for position and scale.</summary>
    public float lerpSpeed = 10f;
    
    /// <summary>Index of the currently centered (selected) block.</summary>
    public int currentIndex = 0;

    [Header("Audio")]
    /// <summary>Sound effect played when moving to another block.</summary>
    public AudioClip switchSound;
    
    /// <summary>AudioSource used to play the switch sound.</summary>
    private AudioSource audioSource;

    /// <summary>
    /// Initializes the AudioSource component reference.
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Ensure the CanvasGroup or MusicManager logic from before is integrated if needed
    }

    /// <summary>
    /// Updates the carousel positions, scales, and alphas every frame.
    /// </summary>
    void Update()
    {
        UpdateCarouselPositions();
    }

    /// <summary>
    /// Calculates and applies the target position, scale, and alpha for each block.
    /// </summary>
    /// <remarks>
    /// For each block:
    /// - Compute relative index (i - currentIndex)
    /// - Target anchored position = (relativeIndex * horizontalSpacing, 0)
    /// - Smoothly move toward that position using Lerp
    /// - Scale is 1.2x for the center block, otherwise sideBlockScale
    /// - If a CanvasGroup exists, alpha is 1 for center, sideBlockAlpha for others
    /// - The center block is moved to the end of the sibling order so it renders on top
    /// </remarks>
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

    /// <summary>
    /// Moves the carousel one step left or right.
    /// </summary>
    /// <param name="direction">-1 for previous block, +1 for next block.</param>
    /// <remarks>
    /// The method uses simple bounds checking to prevent moving outside the array.
    /// To enable infinite wrapping, replace the condition with modulo logic.
    /// </remarks>
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