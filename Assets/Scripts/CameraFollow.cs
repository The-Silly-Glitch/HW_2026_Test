using UnityEngine;

/// <summary>
/// Attach to the Main Camera. GameManager calls SetTarget() with the
/// newly-spawned Doofus instance each time a run starts, since Doofus
/// doesn't exist in the scene until runtime (he's spawned from a prefab).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Position of the camera relative to the target.")]
    public Vector3 offset = new Vector3(0f, 12f, -12f);

    [Tooltip("Higher = snappier, lower = smoother/laggier.")]
    public float followSpeed = 5f;

    [Tooltip("If true, camera keeps looking at the target every frame.")]
    public bool lookAtTarget = true;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // Snap immediately on assignment so the camera doesn't slide in
        // from wherever it was left after the previous run/Game Over.
        if (target != null)
        {
            transform.position = target.position + offset;
            if (lookAtTarget) transform.LookAt(target);
        }
    }

    public void ClearTarget()
    {
        target = null;
    }

    private void LateUpdate()
    {
        // --- Edge case: target may be null before a run starts, or after
        // Doofus is destroyed on Game Over/Restart. Just hold position. ---
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        if (lookAtTarget) transform.LookAt(target);
    }
}
