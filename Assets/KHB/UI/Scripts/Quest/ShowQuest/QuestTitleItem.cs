using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestTitleItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button button;

    private QuestDataSO cachedQuestData;
    private System.Action<QuestDataSO> onClickCallback;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClickButton);
    }

    public void Setup(QuestDataSO questData, System.Action<QuestDataSO> onClick)
    {
        cachedQuestData = questData;
        onClickCallback = onClick;

        if (titleText != null)
        {
            titleText.text = questData.questTitle;
        }
    }

    private void OnClickButton()
    {
        onClickCallback?.Invoke(cachedQuestData);
    }
}