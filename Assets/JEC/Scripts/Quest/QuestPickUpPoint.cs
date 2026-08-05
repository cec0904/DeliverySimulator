using UnityEngine;

public enum ShopType
{
    Burger,
    Grocery,
    SuperMarket,
    Electronics,
    Bagel
}

public class QuestPickUpPoint : MonoBehaviour
{
    // 픽업 포인트를 구별하는 고유 ID
    public string pointId;

    public ShopType shopType;

    // 이 위치에서 생성될 수 있는 퀘스트 목록
    public DeliveryQuestData[] availableQuests;
}