using UnityEngine;
using UnityEngine.AI;
using rayzngames;

public enum PoliceState { Patrol, Chase }

[RequireComponent(typeof(NavMeshAgent))]
public class PoliceAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform targetToChase;
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
    public void StartFootChase(Transform target)
    {
        if (target == null) return;
        targetToChase = target;
        isChasing = true;

        agent.speed = runSpeed;
        agent.isStopped = false;

        // 애니메이터가 있다면 달리기 애니메이션 트리거 설정
        // Animator anim = GetComponentInChildren<Animator>();
        // anim.SetBool("isRunning", true);
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