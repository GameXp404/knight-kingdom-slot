using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    public Slider volumeSlider;
    public Toggle muteToggle;
    public Button resetButton;
    public Button closeButton;
    public TextMeshProUGUI statsText;
    public Button easyBtn;
    public Button mediumBtn;
    public Button hardBtn;
    public TextMeshProUGUI difficultyLabel;

    private static readonly Color SelectedColor = new Color(0.95f, 0.55f, 0.10f);
    private static readonly Color UnselectedColor = new Color(0.25f, 0.25f, 0.35f);

    void OnEnable()
    {
        if (volumeSlider) { volumeSlider.value = SaveSystem.Volume; volumeSlider.onValueChanged.AddListener(OnVolumeChanged); }
        if (muteToggle) { muteToggle.isOn = SaveSystem.Muted; muteToggle.onValueChanged.AddListener(OnMuteChanged); }
        if (resetButton) resetButton.onClick.AddListener(OnResetPressed);
        if (closeButton) closeButton.onClick.AddListener(OnClosePressed);
        if (easyBtn) easyBtn.onClick.AddListener(() => SetDifficulty(0));
        if (mediumBtn) mediumBtn.onClick.AddListener(() => SetDifficulty(1));
        if (hardBtn) hardBtn.onClick.AddListener(() => SetDifficulty(2));
        RefreshStats(); UpdateDifficultyDisplay();
    }
    void OnDisable() { if (volumeSlider) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged); if (muteToggle) muteToggle.onValueChanged.RemoveListener(OnMuteChanged); if (resetButton) resetButton.onClick.RemoveListener(OnResetPressed); if (closeButton) closeButton.onClick.RemoveListener(OnClosePressed); }
    private void OnVolumeChanged(float v) { if (AudioManager.Instance) AudioManager.Instance.SetVolume(v); }
    private void OnMuteChanged(bool m) { if (AudioManager.Instance) AudioManager.Instance.SetMuted(m); }
    private void OnResetPressed() { SaveSystem.ResetAll(); RefreshStats(); if (volumeSlider) volumeSlider.value = SaveSystem.Volume; if (muteToggle) muteToggle.isOn = SaveSystem.Muted; UpdateDifficultyDisplay(); }
    private void OnClosePressed() { gameObject.SetActive(false); }
    private void SetDifficulty(int level) { SaveSystem.DifficultyLevel = level; UpdateDifficultyDisplay(); if (AudioManager.Instance) AudioManager.Instance.PlayClick(); if (GameManager.Instance != null) GameManager.Instance.RefreshReelStrips(); }
    private void UpdateDifficultyDisplay() { var d = (Difficulty)SaveSystem.DifficultyLevel; if (difficultyLabel) difficultyLabel.text = SymbolDatabase.DifficultyLabel(d); if (easyBtn) easyBtn.image.color = (d == Difficulty.Easy) ? SelectedColor : UnselectedColor; if (mediumBtn) mediumBtn.image.color = (d == Difficulty.Medium) ? SelectedColor : UnselectedColor; if (hardBtn) hardBtn.image.color = (d == Difficulty.Hard) ? SelectedColor : UnselectedColor; }
    private void RefreshStats() { if (statsText == null) return; statsText.text = $"Total Spins: {SaveSystem.TotalSpins:N0}    Total Wins: {SaveSystem.TotalWins:N0}\nBiggest Win: {SaveSystem.BiggestWin:N0}    Saldo: {SaveSystem.Currency:N0}"; }
}
