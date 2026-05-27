using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public CanvasGroup menuGroup;
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    public TextMeshProUGUI titleText;
    public GameObject gameRoot;
    public GameObject settingsPanelObject;

    void Start()
    {
        if (playButton) playButton.onClick.AddListener(OnPlay);
        if (settingsButton) settingsButton.onClick.AddListener(OnSettings);
        if (quitButton) quitButton.onClick.AddListener(OnQuit);
        ShowMenu(true);
    }

    private void OnPlay() { if (AudioManager.Instance) AudioManager.Instance.PlayClick(); ShowMenu(false); if (gameRoot) gameRoot.SetActive(true); }
    private void OnSettings() { if (AudioManager.Instance) AudioManager.Instance.PlayClick(); if (settingsPanelObject) settingsPanelObject.SetActive(true); }
    private void OnQuit() { if (AudioManager.Instance) AudioManager.Instance.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void ShowMenu(bool show) { if (menuGroup) { menuGroup.alpha = show ? 1f : 0f; menuGroup.interactable = show; menuGroup.blocksRaycasts = show; menuGroup.gameObject.SetActive(show); } if (gameRoot && show) gameRoot.SetActive(false); }
}
