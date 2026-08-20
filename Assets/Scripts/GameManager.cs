using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    StartScreen,
    Playing,
    Paused,
    Respawning, // Doofus fell, lost a life, and is being dropped back onto his last Pulpit
    GameOver
}

/// <summary>
/// Central brain of the game. Holds current state, score, lives, and the
/// loaded Doofus Diary config so every other script can read from
/// one place instead of re-reading the JSON everywhere.
/// Attach this to an empty GameObject called "GameManager" in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.StartScreen;
    public DoofusDiaryData DiaryData { get; private set; }
    public int Score { get; private set; }
    public int CurrentLives { get; private set; }

    [Header("Scene References")]
    public UIManager uiManager;
    public PulpitSpawner pulpitSpawner;
    public Transform doofusStartPosition; // empty GameObject marking spawn point
    public GameObject doofusPrefab;
    public CameraFollow cameraFollow; // optional - drag Main Camera here if using camera-follow

    [Header("Lives & Respawn")]
    [Tooltip("How many times Doofus can fall before it's actually Game Over.")]
    public int startingLives = 3;
    [Tooltip("World-space Y height Doofus teleports to before falling back onto his last Pulpit.")]
    public float respawnHeight = 10f;

    private GameObject currentDoofusInstance;
    private Pulpit currentPulpit; // last Pulpit Doofus actually touched - the respawn target if he falls

    private void Awake()
    {
        // Simple singleton so DoofusController / Pulpit scripts can reach this
        // without manually wiring references everywhere in the Inspector.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DiaryData = DoofusDiaryLoader.Load();
    }

    private void Start()
    {
        ShowStartScreen();
    }

    private void Update()
    {
        // --- Escape toggles Pause/Resume. Guarded internally by
        // PauseGame()/ResumeGame() so pressing it on the Start Screen,
        // Game Over screen, or mid-respawn does nothing. ---
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.Playing) PauseGame();
            else if (CurrentState == GameState.Paused) ResumeGame();
        }
    }

    public void ShowStartScreen()
    {
        CurrentState = GameState.StartScreen;
        Score = 0;
        Time.timeScale = 1f; // defensive reset in case we somehow got here while paused

        if (pulpitSpawner != null) pulpitSpawner.StopAllPulpits();
        if (currentDoofusInstance != null) Destroy(currentDoofusInstance);
        if (cameraFollow != null) cameraFollow.ClearTarget();
        currentPulpit = null;

        if (uiManager != null) uiManager.ShowStartScreen();
    }

    public void StartGame()
    {
        Score = 0;
        CurrentLives = startingLives;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (uiManager != null) uiManager.ShowGameplayHud(Score, CurrentLives);

        // Spawn a fresh Doofus at the marked start point.
        if (doofusPrefab != null && doofusStartPosition != null)
        {
            currentDoofusInstance = Instantiate(doofusPrefab, doofusStartPosition.position, Quaternion.identity);
            if (cameraFollow != null) cameraFollow.SetTarget(currentDoofusInstance.transform);
        }
        else
        {
            Debug.LogError("[GameManager] doofusPrefab or doofusStartPosition not assigned in Inspector.");
        }

        currentPulpit = null;
        if (pulpitSpawner != null && doofusStartPosition != null)
        {
            currentPulpit = pulpitSpawner.BeginSpawning(doofusStartPosition.position);
        }
    }

    public void AddScore(int amount)
    {
        if (CurrentState != GameState.Playing) return; // ignore late/duplicate calls

        Score += amount;
        if (uiManager != null) uiManager.UpdateScore(Score);
    }

    /// <summary>
    /// Called by any Pulpit whenever Doofus makes contact with it -
    /// this is how GameManager always knows the last Pulpit actually
    /// stood on (needed as the respawn target), and how a completed
    /// respawn hands control back to the player.
    /// </summary>
    public void NotifyDoofusLanded(Pulpit pulpit)
    {
        if (pulpit == null) return;

        if (CurrentState == GameState.Playing)
        {
            currentPulpit = pulpit;
        }
        else if (CurrentState == GameState.Respawning && pulpit == currentPulpit)
        {
            // Doofus has landed back on the exact Pulpit he was sent to -
            // give it a fresh full countdown and hand control back.
            pulpit.ResetTimerAndUnfreeze();
            CurrentState = GameState.Playing;
        }
    }

    /// <summary>
    /// Called by DoofusController when he falls off the map. Loses a
    /// life; either ends the game (no lives left) or kicks off the
    /// fall-and-respawn sequence back onto his last Pulpit.
    /// </summary>
    public void HandleDoofusFell()
    {
        if (CurrentState != GameState.Playing) return; // avoid double-trigger edge case

        CurrentLives--;
        if (uiManager != null) uiManager.UpdateLives(CurrentLives);

        if (CurrentLives <= 0)
        {
            EndGame();
            return;
        }

        RespawnDoofus();
    }

    private void RespawnDoofus()
    {
        // --- Edge case: if for some reason there's no valid Pulpit or
        // Doofus instance to respawn (shouldn't normally happen), fail
        // gracefully into Game Over instead of soft-locking. ---
        if (currentPulpit == null || currentDoofusInstance == null)
        {
            Debug.LogWarning("[GameManager] No valid Pulpit/Doofus to respawn onto - ending game.");
            EndGame();
            return;
        }

        CurrentState = GameState.Respawning;
        currentPulpit.Freeze(); // pause its countdown while Doofus is away

        Vector3 pulpitPos = currentPulpit.transform.position;
        Vector3 respawnPos = new Vector3(pulpitPos.x, respawnHeight, pulpitPos.z);

        DoofusController controller = currentDoofusInstance.GetComponent<DoofusController>();
        if (controller != null)
        {
            controller.TeleportTo(respawnPos);
        }
        // From here, gravity takes over automatically (Doofus's Rigidbody
        // still simulates physics even though input is ignored while
        // CurrentState != Playing). Landing is detected by NotifyDoofusLanded.
    }

    public void EndGame()
    {
        if (CurrentState == GameState.GameOver) return; // avoid double-trigger edge case

        CurrentState = GameState.GameOver;
        Time.timeScale = 1f;

        if (pulpitSpawner != null) pulpitSpawner.StopAllPulpits();
        if (uiManager != null) uiManager.ShowGameOverScreen(Score);
    }

    public void RestartGame()
    {
        if (currentDoofusInstance != null) Destroy(currentDoofusInstance);
        if (pulpitSpawner != null) pulpitSpawner.ResetSpawner();

        StartGame();
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f; // freezes physics + Pulpit countdowns + Doofus input for free

        if (uiManager != null) uiManager.ShowResumeScreen();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (uiManager != null) uiManager.ShowGameplayHud(Score, CurrentLives);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}