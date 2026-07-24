using UnityEngine;
using System.Collections.Generic;

public class QuestGenerator : MonoBehaviour
{
    [Header("[ 퀘스트 데이터베이스 연결 ]")]
    public QuestDatabaseSO questDatabase;

    [Header("[ 플레이어 / 출발 위치 ]")]
    public Transform playerTransform; // 플레이어의 현재 위치 (또는 출발지 위치)

    [Header("[ 장소별 실제 위치 (목적지 좌표) ]")]
    // 인스펙터에서 각 장소에 해당하는 3D 오브젝트/Transform을 연결해 줍니다.
    public Transform officeIndoorTransform;
    public Transform officeOutdoorTransform;
    public Transform apartmentTransform;
    public Transform houseTransform;

    [Header("[ 제한 시간 랜덤 범위 (초 단위) ]")]
    public int minTimeLimit = 60;   // 최소 제한시간 (예: 60초)
    public int maxTimeLimit = 180;  // 최대 제한시간 (예: 180초)

    [Header("[ 거리에 따른 보상 수치 설정 ]")]
    public int baseReward = 100;          // 기본 보상 골드
    public float goldPerMeter = 10.0f;     // 1미터(유니티 1단위)당 추가될 골드
    public QuestDataSO GenerateRandomQuest()
    {
        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase가 연결되지 않았습니다!");
            return null;
        }

        QuestDataSO originalQuest = questDatabase.GetRandomQuest();
        if (originalQuest == null) return null;

        QuestDataSO runtimeQuest = ScriptableObject.CreateInstance<QuestDataSO>();

        // 3. 고정 정보 복사 (제목, 내용, 장소, 배달물 등)
        runtimeQuest.questID = originalQuest.questID;
        runtimeQuest.questTitle = originalQuest.questTitle;
        runtimeQuest.questDescription = originalQuest.questDescription;
        runtimeQuest.deliveryLocation = originalQuest.deliveryLocation;
        runtimeQuest.deliveryType = originalQuest.deliveryType;
        runtimeQuest.deliveryItemName = originalQuest.deliveryItemName;

        // 4. [요청 기능 1] timeLimit : 설정한 범위(min ~ max)에서 랜덤 지정
        runtimeQuest.timeLimit = Random.Range(minTimeLimit, maxTimeLimit + 1);

        // 5. [요청 기능 2] rewardGold : 목적지까지의 거리에 따라 자동으로 계산
        Vector3 destinationPos = GetLocationPosition(runtimeQuest.deliveryLocation);
        Vector3 startPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        // 직선 거리(미터) 계산
        float distance = Vector3.Distance(startPos, destinationPos);

        // 거리 기반 보상 공식: 기본 보상 + (거리 * 미터당 골드)
        int calculatedReward = baseReward + Mathf.RoundToInt(distance * goldPerMeter);
        runtimeQuest.rewardGold = calculatedReward;

        return runtimeQuest;
    }
    private Vector3 GetLocationPosition(DeliveryLocation location)
    {
        switch (location)
        {
            case DeliveryLocation.Office_Indoor:
                return officeIndoorTransform != null ? officeIndoorTransform.position : Vector3.zero;
            case DeliveryLocation.Office_Outdoor:
                return officeOutdoorTransform != null ? officeOutdoorTransform.position : Vector3.zero;
            case DeliveryLocation.Apartment:
                return apartmentTransform != null ? apartmentTransform.position : Vector3.zero;
            case DeliveryLocation.House:
                return houseTransform != null ? houseTransform.position : Vector3.zero;
            default:
                return Vector3.zero;
        }
    }
}
