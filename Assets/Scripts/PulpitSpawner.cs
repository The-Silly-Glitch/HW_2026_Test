using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to an empty GameObject called "PulpitSpawner".
/// Responsible for: keeping at most 2 Pulpits alive at once, spawning a
/// new one adjacent to (but not on top of) the previous one, and pulling
/// all timing values from the JSON-driven Doofus Diary data.
/// </summary>
public class PulpitSpawner : MonoBehaviour
{
    [Header("Prefab & Sizing")]
    public GameObject pulpitPrefab;
    public float pulpitSize = 9f; // matches the 9x9 Pulpit footprint

    [Header("Rules")]
    public int maxConcurrentPulpits = 2;

    private readonly List<Pulpit> activePulpits = new List<Pulpit>();
    private Vector3 lastSpawnPosition;
    private Vector3 lastDirection = Vector3.forward;
    private bool isRunning;

    // --- Handles the case where a Pulpit hits its spawn-threshold while
    // the cap (2) is still full because the other active Pulpit hasn't
    // expired yet. Without this, that request would be silently dropped
    // and no further Pulpit would ever spawn, since each Pulpit only
    // requests once. ---
    private bool hasPendingRequest;
    private Vector3 pendingRequestPosition;
    private Vector3 pendingExcludeDirection;

    private static readonly Vector3[] CardinalDirections =
    {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right
    };

    /// <summary>
    /// Called once by GameManager.StartGame(). Spawns the very first
    /// Pulpit under Doofus's feet, then lets the chain reaction begin.
    /// </summary>
    public Pulpit BeginSpawning(Vector3 startPosition)
    {
        ResetSpawner();
        isRunning = true;

        lastSpawnPosition = startPosition;
        return SpawnPulpitAt(lastSpawnPosition, Vector3.zero); // no predecessor, so no incoming direction
    }

    public void StopAllPulpits()
    {
        isRunning = false;
    }

    public void ResetSpawner()
    {
        foreach (var p in activePulpits)
        {
            if (p != null) Destroy(p.gameObject);
        }
        activePulpits.Clear();
        isRunning = false;
        hasPendingRequest = false;
    }

    /// <summary>
    /// A Pulpit calls this on itself-nearing-death (when remaining time
    /// hits the JSON-derived "x" threshold) to request the next one spawn.
    /// </summary>
    public void RequestNextPulpit(Pulpit requester)
    {
        if (!isRunning) return;

        Vector3 basePos = requester != null ? requester.transform.position : lastSpawnPosition;
        Vector3 excludeDirection = requester != null ? requester.IncomingDirection : Vector3.zero;

        // --- Edge case: respect the "only two at once" rule. If we're
        // already full, don't drop this request - remember it so it can
        // be fulfilled the instant a slot frees up (see NotifyPulpitExpired). ---
        if (activePulpits.Count >= maxConcurrentPulpits)
        {
            hasPendingRequest = true;
            pendingRequestPosition = basePos;
            pendingExcludeDirection = excludeDirection;
            return;
        }

        SpawnNextFrom(basePos, excludeDirection);
    }

    public void NotifyPulpitExpired(Pulpit pulpit)
    {
        activePulpits.Remove(pulpit);

        // A slot just freed up - if some earlier Pulpit was blocked from
        // spawning its successor because we were at capacity, fulfill it now.
        if (hasPendingRequest && isRunning)
        {
            hasPendingRequest = false;
            SpawnNextFrom(pendingRequestPosition, pendingExcludeDirection);
        }
    }

    /// <summary>
    /// Returns the most recently spawned still-active Pulpit (last one
    /// added to the list, since Pulpits are always appended in spawn
    /// order). Used by GameManager as the respawn target - the freshest
    /// Pulpit always gives the player the most time to react.
    /// </summary>
    public Pulpit GetLatestPulpit()
    {
        if (activePulpits.Count == 0) return null;
        return activePulpits[activePulpits.Count - 1];
    }

    /// <summary>
    /// Force-expires every active Pulpit except the one given. Called
    /// right before a respawn so the player only ever has ONE Pulpit to
    /// worry about landing back on - no more accidentally respawning onto
    /// a stale, about-to-expire Pulpit while a fresher one sits unused.
    /// </summary>
    public void ClearAllExcept(Pulpit keep)
    {
        // Snapshot first: Pulpit.Expire() calls back into NotifyPulpitExpired()
        // above, which mutates activePulpits - can't safely iterate the live list.
        var snapshot = new List<Pulpit>(activePulpits);
        foreach (var p in snapshot)
        {
            if (p == null || p == keep) continue;
            p.Expire();
        }
    }

    private void SpawnNextFrom(Vector3 basePos, Vector3 excludeDirection)
    {
        Vector3 chosenDirection = PickDirection(basePos, excludeDirection);
        Vector3 nextPos = basePos + chosenDirection * pulpitSize;
        SpawnPulpitAt(nextPos, chosenDirection);
    }

    private Pulpit SpawnPulpitAt(Vector3 position, Vector3 directionUsed)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitSpawner] pulpitPrefab not assigned in Inspector.");
            return null;
        }

        GameObject go = Instantiate(pulpitPrefab, position, Quaternion.identity);
        Pulpit pulpit = go.GetComponent<Pulpit>();
        if (pulpit == null)
        {
            Debug.LogError("[PulpitSpawner] pulpitPrefab has no Pulpit component attached.");
            Destroy(go);
            return null;
        }

        DoofusDiaryData diary = GameManager.Instance != null ? GameManager.Instance.DiaryData : null;
        float minLife = diary != null ? diary.minPulpitLifetime : 3f;
        float maxLife = diary != null ? diary.maxPulpitLifetime : 6f;
        // Fixed, JSON-driven threshold - "x" seconds remaining triggers the next spawn.
        float spawnThreshold = diary != null ? diary.spawnThresholdSeconds : 1.5f;

        float lifetime = Random.Range(minLife, maxLife);

        pulpit.Initialize(this, lifetime, spawnThreshold, directionUsed);

        activePulpits.Add(pulpit);
        lastSpawnPosition = position;
        lastDirection = directionUsed;

        return pulpit;
    }

    /// <summary>
    /// Picks a cardinal direction for the next Pulpit, given the position
    /// it's spawning from and the direction that led INTO that position
    /// (so we can exclude going straight back the way we came - this is
    /// what stops a new Pulpit landing on the previous Pulpit's old spot).
    /// Also avoids landing on any currently-active Pulpit as a backup check.
    /// </summary>
    private Vector3 PickDirection(Vector3 fromPosition, Vector3 excludeDirection)
    {
        Vector3 reverseOfExclude = -excludeDirection;

        List<Vector3> candidates = new List<Vector3>();
        foreach (var dir in CardinalDirections)
        {
            // Skip the direction that would take us straight back to
            // where this Pulpit's predecessor was.
            if (excludeDirection != Vector3.zero && Vector3.Distance(dir, reverseOfExclude) < 0.01f)
                continue;

            Vector3 candidatePos = fromPosition + dir * pulpitSize;
            if (IsPositionOccupied(candidatePos)) continue;

            candidates.Add(dir);
        }

        // --- Edge case: if every direction got filtered out (shouldn't
        // normally happen with 4 directions and only 1-2 active Pulpits),
        // fall back to allowing the exclusion rule to be broken rather
        // than getting stuck with no candidates at all. ---
        if (candidates.Count == 0)
        {
            foreach (var dir in CardinalDirections)
            {
                Vector3 candidatePos = fromPosition + dir * pulpitSize;
                if (!IsPositionOccupied(candidatePos)) candidates.Add(dir);
            }
        }

        if (candidates.Count == 0)
        {
            // Absolute last resort - everything is somehow occupied.
            return lastDirection;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool IsPositionOccupied(Vector3 candidate)
    {
        const float tolerance = 0.5f;
        foreach (var p in activePulpits)
        {
            if (p == null) continue;
            if (Vector3.Distance(p.transform.position, candidate) < tolerance) return true;
        }
        return false;
    }
}