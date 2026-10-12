using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Screen Panels")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject infoPanel;

    private void Start()
    {
        ShowGame();
    }

    public void ShowGame() => SwitchPanel(gamePanel);
    public void ShowOptions() => SwitchPanel(optionsPanel);
    public void ShowStats() => SwitchPanel(statsPanel);
    public void ShowInfo() => SwitchPanel(infoPanel);

    private void SwitchPanel(GameObject activePanel)
    {
        if (gamePanel != null) gamePanel.SetActive(gamePanel == activePanel);
        if (optionsPanel != null) optionsPanel.SetActive(optionsPanel == activePanel);
        if (statsPanel != null) statsPanel.SetActive(statsPanel == activePanel);
        if (infoPanel != null) infoPanel.SetActive(infoPanel == activePanel);
    }
}