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
    [SerializeField] private string displayName;

    [Header("생성 가능한 퀘스트")]
    public DeliveryQuestData[] availableQuests;

    [Header("퀘스트 목록")]
    [SerializeField] private PlayerQuestList playerQuestList;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? gameObject.name
        : displayName;

    public Texture2D RepresentativeIcon
    {
        get
        {
            if (availableQuests == null)
            {
                return null;
            }

            foreach (DeliveryQuestData quest in availableQuests)
            {
                if (quest != null && quest.icon != null)
                {
                    return quest.icon;
                }
            }

            return null;
        }
    }

    private void Awake()
    {
        if (playerQuestList == null)
        {
            playerQuestList = FindAnyObjectByType<PlayerQuestList>();
        }
    }

    public override string GetPromptMessage(GameObject interactor)
    {
        if (playerQuestList == null || !playerQuestList.HasQuestReadyForPickupAt(this))
        {
            return string.Empty;
        }

        return "<color=#FFD36A>F키</color>를 누르면 물건을 받을 수 있습니다";
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
