using UnityEngine;

public enum ShopType
{
    Burger,
    Grocery,
    SuperMarket,
    Electronics,
    Bagel
}

public class QuestPickUpPoint : Interactable
{
    [Header("픽업포인트 정보")]
    public string pointId;
    public ShopType shopType;

    [Header("생성 가능한 퀘스트")]
    public DeliveryQuestData[] availableQuests;

    [Header("퀘스트 목록")]
    [SerializeField] private PlayerQuestList playerQuestList;

    private void Awake()
    {
        if (playerQuestList == null)
        {
            playerQuestList = FindAnyObjectByType<PlayerQuestList>();
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (playerQuestList == null)
        {
            Debug.LogError($"[{name}] PlayerQuestList를 찾을 수 없습니다.", this);

            return;
        }

        int pickedUpCount = playerQuestList.TryPickUpQuestsAt(this);

        if (pickedUpCount == 0)
        {
            Debug.Log($"[{name}] 이 장소에서 받을 수 있는 수락된 퀘스트가 없습니다.", this);

            return;
        }

        Debug.Log($"[{name}] 물건 {pickedUpCount}개를 받았습니다.", this);
    }
}