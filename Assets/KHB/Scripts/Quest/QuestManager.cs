using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public QuestGenerator questGenerator;
    public QuestOfferUI offerUI;

    public float offerInterval = 10.0f;

    //gameover count?
    public int maxActiveQuestCount = 5;

    private List<QuestDataSO> activeQuestList = new List<QuestDataSO>();
    public List<QuestDataSO> GetActiveQuests()
    {
        return activeQuestList;
    }
    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        // 🌟 게임 시작과 동시에 100초 주기 코루틴 실행!
        StartCoroutine(Co_AutoQuestOfferRoutine());
    }
    private IEnumerator Co_AutoQuestOfferRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(offerInterval);
            ReceiveNewQuestOffer();
        }
    }

    public void ReceiveNewQuestOffer()
    {
        if (questGenerator == null || offerUI == null) return;

        if (activeQuestList.Count >= maxActiveQuestCount)
        {
            Debug.Log("[퀘스트 제안 스킵] 이미 진행 중인 퀘스트가 가득 찼습니다.");
            return;
            // 게임 오버
        }


        // 1. 임시 runtimeQuest 생성
        QuestDataSO runtimeQuest = questGenerator.GenerateRandomQuest();

        if (runtimeQuest != null)
        {
            // 2. UI 팝업을 열고, 10초 후 결과(isAccepted)를 콜백받음
            offerUI.ShowQuestOffer(runtimeQuest, (bool isAccepted) =>
            {
                if (isAccepted)
                {
                    // ⭕ 수락: 퀘스트 목록에 추가
                    if (QuestManager.Instance != null)
                    {
                        QuestManager.Instance.GetActiveQuests().Add(runtimeQuest);
                        if (QuestListUI.Instance != null && QuestListUI.Instance.gameObject.activeInHierarchy)
                        {
                            QuestListUI.Instance.RefreshQuestList();
                        }
                    }
                    Debug.Log($"[수락 완료] {runtimeQuest.questTitle} 퀘스트가 목록에 추가되었습니다.");
                }
                else
                {
                    Debug.Log($"[거절/취소] {runtimeQuest.questTitle} 의뢰를 수락하지 않았습니다.");
                    Destroy(runtimeQuest);
                }
            });
        }
    }
}
