using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using rayzngames;

public class PoliceCarAI : MonoBehaviour
{
    public enum PoliceState { Idle, Intercept, Blocking, OfficerDeployed }
    public enum ApproachType { None, Front, Side, RearWide, RearNarrow }

    [Header("AI 상태 및 목표")]
    public PoliceState currentState = PoliceState.Idle;
    public ApproachType currentApproach = ApproachType.None;
    public BicycleVehicle targetPlayer;

    [Header("주행 및 회전 설정")]
    [Tooltip("몇 초 뒤의 플레이어 위치를 예측하여 선점할 것인가")]
    [SerializeField] private float predictionTime = 1.0f;
    [SerializeField] private float chaseSpeed = 25f;
    [SerializeField] private float turnSpeed = 5.0f;
    [Tooltip("선점한 목적지 도달 판정 거리")]
    [SerializeField] private float arrivalDistance = 3.0f;

    [Header("바퀴 회전 설정")]
    [SerializeField] private Transform[] wheels; // 경찰차 바퀴 Transform 4개
    [SerializeField] private float wheelRotateSpeed = 200f; // 바퀴 회전 속도 배율
    [SerializeField] private Vector3 wheelAxis = Vector3.right; // 회전 축 (기본: X축)


    [Header("🔥 스마트 추격 보정 (Failsafe)")]
    [Tooltip("플레이어가 예측 지점에서 이 거리 이상 벗어나면 예측을 재계산합니다.")]
    [SerializeField] private float repredictTolerance = 15.0f;
    [Tooltip("예측 재계산 쿨타임 (너무 잦은 계산 방지)")]
    [SerializeField] private float repredictCooldown = 1.5f;

    [Header("후방 충돌 방지 설정")]
    [SerializeField] private float sideOffsetDistance = 3.5f;
    [SerializeField] private float minSafeDistance = 8.0f;

    [Header("차단 및 하차 설정")]
    [SerializeField] private float blockAngleOffset = 90f;
    [SerializeField] private GameObject policeOfficerPrefab;
    [SerializeField] private Transform doorSpawnPoint;
    [Tooltip("하차하기 위한 플레이어와의 최대 허용 거리 ")]
    [SerializeField] private float maxDeployDistance = 10.0f;

    [Header("장애물 고립(Stuck) 대처")]
    [Tooltip("길이 막혔을 때 차를 버리고 하차를 결심할 플레이어와의 최대 거리")]
    [SerializeField] private float deployWhenBlockedDistance = 25.0f;
    private float stuckTimer = 0f; // 막힌 상태 유지 시간 측정용

    private NavMeshAgent agent;
    private Vector3 lockedInterceptPosition;
    private bool hasLockedDestination = false;
    private bool isDeploying = false; // 코루틴 중복 실행 방지 플래그
    private float lastRepredictTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.avoidancePriority = 30;
    }

    private void Update()
    {
        // 타겟 상실 (예: 플레이어 접속 종료, 파괴 등) 안전장치
        if (currentState == PoliceState.Intercept && targetPlayer == null)
        {
            StopChase();
            return;
        }

        switch (currentState)
        {
            case PoliceState.Idle:
                break;

            case PoliceState.Intercept:
                ValidateAndRepredict();
                CheckIfPathBlocked();
                DriveNaturally();
                CheckIfArrivedAtDestination();
                RotateWheels();
                break;

            case PoliceState.Blocking:
            case PoliceState.OfficerDeployed:
                break;
        }
    }

    public void StartIntercept(BicycleVehicle player)
    {
        if (currentState != PoliceState.Idle || player == null) return;

        targetPlayer = player;
        currentState = PoliceState.Intercept;
        isDeploying = false;

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        LockAndSetInterceptDestination();
    }

    private void StopChase()
    {
        currentState = PoliceState.Idle;
        if (agent.isActiveAndEnabled) agent.isStopped = true;
        hasLockedDestination = false;
    }

    /// <summary>
    /// 플레이어의 궤적이 처음 예측한 곳에서 너무 많이 벗어났는지 감시하고, 페이크를 치면 다시 추격합니다.
    /// </summary>
    private void ValidateAndRepredict()
    {
        if (!hasLockedDestination || Time.time - lastRepredictTime < repredictCooldown) return;

        float playerDistanceFromLock = Vector3.Distance(targetPlayer.transform.position, lockedInterceptPosition);

        // 플레이어가 예측 지점을 무시하고 완전히 다른 곳으로 도망갔다면? -> 목적지 재설정 (바보 방지)
        if (playerDistanceFromLock > repredictTolerance)
        {
            Debug.Log("<color=orange>[경찰 AI]</color> 플레이어의 경로 이탈 감지! 목적지를 재수정합니다.");
            LockAndSetInterceptDestination();
        }
    }

    private void CheckIfPathBlocked()
    {
        if (!hasLockedDestination || isDeploying) return;

        if (!agent.pathPending && (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer > 1.0f)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

                if (distanceToPlayer <= deployWhenBlockedDistance)
                {
                    Debug.Log("<color=magenta>[경찰 AI]</color> 장애물로 도로 통제됨! 차량을 포기하고 하차합니다.");

                    isDeploying = true;
                    agent.velocity = Vector3.zero;
                    agent.isStopped = true;
                    agent.ResetPath();

                    DeployOfficer(); // 즉시 하차 루틴 실행
                }
                else
                {
                    Debug.LogWarning("<color=orange>[경찰 AI]</color> 도로 차단됨. 우회 경로를 재탐색합니다.");
                    LockAndSetInterceptDestination();
                    stuckTimer = 0f;
                }
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void LockAndSetInterceptDestination()
    {
        if (targetPlayer == null || !agent.isActiveAndEnabled) return;

        Vector3 playerPos = targetPlayer.transform.position;
        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();

        Vector3 playerVelocity = playerRb != null ? playerRb.linearVelocity : targetPlayer.transform.forward * targetPlayer.currentSpeed;
        playerVelocity.y = 0;

        Vector3 moveDirection = playerVelocity.magnitude > 1.0f ? playerVelocity.normalized : targetPlayer.transform.forward;
        Vector3 moveRight = Vector3.Cross(Vector3.up, moveDirection).normalized;

        Vector3 toPolice = transform.position - playerPos;
        float dot = Vector3.Dot(moveDirection, toPolice.normalized);

        if (dot >= 0.5f) currentApproach = ApproachType.Front;
        else if (dot <= -0.7f)
        {
            Vector3 leftPos = playerPos - (moveRight * sideOffsetDistance);
            Vector3 rightPos = playerPos + (moveRight * sideOffsetDistance);

            bool canGoLeft = NavMesh.SamplePosition(leftPos, out NavMeshHit leftHit, 1.0f, NavMesh.AllAreas);
            bool canGoRight = NavMesh.SamplePosition(rightPos, out NavMeshHit rightHit, 1.0f, NavMesh.AllAreas);

            currentApproach = (!canGoLeft && !canGoRight) ? ApproachType.RearNarrow : ApproachType.RearWide;
        }
        else currentApproach = ApproachType.Side;

        switch (currentApproach)
        {
            case ApproachType.Front:
                lockedInterceptPosition = playerPos;
                break;
            case ApproachType.Side:
                lockedInterceptPosition = playerPos + (playerVelocity * predictionTime);
                break;
            case ApproachType.RearNarrow:
                lockedInterceptPosition = playerPos - (moveDirection * minSafeDistance);
                break;
            case ApproachType.RearWide:
                lockedInterceptPosition = playerPos + (playerVelocity * predictionTime) + (moveRight * sideOffsetDistance);
                break;
        }

        //if (NavMesh.SamplePosition(lockedInterceptPosition, out NavMeshHit finalHit, 5.0f, NavMesh.AllAreas))
        //    lockedInterceptPosition = finalHit.position;
        //else
        //    lockedInterceptPosition = playerPos;

        if (NavMesh.SamplePosition(lockedInterceptPosition, out NavMeshHit finalHit, 5.0f, NavMesh.AllAreas))
        {
          lockedInterceptPosition = finalHit.position;
        }
        else if (NavMesh.SamplePosition(playerPos, out NavMeshHit fallbackHit, 30.0f, NavMesh.AllAreas))
        {
         lockedInterceptPosition = fallbackHit.position;
        }
        else
        {
            Debug.LogWarning("<color=red>[경찰 AI]</color> 타겟이 맵 바깥으로 이탈했습니다. 추격을 중단합니다.");
            StopChase();
            return;
        }

        agent.SetDestination(lockedInterceptPosition);
        hasLockedDestination = true;
        lastRepredictTime = Time.time;
    }

    private void DriveNaturally()
    {
        Vector3 desiredVelocity = agent.desiredVelocity;

        float distanceToTarget = Vector3.Distance(transform.position, lockedInterceptPosition);
        if (distanceToTarget < 5.0f)
        {
            agent.speed = Mathf.Lerp(agent.speed, chaseSpeed * 0.4f, Time.deltaTime * 3f);
        }

        if (desiredVelocity.sqrMagnitude > 0.1f)
        {
            desiredVelocity.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void RotateWheels()
    {
        if (wheels == null || wheels.Length == 0) return;

        float currentSpeed = agent.velocity.magnitude;

        if (currentSpeed > 0.1f)
        {
            float rotationAmount = currentSpeed * wheelRotateSpeed * Time.deltaTime;

            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
                    wheels[i].Rotate(wheelAxis * rotationAmount, Space.Self);
                }
            }
        }
    }
    private void CheckIfArrivedAtDestination()
    {
        if (!hasLockedDestination || isDeploying) return;

        float distanceToTarget = Vector3.Distance(transform.position, lockedInterceptPosition);

        // 예측 지점(목적지)에 도달했는지 확인
        if (!agent.pathPending && (agent.remainingDistance <= arrivalDistance || distanceToTarget <= arrivalDistance))
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

            // ★ 맹점 방지: 차가 목적지에는 왔는데, 플레이어가 너무 멀리 있다면? (헛스윙)
            if (distanceToPlayer > maxDeployDistance)
            {
                // 경찰관을 내리지 않고 목표를 다시 수정해서 계속 추격합니다.
                LockAndSetInterceptDestination();
                return;
            }

            // 진짜 도착 & 플레이어가 근처에 있음
            isDeploying = true; // 코루틴 중복 실행 방지
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            agent.ResetPath(); // 에이전트 경로 완벽 초기화

            float facingDot = Vector3.Dot(transform.forward, targetPlayer.transform.forward);
            bool isRealHeadOn = (currentApproach == ApproachType.Front) && (facingDot < -0.2f);
            bool isRearNarrowStop = (currentApproach == ApproachType.RearNarrow);

            if (isRealHeadOn || isRearNarrowStop)
                StartCoroutine(ImmediateStopRoutine());
            else
                StartCoroutine(BlockRoadRoutine());
        }
    }

    private IEnumerator ImmediateStopRoutine()
    {
        currentState = PoliceState.Blocking;

        Debug.Log("<color=yellow>[경찰 AI]</color> 급정거! 즉시 하차합니다.");
        yield return new WaitForSeconds(0.3f); // 브레이크 끼이익 잡는 시간

        DeployOfficer();
    }

    private IEnumerator BlockRoadRoutine()
    {
        currentState = PoliceState.Blocking;

        Vector3 playerForward = targetPlayer.transform.forward;
        playerForward.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(-playerForward) * Quaternion.Euler(0, blockAngleOffset, 0);

        float elapsed = 0f;
        float blockDuration = 0.5f;
        Quaternion startRotation = transform.rotation;

        while (elapsed < blockDuration)
        {
            float t = elapsed / blockDuration;
            float easeOutT = 1f - Mathf.Pow(1f - t, 3f);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easeOutT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
        Debug.Log("<color=yellow>[경찰 AI]</color> 길목 차단 완료!");

        DeployOfficer();
    }

    private void DeployOfficer()
    {
        currentState = PoliceState.OfficerDeployed;

        if (policeOfficerPrefab != null && doorSpawnPoint != null)
        {
            GameObject officer = Instantiate(policeOfficerPrefab, doorSpawnPoint.position, doorSpawnPoint.rotation);
            PoliceAI officerAI = officer.GetComponent<PoliceAI>();

            if (officerAI != null)
            {
                Debug.Log("<color=cyan>[경찰 AI 차량]</color> 경찰관 하차 완료! 도보 추격 개시.");
                officerAI.SetSourceCar(gameObject);
                officerAI.StartFootChase();
            }
        }
    }
}