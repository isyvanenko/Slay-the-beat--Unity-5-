using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterAnimationController : MonoBehaviour
{
    [Header("Setup")]
    public Animator animator;

    // Internal Queue
    private Queue<string> animationQueue = new Queue<string>();
    private bool isRunningQueue = false;

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

    IEnumerator ProcessAnimationQueue()
    {
        isRunningQueue = true;

        while (animationQueue.Count > 0)
        {
            // A. Get the next animation
            string nextAnim = animationQueue.Dequeue();
            

            // B. Tell Animator to switch NOW
            // We use Play to snap instantly
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
        
        animator.Play("Idle");
        isRunningQueue = false;
    }
}