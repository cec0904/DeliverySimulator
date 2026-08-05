using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class questManager : MonoBehaviour
{
    private const int MaxOfferCount = 5;

    // 퀘스트 갱신(생성) 시간
    [SerializeField] private float OfferInterval = 60f;

    [SerializeField] private QuestPickUpPoint[] pickUpPoints;
    [SerializeField] private QuestDestination[] destinations;
    [SerializeField] private PlayerQuestList playerQuestList;

    // 플레이어가 선택할 수 있는 퀘스트 목록
    private readonly List<QuestRuntimeData> questOffers = new();

    // 다른 스크립트에서 목록을 읽을 수 있도록 제공
    public IReadOnlyList<QuestRuntimeData> QuestOffers => questOffers;
    public event Action OffersChanged;

    private Coroutine offerRoutine;

    private void OnEnable()
    {
        offerRoutine = StartCoroutine(CreateQuestOfferRoutine());
    }

    private void OnDisable()
    {
        if (offerRoutine != null)
        {
            StopCoroutine(offerRoutine);
            offerRoutine = null;
        }
    }

    private IEnumerator CreateQuestOfferRoutine()
    {
        while (true)
        {
            // 게임 시작 후 첫 퀘스트도 60초 뒤에 생성
            // 60초동안 기본 조작과 게임의 목표 같은 튜토리얼 ui 띄울 것임
            yield return new WaitForSeconds(OfferInterval);
            TryAddRandomQuestOffer();
        }
    }

    public QuestRuntimeData CreateRandomQuest()
    {
        QuestPickUpPoint pickUpPoint = GetRandomPickUpPoint();

        if (pickUpPoint == null)
        {
            return null;
        }

        // 해당 포인트의 데이터 하나와 목적지 NPC 하나 선택
        DeliveryQuestData questData = GetRandomQuestData(pickUpPoint);
        QuestDestination destination = GetRandomDestination();

        if (questData == null || destination == null)
        {
            return null;
        }

        QuestRuntimeData newQuest = new QuestRuntimeData
        {
            runtimeQuestId = Guid.NewGuid().ToString(),
            questData = questData,
            pickupPoint = pickUpPoint,
            destination = destination,
            state = QuestState.Offered,
            reward = questData.baseReward
        };

        return newQuest;
    }

    public bool TryAddRandomQuestOffer()
    {
        if (questOffers.Count >= MaxOfferCount)
        {
            return false;
        }

        QuestRuntimeData newQuest = CreateRandomQuest();

        if (newQuest == null)
        {
            return false;
        }

        questOffers.Add(newQuest);
        //OffersChanged?.Invoke(); 같은뜻
        if (OffersChanged != null)
        {
            OffersChanged.Invoke();
        }
        return true;
    }

    public bool TryAcceptQuest(string runtimeQuestId)
    {
        QuestRuntimeData quest = questOffers.Find(offer => offer.runtimeQuestId == runtimeQuestId);

        if (quest == null || playerQuestList == null)
        {
            return false;
        }

        if (!playerQuestList.TryAddQuest(quest))
        {
            return false;
        }

        questOffers.Remove(quest);
        OffersChanged?.Invoke();
        return true;
    }

    private QuestPickUpPoint GetRandomPickUpPoint()
    {
        if (pickUpPoints == null || pickUpPoints.Length == 0)
        {
            return null;
        }

        // 퀘스트가 등록된 포인트만 후보로 사용
        List<QuestPickUpPoint> candidates = new();

        foreach (QuestPickUpPoint point in pickUpPoints)
        {
            if (point != null && point.availableQuests != null && point.availableQuests.Length > 0)
            {
                candidates.Add(point);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private DeliveryQuestData GetRandomQuestData(QuestPickUpPoint pickUpPoint)
    {
        DeliveryQuestData[] quests = pickUpPoint.availableQuests;

        if (quests == null || quests.Length == 0)
        {
            return null;
        }

        return quests[UnityEngine.Random.Range(0, quests.Length)];
    }

    private QuestDestination GetRandomDestination()
    {
        if (destinations == null || destinations.Length == 0)
        {
            return null;
        }

        List<QuestDestination> candidates = new();

        foreach (QuestDestination destination in destinations)
        {
            if (destination != null && destination.CanReceiveDelivery)
            {
                candidates.Add(destination);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
}
