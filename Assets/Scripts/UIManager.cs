using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a Canvas GameObject. Wire up the panels/text/buttons in the
/// Inspector (see the guide for the exact hierarchy to build).
/// Covers Level 3: Start Screen + Game Over Screen.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;

    [Header("Start Panel")]
    public Button startButton;

    [Header("HUD")]
    public Text scoreText; // swap for TMP_Text if you use TextMeshPro

    [Header("Game Over Panel")]
    public Text finalScoreText;
    public Button restartButton;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
    }

    public void ShowStartScreen()
    {
        SetPanels(start: true, hud: false, gameOver: false);
    }

    public void ShowGameplayHud(int startingScore)
    {
        SetPanels(start: false, hud: true, gameOver: false);
        UpdateScore(startingScore);
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    public void ShowGameOverScreen(int finalScore)
    {
        SetPanels(start: false, hud: false, gameOver: true);
        if (finalScoreText != null) finalScoreText.text = $"Game Over\nPulpits walked: {finalScore}";
    }

    private void SetPanels(bool start, bool hud, bool gameOver)
    {
        if (startPanel != null) startPanel.SetActive(start);
        if (hudPanel != null) hudPanel.SetActive(hud);
        if (gameOverPanel != null) gameOverPanel.SetActive(gameOver);
    }

    private void OnStartClicked()
    {
        GameManager.Instance?.StartGame();
    }

    private void OnRestartClicked()
    {
        GameManager.Instance?.RestartGame();
    }
}
