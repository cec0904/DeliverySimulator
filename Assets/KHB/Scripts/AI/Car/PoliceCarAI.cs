using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using rayzngames;

public class PoliceCarAI : MonoBehaviour
{
    public enum PoliceState { Idle, Intercept, Blocking }

    [Header("AI 상태 및 목표")]
    public PoliceState currentState = PoliceState.Idle;
    public BicycleVehicle targetPlayer;

    [Header("길목 예측 설정")]
    [Tooltip("몇 초 뒤의 플레이어 위치를 예측할 것인가")]
    [SerializeField] private float predictionTime = 3.0f;
    [Tooltip("추격 시 경찰차 이동 속도")]
    [SerializeField] private float chaseSpeed = 25f;
    [Tooltip("목표 길목 도착 인정 거리")]
    [SerializeField] private float arrivalDistance = 3.0f;

    [Header("차단 연동 설정")]
    [Tooltip("길목 도달 후 길을 막아설 방향 (플레이어 진행 방향의 직각으로 회전)")]
    [SerializeField] private float blockAngleOffset = 90f;

    private NavMeshAgent agent;
    private Vector3 currentTargetPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        switch (currentState)
        {
            case PoliceState.Idle:
                // 대기 상태 로직 (필요시 순찰 로직 추가 가능)
                break;

            case PoliceState.Intercept:
                UpdateInterceptDestination();
                CheckIfArrivedAtBlockPoint();
                break;

            case PoliceState.Blocking:
                // 길목 차단 완료 후 정지 상태 유지
                break;
        }
    }

    /// <summary>
    /// 과속 감지 시 호출되는 출동 명령 메서드
    /// </summary>
    public void StartIntercept(BicycleVehicle player)
    {
        if (currentState != PoliceState.Idle) return;

        targetPlayer = player;
        currentState = PoliceState.Intercept;

        // 추격 속도 증가 및 NavMesh 설정
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.updateRotation = true;

        Debug.Log("<color=red>[경찰 AI]</color> 과속 차량 감지! 길목 차단을 위해 출동합니다.");
    }

    /// <summary>
    /// 플레이어의 속도와 방향을 이용해 미래 위치 예측 후 NavMesh 목표 설정
    /// </summary>
    private void UpdateInterceptDestination()
    {
        if (targetPlayer == null) return;

        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();
        Vector3 playerVelocity = playerRb != null ? playerRb.linearVelocity : targetPlayer.transform.forward * targetPlayer.currentSpeed;

        // 플레이어가 이동 중이 아니면 현재 위치를 목표로 설정
        if (playerVelocity.magnitude < 0.5f)
        {
            currentTargetPosition = targetPlayer.transform.position;
        }
        else
        {
            // 예측 위치 = 현재 위치 + (속도 벡터 * 예측 시간)
            currentTargetPosition = targetPlayer.transform.position + (playerVelocity * predictionTime);
        }

        // NavMesh 위의 유효한 위치인지 검증 후 이동 지정
        if (NavMesh.SamplePosition(currentTargetPosition, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(targetPlayer.transform.position);
        }
    }

    /// <summary>
    /// 길목에 도착했는지 확인하고 차단 태세로 전환
    /// </summary>
    private void CheckIfArrivedAtBlockPoint()
    {
        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            StartCoroutine(BlockRoadRoutine());
        }
    }

    /// <summary>
    /// 도로를 가로막는 회전 연출 및 차단 상태 전환
    /// </summary>
    private IEnumerator BlockRoadRoutine()
    {
        currentState = PoliceState.Blocking;
        agent.isStopped = true;
        agent.updateRotation = false; // 수동 회전을 위해 NavMesh 자동 회전 비활성화

        // 플레이어의 예상 진입 방향을 바라보고 90도 회전하여 도로 가로막기
        Vector3 approachDirection = (currentTargetPosition - targetPlayer.transform.position).normalized;
        if (approachDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(approachDirection) * Quaternion.Euler(0, blockAngleOffset, 0);

            float elapsed = 0f;
            Quaternion startRotation = transform.rotation;

            while (elapsed < 1.0f)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed);
                elapsed += Time.deltaTime * 3f;
                yield return null;
            }
            transform.rotation = targetRotation;
        }

        Debug.Log("<color=yellow>[경찰 AI]</color> 길목 차단 완료!");
    }
}
