using UnityEngine;
using UnityEngine.UI;

// HUD and overlay panels only: shows what GameManager tells it, no gameplay logic.
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Text roundText;
    [SerializeField] private string roundFormat = "RONDA {0}/{1}";
    [SerializeField] private Image[] hullIcons;

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;

    [Header("Mute")]
    [SerializeField] private Image muteIcon;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Game Over")]
    [SerializeField] private GameObject retryButton;

    private void Awake()
    {
        roundText.gameObject.SetActive(false); // Shown when the first round starts
        pausePanel.SetActive(false);
        retryButton.SetActive(false);
        SetHull(hullIcons.Length);
    }

    private void OnEnable() => ShipPartBehaviour.HitPointsChanged += SetHull;
    private void OnDisable() => ShipPartBehaviour.HitPointsChanged -= SetHull;

    public void SetRound(int round, int total)
    {
        roundText.gameObject.SetActive(true);
        roundText.text = string.Format(roundFormat, round, total);
    }

    public void SetHull(int hitPoints)
    {
        for (int i = 0; i < hullIcons.Length; i++)
            hullIcons[i].enabled = i < hitPoints;
    }

    public void SetPause(bool paused) => pausePanel.SetActive(paused);

    public void SetMuted(bool muted)
    {
        if (muteIcon != null) muteIcon.sprite = muted ? soundOffSprite : soundOnSprite;
    }

    public void ShowRetry() => retryButton.SetActive(true);
}
