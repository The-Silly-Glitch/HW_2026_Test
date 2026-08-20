using UnityEngine;

/// <summary>
/// Put this on the Pulpit prefab (a 9x9 flat Box with a Collider,
/// tag = "Pulpit"). Each Pulpit manages its own countdown, tells the
/// spawner when it's time to spawn the next one, and destroys/disables
/// itself when its life runs out (dropping Doofus if he's still on it).
/// </summary>
public class Pulpit : MonoBehaviour
{
    [Header("Visuals (optional)")]
    public Renderer pulpitRenderer;
    public Color normalColor = new Color(0.1f, 0.8f, 0.2f); // green
    public Color warningColor = Color.red;

    private float lifetime;          // total seconds this Pulpit will exist (y..z)
    private float spawnNextAtRemaining; // "x" - when remaining time drops to this, ask for next Pulpit
    private float remaining;
    private bool hasNotifiedSpawner;
    private bool hasBeenScored;
    private bool isDestroyed;

    private PulpitSpawner ownerSpawner;
    private Collider col;

    /// <summary>
    /// Direction (in world space) from the previous Pulpit to this one.
    /// Used by PulpitSpawner to avoid spawning the *next* Pulpit straight
    /// back onto the position this one came from. Vector3.zero for the
    /// very first Pulpit, which has no predecessor.
    /// </summary>
    public Vector3 IncomingDirection { get; private set; } = Vector3.zero;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (pulpitRenderer == null) pulpitRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Called by PulpitSpawner right after Instantiate to configure this
    /// Pulpit's lifespan using values derived from the JSON-driven diary data.
    /// </summary>
    public void Initialize(PulpitSpawner spawner, float lifetimeSeconds, float spawnThresholdSeconds, Vector3 incomingDirection)
    {
        ownerSpawner = spawner;
        IncomingDirection = incomingDirection;
        lifetime = Mathf.Max(0.1f, lifetimeSeconds); // guard against 0/negative values
        remaining = lifetime;

        // Edge case: threshold must be less than the total lifetime, otherwise
        // we'd try to spawn the next Pulpit before/at the moment this one exists.
        spawnNextAtRemaining = Mathf.Clamp(spawnThresholdSeconds, 0.1f, lifetime - 0.05f);

        if (pulpitRenderer != null) pulpitRenderer.material.color = normalColor;
    }

    private void Update()
    {
        if (isDestroyed) return;

        remaining -= Time.deltaTime;

        // Simple visual warning as the Pulpit nears the end of its life.
        if (pulpitRenderer != null)
        {
            float t = 1f - Mathf.Clamp01(remaining / lifetime);
            pulpitRenderer.material.color = Color.Lerp(normalColor, warningColor, t);
        }

        if (!hasNotifiedSpawner && remaining <= spawnNextAtRemaining)
        {
            hasNotifiedSpawner = true;
            ownerSpawner?.RequestNextPulpit(this);
        }

        if (remaining <= 0f)
        {
            Expire();
        }
    }

    private void Expire()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Disable collider/visual immediately so Doofus (if standing on it)
        // has nothing left to stand on and falls under gravity.
        if (col != null) col.enabled = false;
        if (pulpitRenderer != null) pulpitRenderer.enabled = false;

        ownerSpawner?.NotifyPulpitExpired(this);

        Destroy(gameObject, 2f); // small delay in case anything still references it this frame
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryScore(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryScore(other);
    }

    private void TryScore(Collider other)
    {
        if (hasBeenScored || isDestroyed) return;
        if (!other.CompareTag("Doofus")) return;

        hasBeenScored = true;
        GameManager.Instance?.AddScore(1);
    }
}
