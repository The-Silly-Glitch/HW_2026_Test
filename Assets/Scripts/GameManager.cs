using UnityEngine;

public enum GameState
{
    StartScreen,
    Playing,
    GameOver
}

/// <summary>
/// Central brain of the game. Holds current state, score, and the
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

    [Header("Scene References")]
    public UIManager uiManager;
    public PulpitSpawner pulpitSpawner;
    public Transform doofusStartPosition; // empty GameObject marking spawn point
    public GameObject doofusPrefab;

    private GameObject currentDoofusInstance;

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

    public void ShowStartScreen()
    {
        CurrentState = GameState.StartScreen;
        Score = 0;

        if (pulpitSpawner != null) pulpitSpawner.StopAllPulpits();
        if (currentDoofusInstance != null) Destroy(currentDoofusInstance);

        if (uiManager != null) uiManager.ShowStartScreen();
    }

    public void StartGame()
    {
        Score = 0;
        CurrentState = GameState.Playing;

        if (uiManager != null) uiManager.ShowGameplayHud(Score);

        // Spawn a fresh Doofus at the marked start point.
        if (doofusPrefab != null && doofusStartPosition != null)
        {
            currentDoofusInstance = Instantiate(doofusPrefab, doofusStartPosition.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("[GameManager] doofusPrefab or doofusStartPosition not assigned in Inspector.");
        }

        if (pulpitSpawner != null)
        {
            pulpitSpawner.BeginSpawning(doofusStartPosition.position);
        }
    }

    public void AddScore(int amount)
    {
        if (CurrentState != GameState.Playing) return; // ignore late/duplicate calls

        Score += amount;
        if (uiManager != null) uiManager.UpdateScore(Score);
    }

    public void EndGame()
    {
        if (CurrentState != GameState.Playing) return; // avoid double-trigger edge case

        CurrentState = GameState.GameOver;

        if (pulpitSpawner != null) pulpitSpawner.StopAllPulpits();
        if (uiManager != null) uiManager.ShowGameOverScreen(Score);
    }

    public void RestartGame()
    {
        if (currentDoofusInstance != null) Destroy(currentDoofusInstance);
        if (pulpitSpawner != null) pulpitSpawner.ResetSpawner();

        StartGame();
    }
}
