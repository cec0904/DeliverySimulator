using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using rayzngames;

public class PoliceCarAI : MonoBehaviour
{
    public enum PoliceState { Idle, Intercept, Blocking, OfficerDeployed}

    [Header("AI 상태 및 목표")]
    public PoliceState currentState = PoliceState.Idle;
    public BicycleVehicle targetPlayer;

    [Header("주행 및 회전 설정")]
    [Tooltip("몇 초 뒤의 플레이어 위치를 예측")]
    [SerializeField] private float predictionTime = 3.0f;

    [SerializeField] private float chaseSpeed = 25f;

    [SerializeField] private float turnSpeed = 5.0f;

    [SerializeField] private float arrivalDistance = 5.0f;

    [Header("차단 연동 설정")]
    [Tooltip("길목 도달 후 길을 막아설 방향 (플레이어 진행 방향의 직각)")]
    [SerializeField] private float blockAngleOffset = 90f;

    //[Tooltip("차단 시 앞으로 미끄러지며(직진하며) 회전할 거리")]

    //[SerializeField] private float blockSlideDistance = 4.0f;

    [Header("경찰관 하차 설정")]
    [Tooltip("차에서 내려 추격을 시작할 경찰관 프리팹")]
    [SerializeField] private GameObject policeOfficerPrefab;
    [Tooltip("경찰관이 생성될 문 위치 (차량 좌측 문 위치에 빈 게임오브젝트 배치)")]
    [SerializeField] private Transform doorSpawnPoint;

    private NavMeshAgent agent;
    private Vector3 currentTargetPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // 자연스러운 자동차 회전을 위해 NavMesh의 자동 회전을 끕니다.
        agent.updateRotation = false;

        // 경찰차의 회피 우선순위를 높여 다른 AI가 비키도록 유도 (값이 낮을수록 우선순위 높음)
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
                CheckIfArrivedAtBlockPoint();
                break;

            case PoliceState.Blocking:
                // 차단 완료 후에는 움직이지 않음
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

        Debug.Log("<color=red>[경찰 AI]</color> 과속 차량 감지! 길목 차단을 위해 출동합니다.");
    }

    private void UpdateInterceptDestination()
    {
        if (targetPlayer == null) return;

        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();
        Vector3 playerVelocity = playerRb != null ? playerRb.linearVelocity : targetPlayer.transform.forward * targetPlayer.currentSpeed;

        if (playerVelocity.magnitude < 0.5f)
        {
            currentTargetPosition = targetPlayer.transform.position;
        }
        else
        {
            currentTargetPosition = targetPlayer.transform.position + (playerVelocity * predictionTime);
        }

        if (NavMesh.SamplePosition(currentTargetPosition, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// 자동차처럼 자연스럽게 코너를 도는 조향 로직
    /// </summary>
    private void DriveNaturally()
    {
        // 에이전트가 가고자 하는 방향(경로) 확인
        Vector3 desiredVelocity = agent.desiredVelocity;

        // 속도가 있을 때만 방향 전환 (제자리 회전 방지)
        if (desiredVelocity.sqrMagnitude > 0.1f)
        {
            desiredVelocity.y = 0; // 상하 회전 방지
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity);

            // Slerp를 이용해 부드럽게 핸들을 꺾는 연출
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void CheckIfArrivedAtBlockPoint()
    {
        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            StartCoroutine(BlockRoadRoutine());
        }
    }

    /// <summary>
    /// 제자리 팽이 회전이 아닌, 앞으로 쏠리며 90도로 차를 꺾는 자연스러운 차단 연출
    /// </summary>
    private IEnumerator BlockRoadRoutine()
    {
        currentState = PoliceState.Blocking;

        // NavMesh 이동 종료
        agent.isStopped = true;

        // 플레이어의 진행 방향 
        Vector3 playerForward = targetPlayer.transform.forward;
        playerForward.y = 0;

        // 차단할 최종 회전 목표 (플레이어 진행 방향의 90도)
        Quaternion targetRotation = Quaternion.LookRotation(-playerForward) * Quaternion.Euler(0, blockAngleOffset, 0);

        // 차단 시 조금 앞으로 전진할 위치 (브레이크 밀림 연출)
        //Vector3 slideTargetPosition = transform.position + (transform.forward * blockSlideDistance);

        float elapsed = 0f;
        float blockDuration = 0.6f;
        //Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (elapsed < blockDuration)
        {
            float t = elapsed / blockDuration;
            // Ease-Out 곡선 적용 (갈수록 느려짐)
            float easeOutT = 1f - Mathf.Pow(1f - t, 3f);

            // 회전과 동시에 앞으로 살짝 미끄러지며 이동
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easeOutT);
            //transform.position = Vector3.Lerp(startPosition, slideTargetPosition, easeOutT);

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

                // targetPlayer가 Transform일 경우 targetPlayer 그대로 전달
                // targetPlayer가 기존 BicycleVehicle 형태라면 targetPlayer.transform 전달
                Transform targetTransform = targetPlayer != null ? targetPlayer.transform : null;

                officerAI.StartFootChase(targetTransform);
            }
        }
        else
        {
            Debug.LogWarning("경찰관 프리팹 또는 문 스폰 위치가 할당되지 않았습니다.");
        }
    }

}