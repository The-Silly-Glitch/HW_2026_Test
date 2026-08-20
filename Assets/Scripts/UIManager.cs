using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a Canvas GameObject. Wire up the panels/text/buttons in the
/// Inspector (see the guide for the exact hierarchy to build).
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;
    public GameObject resumePanel;

    [Header("Start Panel")]
    public Button startButton;
    public Button startExitButton;

    [Header("HUD")]
    public Text scoreText; // swap for TMP_Text if you use TextMeshPro
    public Text livesText;

    [Header("Game Over Panel")]
    public Text finalScoreText;
    public Button restartButton;
    public Button gameOverExitButton;

    [Header("Resume Panel")]
    public Button resumeButton;
    public Button resumeExitButton;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);

        // Every screen gets the same Exit behavior - wired once here so
        // there's a single source of truth for what "Exit" does.
        if (startExitButton != null) startExitButton.onClick.AddListener(OnExitClicked);
        if (gameOverExitButton != null) gameOverExitButton.onClick.AddListener(OnExitClicked);
        if (resumeExitButton != null) resumeExitButton.onClick.AddListener(OnExitClicked);
    }

    public void ShowStartScreen()
    {
        SetPanels(start: true, hud: false, gameOver: false, resume: false);
    }

    public void ShowGameplayHud(int score, int lives)
    {
        SetPanels(start: false, hud: true, gameOver: false, resume: false);
        UpdateScore(score);
        UpdateLives(lives);
    }

    public void ShowResumeScreen()
    {
        SetPanels(start: false, hud: false, gameOver: false, resume: true);
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null) livesText.text = $"Lives: {Mathf.Max(0, lives)}";
    }

    public void ShowGameOverScreen(int finalScore)
    {
        SetPanels(start: false, hud: false, gameOver: true, resume: false);
        if (finalScoreText != null) finalScoreText.text = $"Game Over\nPulpits walked: {finalScore}";
    }

    private void SetPanels(bool start, bool hud, bool gameOver, bool resume)
    {
        if (startPanel != null) startPanel.SetActive(start);
        if (hudPanel != null) hudPanel.SetActive(hud);
        if (gameOverPanel != null) gameOverPanel.SetActive(gameOver);
        if (resumePanel != null) resumePanel.SetActive(resume);
    }

    private void OnStartClicked() => GameManager.Instance?.StartGame();
    private void OnRestartClicked() => GameManager.Instance?.RestartGame();
    private void OnResumeClicked() => GameManager.Instance?.ResumeGame();
    private void OnExitClicked() => GameManager.Instance?.QuitGame();
}