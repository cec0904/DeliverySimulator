using UnityEngine;
using UnityEngine.AI;

public enum PoliceState { Patrol, Chase }

[RequireComponent(typeof(NavMeshAgent))]
public class PoliceAI : MonoBehaviour
{
    public PoliceState currentState = PoliceState.Patrol;

    [Header("추격 대상")]
    public Transform target;

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        switch (currentState)
        {
            case PoliceState.Patrol:
                // 기본 순찰 로직 (생략 가능)
                break;

            case PoliceState.Chase:
                // 타겟(속도 위반한 자전거)을 실시간 추격
                if (target != null)
                {
                    agent.SetDestination(target.position);
                }
                break;
        }
    }

    // 감지 구역에서 속도 위반 감지 시 호출되는 메서드
    public void StartChase(Transform targetTransform)
    {
        if (currentState != PoliceState.Chase)
        {
            target = targetTransform;
            currentState = PoliceState.Chase;
            Debug.Log("경찰: 속도 위반 차량 추격을 시작합니다!");
        }
    }
}