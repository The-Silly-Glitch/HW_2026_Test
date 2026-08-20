using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on the SlimeModel child object (the one with the Animator
/// component from the imported FBX). Reads movement state from
/// DoofusController on the parent and drives the Animator Controller's
/// "IsMoving" bool and "IdleBreak" trigger accordingly.
/// </summary>
public class SlimeAnimationController : MonoBehaviour
{
    [Tooltip("Animator on this same object (auto-found if left empty).")]
    public Animator animator;

    [Tooltip("DoofusController on the parent object (auto-found if left empty).")]
    public DoofusController doofusController;

    [Header("Idle Break Timing")]
    [Tooltip("Random delay range (seconds) between idle-break quirks while standing still.")]
    public float idleBreakMinDelay = 4f;
    public float idleBreakMaxDelay = 9f;

    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    private static readonly int IdleBreakParam = Animator.StringToHash("IdleBreak");

    private Coroutine idleBreakRoutine;

    private void Reset()
    {
        // Convenience auto-wiring the moment this component is added in the Editor.
        if (animator == null) animator = GetComponent<Animator>();
        if (doofusController == null) doofusController = GetComponentInParent<DoofusController>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (doofusController == null) doofusController = GetComponentInParent<DoofusController>();

        // --- Edge case: missing references shouldn't crash the game,
        // just silently disable animation driving and log why. ---
        if (animator == null)
            Debug.LogWarning("[SlimeAnimationController] No Animator found on this object.");
        if (doofusController == null)
            Debug.LogWarning("[SlimeAnimationController] No DoofusController found on parent.");
    }

    private void OnEnable()
    {
        idleBreakRoutine = StartCoroutine(IdleBreakLoop());
    }

    private void OnDisable()
    {
        if (idleBreakRoutine != null) StopCoroutine(idleBreakRoutine);
    }

    private void Update()
    {
        if (animator == null || doofusController == null) return;
        animator.SetBool(IsMovingParam, doofusController.IsMoving);
    }

    /// <summary>
    /// Fires the one-shot Idle_break animation at random intervals,
    /// but only while Doofus is actually standing still - the Animator
    /// transition for IsMoving=true will still interrupt it cleanly if
    /// the player starts moving mid-quirk (see the Idle_break->Move transition).
    /// </summary>
    private IEnumerator IdleBreakLoop()
    {
        while (true)
        {
            float delay = Random.Range(idleBreakMinDelay, idleBreakMaxDelay);
            yield return new WaitForSeconds(delay);

            if (animator != null && doofusController != null && !doofusController.IsMoving)
            {
                animator.SetTrigger(IdleBreakParam);
            }
        }
    }
}
