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

    private static readonly Vector3[] CardinalDirections =
    {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right
    };

    /// <summary>
    /// Called once by GameManager.StartGame(). Spawns the very first
    /// Pulpit under Doofus's feet, then lets the chain reaction begin.
    /// </summary>
    public void BeginSpawning(Vector3 startPosition)
    {
        ResetSpawner();
        isRunning = true;

        lastSpawnPosition = startPosition;
        SpawnPulpitAt(lastSpawnPosition);
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
    }

    /// <summary>
    /// A Pulpit calls this on itself-nearing-death (when remaining time
    /// hits the JSON-derived "x" threshold) to request the next one spawn.
    /// </summary>
    public void RequestNextPulpit(Pulpit requester)
    {
        if (!isRunning) return;

        // --- Edge case: respect the "only two at once" rule even if
        // something calls this more than expected. ---
        if (activePulpits.Count >= maxConcurrentPulpits) return;

        Vector3 basePos = requester != null ? requester.transform.position : lastSpawnPosition;
        Vector3 nextPos = GetNextAdjacentPosition(basePos);
        SpawnPulpitAt(nextPos);
    }

    public void NotifyPulpitExpired(Pulpit pulpit)
    {
        activePulpits.Remove(pulpit);
    }

    private void SpawnPulpitAt(Vector3 position)
    {
        if (pulpitPrefab == null)
        {
            Debug.LogError("[PulpitSpawner] pulpitPrefab not assigned in Inspector.");
            return;
        }

        GameObject go = Instantiate(pulpitPrefab, position, Quaternion.identity);
        Pulpit pulpit = go.GetComponent<Pulpit>();
        if (pulpit == null)
        {
            Debug.LogError("[PulpitSpawner] pulpitPrefab has no Pulpit component attached.");
            Destroy(go);
            return;
        }

        DoofusDiaryData diary = GameManager.Instance != null ? GameManager.Instance.DiaryData : null;
        float minLife = diary != null ? diary.minPulpitLifetime : 3f;
        float maxLife = diary != null ? diary.maxPulpitLifetime : 6f;

        float lifetime = Random.Range(minLife, maxLife);
        // Per the brief: "x is a random number between y and z seconds" -
        // the spawn-ahead threshold is itself randomized within the same range.
        float spawnThreshold = Random.Range(minLife, maxLife);

        pulpit.Initialize(this, lifetime, spawnThreshold);

        activePulpits.Add(pulpit);
        lastSpawnPosition = position;
    }

    private Vector3 GetNextAdjacentPosition(Vector3 fromPosition)
    {
        // Try a handful of random directions; avoid re-using the exact
        // same spot as any currently active Pulpit ("not in the same position").
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3 dir = CardinalDirections[Random.Range(0, CardinalDirections.Length)];
            Vector3 candidate = fromPosition + dir * pulpitSize;

            if (!IsPositionOccupied(candidate))
            {
                lastDirection = dir;
                return candidate;
            }
        }

        // Fallback: nudge slightly so we never get stuck in an infinite loop.
        return fromPosition + lastDirection * pulpitSize + Vector3.right * 0.01f;
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
