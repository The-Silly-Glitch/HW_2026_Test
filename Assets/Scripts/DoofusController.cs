using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the Doofus prefab (a Cube with a Rigidbody + BoxCollider).
/// Movement speed is NOT hardcoded - it is pulled from GameManager.DiaryData,
/// which was itself loaded from the JSON file, per the assignment spec.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DoofusController : MonoBehaviour
{
    [Tooltip("How far below the last known Pulpit height counts as 'fallen off'.")]
    public float fallDeathY = -10f;

    [Tooltip("How fast Doofus visually turns to face his movement direction.")]
    public float turnSpeed = 10f;

    /// <summary>
    /// True while there's active movement input this physics step.
    /// Read by SlimeAnimationController to drive the Idle/Move blend -
    /// kept here since this is the single source of truth for movement.
    /// </summary>
    public bool IsMoving { get; private set; }

    private Rigidbody rb;
    private float moveSpeed = 5f; // overwritten from JSON in Start()

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze only the axes that would make Doofus topple over from physics
        // collisions - leave Y (yaw) free so he can turn to face movement direction.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        // --- Edge case: GameManager might not exist yet if this script is
        // tested in isolation. Guard against a null reference crash. ---
        if (GameManager.Instance != null && GameManager.Instance.DiaryData != null)
        {
            moveSpeed = GameManager.Instance.DiaryData.doofusSpeed;
        }
        else
        {
            Debug.LogWarning("[DoofusController] No GameManager/DiaryData found, using default speed.");
        }
    }

    private void Update()
    {
        // Only respond to input while the game is actually being played.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        // --- Fall / edge-case death check ---
        // If Doofus has dropped far below the play area (fell off a Pulpit,
        // or the Pulpit he was standing on was destroyed), end the game.
        if (transform.position.y < fallDeathY)
        {
            if (GameManager.Instance != null) GameManager.Instance.EndGame();
            return;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        // Works with both WASD and Arrow keys, using the new Input System
        // directly (avoids depending on the project's Active Input Handling
        // setting, which caused issues before).
        var kb = Keyboard.current;
        if (kb == null) return; // edge case: no keyboard device detected

        float h = 0f, v = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;

        Vector3 move = new Vector3(h, 0f, v);
        if (move.sqrMagnitude > 1f) move.Normalize(); // stop diagonal speed boost

        IsMoving = move.sqrMagnitude > 0.0001f;

        Vector3 targetPos = rb.position + move * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);

        // Smoothly turn to face the direction Doofus is actually moving in.
        // Skipped entirely when standing still so he doesn't snap to face
        // some leftover direction while idle.
        if (IsMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Alternate, more explicit death trigger: a dedicated "DeathZone"
        // trigger volume placed well below the Pulpits (see the guide).
        if (collision.collider.CompareTag("DeathZone"))
        {
            if (GameManager.Instance != null) GameManager.Instance.EndGame();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            if (GameManager.Instance != null) GameManager.Instance.EndGame();
        }
    }
}