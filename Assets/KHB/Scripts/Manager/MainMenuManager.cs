using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject keySettingPanel;
    //[SerializeField] private GameObject optionPanel; 
    [SerializeField] private GameObject exitPanel;
    public bool IsOpen => gameObject.activeSelf;

    public bool IsSubPanelOpen => (keySettingPanel != null && keySettingPanel.activeSelf);
                                  

    public void SetMenuVisible(bool visible)
    {
        
        gameObject.SetActive(visible);

        ResetToMainButtons();
    }

    public void ResetToMainButtons()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (keySettingPanel != null) keySettingPanel.SetActive(false);

    }

    public void OnClickQuestButton()
    {
        if (UIManager.Instance != null)
        {
            // UIManager에게 메인메뉴 닫기 + 퀘스트 열기를 맡김
            UIManager.Instance.OpenQuestPanel();
        }
    }
    public void OnClickKeySetting()
    {
        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (keySettingPanel != null)
            keySettingPanel.SetActive(true);
    }
    public void OnClickMap()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenMapPanel();
        }
    }
    public void OnClickBackButton()
    {
        ResetToMainButtons();
    }

    public void OnClickCloseMenu()
    {
        Debug.Log("OnClickCloseMenu");

        ResetToMainButtons();

        if (UIManager.Instance != null)
        {

            UIManager.Instance.CloseMainMenu();
        }
    }
    public void OnClickCloseKeySetting()
    {
        if (keySettingPanel != null)
            keySettingPanel.SetActive(false);

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(true);
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
