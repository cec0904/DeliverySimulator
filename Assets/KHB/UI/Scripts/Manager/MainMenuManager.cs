using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject keySettingPanel;
    [SerializeField] private GameObject optionPanel; 
    [SerializeField] private GameObject exitPanel;
    public bool IsOpen => gameObject.activeSelf;

    public bool IsSubPanelOpen => (keySettingPanel != null && keySettingPanel.activeSelf) ||
                                  (optionPanel != null && optionPanel.activeSelf) ||
                                  (exitPanel != null && exitPanel.activeSelf);

    public void SetMenuVisible(bool visible)
    {
        gameObject.SetActive(visible);

        ResetToMainButtons();
    }

    public void ResetToMainButtons()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (keySettingPanel != null) keySettingPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (exitPanel != null) exitPanel.SetActive(false);

    }
    public void OnClickKeySetting()
    {
        mainButtonsPanel.SetActive(false);
        keySettingPanel.SetActive(true);
    }

    public void OnClickOption()
    {
        mainButtonsPanel.SetActive(false);
        optionPanel.SetActive(true);
    }
    public void OnClickExitPopup()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (exitPanel != null) exitPanel.SetActive(true);
    }
    public void OnClickBackButton()
    {
        ResetToMainButtons();
    }

    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
