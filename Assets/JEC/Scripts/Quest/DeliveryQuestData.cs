using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDeliveryQuest",
    menuName = "Quest/Delivery Quest Data"
)]
public class DeliveryQuestData : ScriptableObject
{
    // 퀘스트를 구별하는 고유 ID
    public string questId;

    // UI에 표시할 정보
    public string displayName;

    [TextArea]
    public string description;

    public Texture2D icon;

    // 이 퀘스트를 받을 수 있는 가게 종류
   // public ShopType shopType;

    // 거리 보상을 더하기 전 기본 보상
    public int baseReward;
}