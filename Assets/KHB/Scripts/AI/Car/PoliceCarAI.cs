using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using rayzngames;

public class PoliceCarAI : MonoBehaviour
{
    public enum PoliceState { Idle, Intercept, Blocking, OfficerDeployed }

    // 현재 경찰차가 플레이어 기준 어느 방향에서 추격 중인지 상태 분류
    public enum ApproachType { None, Front, Side, RearWide, RearNarrow }

    [Header("AI 상태 및 목표")]
    public PoliceState currentState = PoliceState.Idle;
    public ApproachType currentApproach = ApproachType.None;
    public BicycleVehicle targetPlayer;

    [Header("주행 및 회전 설정")]
    [Tooltip("몇 초 뒤의 플레이어 위치를 예측")]
    [SerializeField] private float predictionTime = 0.5f;
    [SerializeField] private float chaseSpeed = 25f;
    [SerializeField] private float turnSpeed = 5.0f;
    [Tooltip("이 거리 내에 도달하면 정지/차단 연출 시작")]
    [SerializeField] private float arrivalDistance = 6.0f;

    [Header("후방 충돌 방지 설정")]
    [Tooltip("후방 진입 시 좌/우 여유 공간을 검사할 거리")]
    [SerializeField] private float sideOffsetDistance = 3.5f;
    [Tooltip("최소 안전거리")]
    [SerializeField] private float minSafeDistance = 8.0f;

    [Header("차단 연동 설정")]
    [Tooltip("측면/넓은 길 차단 시 길을 막아설 방향")]
    [SerializeField] private float blockAngleOffset = 90f;

    [Header("경찰관 하차 설정")]
    [SerializeField] private GameObject policeOfficerPrefab;
    [SerializeField] private Transform doorSpawnPoint;

    private NavMeshAgent agent;
    private Vector3 currentTargetPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.avoidancePriority = 30;
    }

    private void Update()
    {
        switch (currentState)
        {
            case PoliceState.Idle:
                break;

            case PoliceState.Intercept:
                UpdateInterceptDestination();
                DriveNaturally();
                CheckIfArrivedAtDestination();
                break;

            case PoliceState.Blocking:
            case PoliceState.OfficerDeployed:
                break;
        }
    }

    public void StartIntercept(BicycleVehicle player)
    {
        if (currentState != PoliceState.Idle) return;

        targetPlayer = player;
        currentState = PoliceState.Intercept;

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        Debug.Log("<color=red>[경찰 AI]</color> 추격 개시!");
    }

    private void UpdateInterceptDestination()
    {
        if (targetPlayer == null) return;

        Vector3 playerPos = targetPlayer.transform.position;
        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();

        // 1. 플레이어의 실제 속도(Velocity) 
        Vector3 playerVelocity = playerRb != null ? playerRb.linearVelocity : targetPlayer.transform.forward * targetPlayer.currentSpeed;
        playerVelocity.y = 0; // 상하 이동(점프 등) 무시

        // 2. ★ 핵심: 차체가 아닌 '실제 이동 방향'을 정면으로 취급
        // (속도가 거의 0일 때는 차체가 바라보는 방향을 기본값으로 사용)
        Vector3 moveDirection = playerVelocity.magnitude > 1.0f ? playerVelocity.normalized : targetPlayer.transform.forward;

        // 이동 방향 기준의 '오른쪽(Right)' 벡터 계산 (좌우 회피 판별용)
        Vector3 moveRight = Vector3.Cross(Vector3.up, moveDirection).normalized;

        // 3. 경찰차와 플레이어 간의 방향 벡터
        Vector3 toPolice = transform.position - playerPos;
        Vector3 dirToPolice = toPolice.normalized;
        float distanceToPlayer = toPolice.magnitude;

        // 4. 차체가 아닌 '실제 이동 방향(moveDirection)'을 기준으로 내적 계산
        float dot = Vector3.Dot(moveDirection, dirToPolice);

        // --- 상황별 접근 방향 판별 ---
        if (dot >= 0.5f)
        {
            currentApproach = ApproachType.Front;
        }
        else if (dot <= -0.7f) // 후방 판별
        {
            // 좌/우 NavMesh 공간 확인 시에도 transform.right 대신 moveRight 사용
            Vector3 leftPos = playerPos - (moveRight * sideOffsetDistance);
            Vector3 rightPos = playerPos + (moveRight * sideOffsetDistance);

            bool canGoLeft = NavMesh.SamplePosition(leftPos, out NavMeshHit leftHit, 1.0f, NavMesh.AllAreas);
            bool canGoRight = NavMesh.SamplePosition(rightPos, out NavMeshHit rightHit, 1.0f, NavMesh.AllAreas);

            if (!canGoLeft && !canGoRight)
                currentApproach = ApproachType.RearNarrow;
            else
                currentApproach = ApproachType.RearWide;
        }
        else
        {
            currentApproach = ApproachType.Side;
        }
        Debug.Log($"<color=orange>[방향 체크]</color> 차체 정면: {targetPlayer.transform.forward} | 실제 이동: {moveDirection} | 내적(Dot): {dot:F2} | 상태: {currentApproach}");

        // 2. 씬(Scene) 뷰 시각적 로그 (기즈모 선 그리기)
        // 파란색 선: 오토바이가 시각적으로 바라보는 앞쪽
        Debug.DrawRay(playerPos, targetPlayer.transform.forward * 4f, Color.blue);

        // 초록색 선: 실제 이동하고 있는 방향 (S키 누르면 파란선과 반대로 그려짐)
        Debug.DrawRay(playerPos + Vector3.up * 0.2f, moveDirection * 4f, Color.green);

        // 빨간색 선: 플레이어 기준에서 경찰차가 있는 방향
        Debug.DrawRay(playerPos + Vector3.up * 0.4f, dirToPolice * 4f, Color.red);
        // --- 판별된 방향에 따른 목적지 설정 로직 ---
        switch (currentApproach)
        {
            case ApproachType.Front:
                currentTargetPosition = playerPos;
                agent.speed = chaseSpeed;
                break;

            case ApproachType.Side:
                Vector3 predictSidePos = playerPos + (playerVelocity * predictionTime);
                if (NavMesh.SamplePosition(predictSidePos, out NavMeshHit sideHit, 4.0f, NavMesh.AllAreas))
                    currentTargetPosition = sideHit.position;
                else
                    currentTargetPosition = playerPos;
                agent.speed = chaseSpeed;
                break;

            case ApproachType.RearNarrow:
                // 후방 좁은 길 추격 시 들이받지 않도록 이동 방향의 반대쪽으로 목적지 설정
                currentTargetPosition = playerPos - (moveDirection * minSafeDistance);

                if (distanceToPlayer < minSafeDistance + 2f)
                    agent.speed = Mathf.Lerp(agent.speed, targetPlayer.currentSpeed * 0.8f, Time.deltaTime * 5f);
                else
                    agent.speed = chaseSpeed;
                break;

            case ApproachType.RearWide:
                Vector3 leftPos = playerPos - (moveRight * sideOffsetDistance);
                Vector3 rightPos = playerPos + (moveRight * sideOffsetDistance);

                bool leftOpen = NavMesh.SamplePosition(leftPos, out NavMeshHit lHit, 1.0f, NavMesh.AllAreas);
                bool rightOpen = NavMesh.SamplePosition(rightPos, out NavMeshHit rHit, 1.0f, NavMesh.AllAreas);

                Vector3 chosenSide = moveRight;
                if (leftOpen && rightOpen)
                {
                    float distLeft = Vector3.Distance(transform.position, leftPos);
                    float distRight = Vector3.Distance(transform.position, rightPos);
                    chosenSide = (distLeft < distRight) ? -moveRight : moveRight;
                }
                else if (leftOpen) chosenSide = -moveRight;
                else chosenSide = moveRight;

                Vector3 predictWidePos = playerPos + (playerVelocity * predictionTime) + (chosenSide * sideOffsetDistance);

                if (NavMesh.SamplePosition(predictWidePos, out NavMeshHit wideHit, 2.0f, NavMesh.AllAreas))
                    currentTargetPosition = wideHit.position;
                else
                    currentTargetPosition = playerPos + (chosenSide * sideOffsetDistance);

                agent.speed = chaseSpeed;
                break;
        }

        if (NavMesh.SamplePosition(currentTargetPosition, out NavMeshHit finalHit, 5.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(finalHit.position);
        }
    }

    private void DriveNaturally()
    {
        Vector3 desiredVelocity = agent.desiredVelocity;

        // [비상 제동용 Raycast (안전장치)]
        if (currentApproach == ApproachType.RearNarrow)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(rayOrigin, transform.forward, out hit, 5.0f))
            {
                if (hit.transform.GetComponentInParent<BicycleVehicle>() == targetPlayer)
                    agent.speed = Mathf.Min(agent.speed, targetPlayer.currentSpeed * 0.5f);
            }
        }

        if (desiredVelocity.sqrMagnitude > 0.1f)
        {
            desiredVelocity.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    //private void CheckIfArrivedAtDestination()
    //{
    //    if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
    //    {
    //        // 1. 바이크의 실제 이동 방향 다시 계산
    //        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();
    //        Vector3 playerVelocity = playerRb != null ? playerRb.linearVelocity : targetPlayer.transform.forward * targetPlayer.currentSpeed;
    //        playerVelocity.y = 0;
    //        Vector3 moveDirection = playerVelocity.magnitude > 1.0f ? playerVelocity.normalized : targetPlayer.transform.forward;

    //        float facingDot = Vector3.Dot(transform.forward, moveDirection);

    //        bool isRealHeadOn = (currentApproach == ApproachType.Front) && (facingDot < -0.2f);

    //        // 후방 좁은 길(RearNarrow)에서 따라가다 멈추는 상황
    //        bool isRearNarrowStop = (currentApproach == ApproachType.RearNarrow);

    //        if (isRealHeadOn || isRearNarrowStop)
    //        {
    //            // 역주행으로 마주쳤거나, 좁은 골목 꼬리잡기 중이면 그대로 제자리 급정거
    //            StartCoroutine(ImmediateStopRoutine());
    //        }
    //        else
    //        {
    //            // 그 외: RearWide에서 시작해 추월에 성공했거나(방향이 같음), 측면 합류(Side)인 경우
    //            // 무조건 90도로 꺾어서 길을 막는 차단 연출 실행!
    //            StartCoroutine(BlockRoadRoutine());
    //        }
    //    }
    //}

    private void CheckIfArrivedAtDestination()
    {
        // pathPending 체크와 함께 남아있는 거리가 도착 거리 내인지 확인
        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            // ★ [핵심 1] 이동 및 관성을 그 즉시 0으로 강제 초기화! (미끄러짐 방지)
            agent.velocity = Vector3.zero;
            agent.isStopped = true;

            // ★ [핵심 2] 목적지를 현재 내 위치로 바꿔서 더 이상 경로 이동을 계산하지 않게 만듦
            agent.SetDestination(transform.position);

            // 정면 충돌 판별 로직
            float facingDot = Vector3.Dot(transform.forward, targetPlayer.transform.forward);
            bool isRealHeadOn = (currentApproach == ApproachType.Front) && (facingDot < -0.2f);
            bool isRearNarrowStop = (currentApproach == ApproachType.RearNarrow);

            if (isRealHeadOn || isRearNarrowStop)
            {
                StartCoroutine(ImmediateStopRoutine());
            }
            else
            {
                StartCoroutine(BlockRoadRoutine());
            }
        }
    }

    /// <summary>
    /// 상황 1, 3-(1): 제자리에 바로 멈춘 후 하차
    /// </summary>
    private IEnumerator ImmediateStopRoutine()
    {
        currentState = PoliceState.Blocking;
        agent.isStopped = true;

        Debug.Log("<color=yellow>[경찰 AI]</color> 급정거! 즉시 하차합니다.");

        // 브레이크 잡는 아주 짧은 딜레이
        yield return new WaitForSeconds(0.3f);
        DeployOfficer();
    }

    /// <summary>
    /// 상황 2, 3-(2): 플레이어 앞을 막아서며 90도로 차단 연출 후 하차
    /// </summary>
    private IEnumerator BlockRoadRoutine()
    {
        currentState = PoliceState.Blocking;
        agent.isStopped = true;

        Vector3 playerForward = targetPlayer.transform.forward;
        playerForward.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(-playerForward) * Quaternion.Euler(0, blockAngleOffset, 0);

        float elapsed = 0f;
        float blockDuration = 0.6f;
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
                Debug.Log("<color=cyan>[경찰 AI 차량]</color> 경찰관 하차! 캐릭터 추격 개시.");
                officerAI.StartFootChase();
            }
        }
    }
}