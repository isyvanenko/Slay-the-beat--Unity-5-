using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages character animation playback with queuing support for sequential animations.
/// </summary>
/// <remarks>
/// This controller allows triggering animations by direction and automatically queues them
/// to play in sequence without overlapping. It selects random variants of each direction
/// animation to create variety in character movements.
/// 
/// Key Features:
/// - Animation queuing system for sequential playback
/// - Random variant selection for directional animations
/// - Automatic transition to Idle when queue completes
/// - Robust state detection with timeout handling
/// - Snapshot animation playback (no crossfade)
/// 
/// This is particularly useful for:
/// - Turn-based games where multiple moves are queued
/// - Dialogue systems with character gestures
/// - Fighting games with combo systems
/// - Any scenario requiring non-interrupting animation sequences
/// </remarks>
public class CharacterAnimationController : MonoBehaviour
{
    [Header("Setup")]

    /// <summary>
    /// Reference to the Animator component controlling character animations.
    /// </summary>
    /// <remarks>
    /// Must be assigned in the inspector or found automatically.
    /// The Animator should contain animation states named like "Left1", "Left2", "Left3",
    /// "Right1", "Right2", etc. as well as an "Idle" state.
    /// </remarks>
    public Animator animator;

    /// <summary>
    /// Queue that stores animation state names waiting to be played.
    /// </summary>
    /// <remarks>
    /// First-in-first-out structure ensuring animations play in the order they were triggered.
    /// New animations are enqueued, and ProcessAnimationQueue dequeues them sequentially.
    /// </remarks>
    private Queue<string> animationQueue = new Queue<string>();

    /// <summary>
    /// Flag indicating whether the animation queue processor is currently running.
    /// </summary>
    /// <remarks>
    /// Prevents multiple coroutine instances from processing the queue simultaneously.
    /// Set to true when processing starts and false when the queue is empty.
    /// </remarks>
    private bool isRunningQueue = false;

    /// <summary>
    /// Triggers a directional animation with a random variant and queues it for playback.
    /// </summary>
    /// <param name="direction">The direction name (e.g., "Left", "Right", "Up", "Down").</param>
    /// <remarks>
    /// This method generates a random variant number between 1-3 and creates a state name
    /// like "Left1" or "Right2". The state name is then added to the queue. If the queue
    /// processor isn't running, it starts the coroutine to begin processing.
    /// 
    /// Important: The animation state names in your Animator Controller MUST match this
    /// naming convention exactly. For each direction, you need variants 1, 2, and 3.
    /// 
    /// Example Animator states needed:
    /// - Left1, Left2, Left3
    /// - Right1, Right2, Right3
    /// - Forward1, Forward2, Forward3
    /// - Back1, Back2, Back3
    /// </remarks>
    public void TriggerAnimation(string direction)
    {
        // 1. Name the state (Make sure these match your Animator exactly!)
        int randomVariant = Random.Range(1, 4);
        string stateName = direction + randomVariant; // e.g., "Left1"

        // 2. Add to Queue
        animationQueue.Enqueue(stateName);

        // 3. Start the processor if it's asleep
        if (!isRunningQueue)
        {
            StartCoroutine(ProcessAnimationQueue());
        }
    }

    /// <summary>
    /// Processes the animation queue, playing each animation sequentially.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// This coroutine runs until the animation queue is empty. For each animation:
    /// 1. Dequeues the next animation state name
    /// 2. Instantly plays the animation using Animator.Play (snapshot, no crossfade)
    /// 3. Waits for the Animator to actually enter the requested state (with timeout)
    /// 4. Retrieves the clip length and waits for the full duration
    /// 5. Continues to the next queued animation
    /// 
    /// After all animations are played, it transitions to "Idle" and stops processing.
    /// 
    /// The timeout mechanism (0.2 seconds) prevents the system from hanging if the
    /// requested animation state cannot be found or entered. The edge case handling
    /// ensures animations with zero or invalid length still play for a reasonable
    /// default duration (0.5 seconds).
    /// </remarks>
    IEnumerator ProcessAnimationQueue()
    {
        isRunningQueue = true;

        while (animationQueue.Count > 0)
        {
            // A. Get the next animation
            string nextAnim = animationQueue.Dequeue();

            // B. Tell Animator to switch NOW
            // We use Play to snap instantly (no crossfade blending)
            animator.Play(nextAnim, 0, 0f);

            // --- CRITICAL FIX: WAIT FOR STATE SWITCH ---
            // We must wait until the Animator is actually PLAYING 'nextAnim'
            // otherwise 'GetCurrentAnimatorStateInfo' gives us the OLD info.

            float timeout = 0f;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName(nextAnim) && timeout < 0.2f)
            {
                // Wait for the engine to catch up
                yield return null;
                timeout += Time.deltaTime;
            }

            // Safety Check: Did we find it?
            if (timeout >= 0.2f)
            {
                // Skip to next item to prevent freezing
                continue;
            }

            // C. Now that we are definitely in the right state, get its length
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            float duration = info.length;

            // Handle edge case where length is reported as 0 or infinity
            if (duration <= 0 || float.IsInfinity(duration)) duration = 0.5f;

            // D. Wait for the duration of the clip
            yield return new WaitForSeconds(duration);
        }

        // E. Queue is empty
        // Transition back to idle state when all animations are complete
        animator.Play("Idle");
        isRunningQueue = false;
    }
}