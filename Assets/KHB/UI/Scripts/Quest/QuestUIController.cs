using TMPro;
using UnityEngine;

public class QuestUIController : MonoBehaviour
{
    [Header("[ UI References ]")]
    [SerializeField] private TextMeshProUGUI titleText;       // QuestTitle 오브젝트 연결
    [SerializeField] private TextMeshProUGUI descriptionText; // QuestDescription 오브젝트 연결

    [Header("[ Test Quest Data ]")]
    public QuestData activeQuest; // 인스펙터에서 테스트할 퀘스트 데이터

    private void Start()
    {
        if (activeQuest != null)
        {
            UpdateQuestUI(activeQuest);
        }
    }

    public void UpdateQuestUI(QuestData newQuest)
    {
        if (newQuest == null) return;

        // 1. 데이터 매핑
        titleText.text = newQuest.questTitle;
        descriptionText.text = newQuest.questDescription;

        Canvas.ForceUpdateCanvases();
    }
}
