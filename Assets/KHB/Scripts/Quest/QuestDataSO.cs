using UnityEngine;
public enum DeliveryLocation
{
    None,
    Office_Indoor,
    Office_Outdoor,
    Apartment,
    House
}

// 배달 종류 유형 정의
public enum DeliveryType
{
    None,
    Normal,         // 일반 배달
    Express,        // 특급 배달
    Fragile,        // 파손 주의
    Secret          // 비밀 배달
}

[CreateAssetMenu(fileName = "NewQuestData", menuName = "QuestSystem/Quest Data")]
public class QuestDataSO : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public int questID;
    public string questTitle;

    [Header("[ 내용 ]")]
    [TextArea(5, 10)]
    public string questDescription;

    [Header("[ 배달 세부 정보 ]")]
    public DeliveryLocation deliveryLocation; 
    public DeliveryType deliveryType;     
    public string deliveryItemName;

    [HideInInspector] public int timeLimit;
    [HideInInspector] public int rewardGold;

}
