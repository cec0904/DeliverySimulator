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

        // 💡 팁: 데이터가 바뀌어 글자 수가 변했을 때 
        // 간혹 Layout 컴포넌트들이 즉시 크기 계산을 못 할 때가 있습니다.
        // 아래 코드를 한 줄 넣어주면 강제로 레이아웃을 즉시 재계산해 스크롤바가 알맞게 조절됩니다.
        Canvas.ForceUpdateCanvases();
    }
}
