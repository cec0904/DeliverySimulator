using UnityEngine;
using UnityEngine.AI;
using rayzngames;

public enum PoliceState { Patrol, Chase }

[RequireComponent(typeof(NavMeshAgent))]
public class PoliceAI : MonoBehaviour
{
    private NavMeshAgent agent;
    [Header("추격 대상 설정")]
    [SerializeField] private Transform targetToChase;
    private bool isChasing = false;

    [Header("도보 추격 설정")]
    public float runSpeed = 6.0f;
    [SerializeField] private float arrestDistance = 1.5f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = arrestDistance;
    }

    /// <summary>
    /// 차량 AI로부터 호출되어 도보 추격을 시작합니다.
    /// </summary>
    public void StartFootChase()
    {
        if (targetToChase == null)
        {
            Debug.LogWarning("[PoliceOfficerAI] 추격할 Target이 에디터에 할당되지 않았습니다!");
            return;
        }

        isChasing = true;
        agent.speed = runSpeed;
        agent.isStopped = false;
    }

    private void Update()
    {
        if (isChasing && targetToChase != null)
        {
            // 플레이어의 실시간 위치로 내비게이션 목적지 갱신
            agent.SetDestination(targetToChase.transform.position);

            // 체포 거리 도달 시 로직
            if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
            {
                ArrestPlayer();
            }
        }
    }

    private void ArrestPlayer()
    {
        isChasing = false;
        agent.isStopped = true;
        Debug.Log("<color=blue>[경찰관 AI]</color> 플레이어 체포 완료!");

    }
}