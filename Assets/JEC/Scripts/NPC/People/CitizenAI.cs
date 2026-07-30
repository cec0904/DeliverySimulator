using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum CitizenState
{
    MovingToPOI,
    WaitingAtPOI,
    MovingToEntrance
}

public enum AvoidDirection
{
    Front = 0,
    Back = 1,
    FrontRight = 2,
    FrontLeft = 3,
    BackRight = 4,
    BackLeft = 5
}

public enum CitizenMoveType
{
    BasicWalk = 0,
    PhoneWalking = 1,
    Running = 2,
    SlowWalk = 3,
    Jogging = 4
}

[RequireComponent(typeof(NavMeshAgent))]
public class CitizenAI : MonoBehaviour
{
    [Header("POI")]
    private Transform[] poiPoints;
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float arrivalTolerance = 0.15f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool isMale;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private static readonly int IsMaleHash = Animator.StringToHash("IsMale");

    private NavMeshAgent agent;
    private CitizenState currentState;
    private int currentPoiIndex = -1;
    private float waitEndTime;

    [Header("Collision Avoid")]
    [SerializeField] private float minimumImpactSpeed = 1f;
    [SerializeField] private float avoidDuration = 0.8f;

    private static readonly int IsAvoidingHash = Animator.StringToHash("IsAvoiding");

    private static readonly int AvoidDirectionHash = Animator.StringToHash("AvoidDirection");

    private bool isAvoiding;
    private Coroutine avoidCoroutine;

    [SerializeField] private float avoidRetriggerCooldown = 2f;
    private float nextAvoidAllowedTime;


    [Header("Movement")]
    [SerializeField] private float basicWalkSpeed = 1.8f;
    [SerializeField] private float phoneWalkingSpeed = 1.5f;
    [SerializeField] private float runningSpeed = 3.5f;
    [SerializeField] private float slowWalkSpeed = 1.5f;
    [SerializeField] private float joggingSpeed = 3.0f;

    private static readonly int MoveTypeHash = Animator.StringToHash("MoveType");

    private CitizenMoveType currentMoveType;

    [SerializeField] private float minimumPoiDistance = 0.5f;
    [SerializeField] private float departureGraceTime = 0.2f;
    private float moveStartTime;

    [Header("Pool")]
    [SerializeField, Range(0f, 1f)] private float returnToEntranceChance = 0.2f;

    private CitizenPoolManager poolManager;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError($"{name}: Animator가 지정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        animator.SetBool(IsMaleHash, isMale);
        animator.SetBool(IsMovingHash, false);
        animator.SetBool(IsAvoidingHash, false);
    }

    private void Start()
    {
        if (poiPoints == null || poiPoints.Length == 0)
        {
            Debug.LogError($"{name}: POI가 하나도 등록되지 않았습니다.", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError($"{name}: 시민이 NavMesh 위에 없습니다. 파란 영역 위로 옮겨주세요.", this);

            enabled = false;
            return;
        }


        animator.SetBool(IsMaleHash, isMale);

        SelectRandomMoveType();
        MoveToNextPOI();
    }

    private void Update()
    {
        if (isAvoiding)
        {
            return;
        }

        switch (currentState)
        {
            case CitizenState.MovingToPOI:
                UpdateMoving();
                break;

            case CitizenState.WaitingAtPOI:
                UpdateWaiting();
                break;

            case CitizenState.MovingToEntrance:
                UpdateMovingToEntrance();
                break;
        }
    }

    private void UpdateMoving()
    {
        if (Time.time - moveStartTime < departureGraceTime)
        {
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (float.IsInfinity(agent.remainingDistance))
        {
            return;
        }

        bool reachedDestination = agent.remainingDistance <= agent.stoppingDistance + arrivalTolerance;


        if (reachedDestination)
        {
            StartWaiting();
        }
    }

    private void UpdateWaiting()
    {
        if (Time.time < waitEndTime)
        {
            return;
        }

        if (poolManager != null && Random.value < returnToEntranceChance)
        {
            MoveToEntrance();
            return;
        }

        MoveToNextPOI();
    }

    private bool TryGetNextValidPOI(out Transform target)
    {
        target = null;

        if (poiPoints == null || poiPoints.Length == 0)
        {
            return false;
        }

        int attemptCount = poiPoints.Length * 2;

        for (int i = 0; i < attemptCount; i++)
        {
            int randomPoiIndex = Random.Range(0, poiPoints.Length);

            if (poiPoints.Length > 1 && randomPoiIndex == currentPoiIndex)
            {
                continue;
            }

            Transform candidate = poiPoints[randomPoiIndex];

            if (candidate == null)
            {
                continue;
            }

            Vector3 offset = candidate.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= minimumPoiDistance * minimumPoiDistance)
            {
                continue;
            }

            currentPoiIndex = randomPoiIndex;
            target = candidate;

            return true;
        }

        return false;
    }

    private void MoveToNextPOI()
    {
        if (!TryGetNextValidPOI(out Transform target))
        {
            Debug.LogWarning($"{name}: 현재 위치에서 이동할 수 있는 POI가 없습니다. POI 위치가 겹쳐 있는지 확인하세요.", this);
            StartWaiting();
            return;
        }

        agent.isStopped = false;

        if (!agent.SetDestination(target.position))
        {
            Debug.LogError($"{name}: {target.name}으로 가는 목적지 설정에 실패했습니다.", target);
            StartWaiting();
            return;
        }

        currentState = CitizenState.MovingToPOI;
        moveStartTime = Time.time;

        animator.SetBool(IsMovingHash, true);

    }

    private void StartWaiting()
    {
        agent.isStopped = true;
        agent.ResetPath();

        waitEndTime = Time.time + Random.Range(minWaitTime, maxWaitTime);
        currentState = CitizenState.WaitingAtPOI;

        animator.SetBool(IsMovingHash, false);
        animator.SetInteger("IdleType", Random.Range(0, 4));
    }

    private AvoidDirection CalculateAvoidDirection(Vector3 localDirection)
    {
        float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        // 정면 중앙
        if (angle >= -30f && angle < 30f)
        {
            return AvoidDirection.Front;
        }

        // 앞 오른쪽
        if (angle >= 30f && angle < 90f)
        {
            return AvoidDirection.FrontRight;
        }

        // 뒤 오른쪽
        if (angle >= 90f && angle < 150f)
        {
            return AvoidDirection.BackRight;
        }

        // 뒤 중앙
        if (angle >= 150f || angle < -150f)
        {
            return AvoidDirection.Back;
        }

        // 뒤 왼쪽
        if (angle >= -150f && angle < -90f)
        {
            return AvoidDirection.BackLeft;
        }

        // -90도 ~ -30도
        return AvoidDirection.FrontLeft;
    }

    private IEnumerator PlayAvoid(AvoidDirection direction)
    {

        // 방향을 먼저 설정한 후 IsAvoiding을 켜야 한다.
        animator.SetInteger(AvoidDirectionHash, (int)direction);

        animator.SetBool(IsAvoidingHash, true);

        animator.SetBool(IsMovingHash, false);

        // 회피 애니메이션 중에는 NavMeshAgent 이동 정지
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(avoidDuration);

        animator.SetBool(IsAvoidingHash, false);

        bool shouldMove = currentState == CitizenState.MovingToPOI;

        animator.SetBool(IsMovingHash, shouldMove);

        if (agent.isOnNavMesh) agent.isStopped = !shouldMove;

        avoidCoroutine = null;
        isAvoiding = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (animator == null || isAvoiding || Time.time < nextAvoidAllowedTime)
        {
            return;
        }

        if (collision.contactCount == 0)
        {
            return;
        }

        if (collision.relativeVelocity.sqrMagnitude < minimumImpactSpeed * minimumImpactSpeed)
        {
            return;
        }

        Vector3 worldContactPoint = collision.GetContact(0).point;

        Vector3 localContactPoint = transform.InverseTransformPoint(worldContactPoint);

        localContactPoint.y = 0f;

        if (localContactPoint.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        AvoidDirection direction = CalculateAvoidDirection(localContactPoint);

        isAvoiding = true;
        nextAvoidAllowedTime = Time.time + avoidDuration + avoidRetriggerCooldown;
        avoidCoroutine = StartCoroutine(PlayAvoid(direction));
    }

    private void SelectRandomMoveType()
    {
        // 0부터 4까지 총 5개
        currentMoveType = (CitizenMoveType)Random.Range(0, 5);

        animator.SetInteger(MoveTypeHash, (int)currentMoveType);

        switch (currentMoveType)
        {
            case CitizenMoveType.BasicWalk:
                agent.speed = basicWalkSpeed;
                break;

            case CitizenMoveType.PhoneWalking:
                agent.speed = phoneWalkingSpeed;
                break;

            case CitizenMoveType.Running:
                agent.speed = runningSpeed;
                break;

            case CitizenMoveType.SlowWalk:
                agent.speed = slowWalkSpeed;
                break;

            case CitizenMoveType.Jogging:
                agent.speed = joggingSpeed;
                break;
        }
    }



    public void InitializePool(CitizenPoolManager manager, Transform[] sharedPoiPoints)
    {
        poolManager = manager;
        poiPoints = sharedPoiPoints;
    }

    private void MoveToEntrance()
    {
        if (poolManager == null)
        {
            MoveToNextPOI();
            return;
        }

        if (!poolManager.TryGetRandomEntrancePosition(out Vector3 entrancePosition))
        {
            Debug.LogWarning($"{name}: 이동할 Entrance Point를 찾지 못했습니다.", this);
            MoveToNextPOI();
            return;
        }

        agent.isStopped = false;

        if (!agent.SetDestination(entrancePosition))
        {
            Debug.LogWarning($"{name}: Entrance Point로 가는 경로 설정에 실패했습니다.", this);
            MoveToNextPOI();
            return;
        }

        currentState = CitizenState.MovingToEntrance;
        moveStartTime = Time.time;

        animator.SetBool(IsMovingHash, true);
    }

    private void UpdateMovingToEntrance()
    {
        if (Time.time - moveStartTime < departureGraceTime)
        {
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (float.IsInfinity(agent.remainingDistance))
        {
            return;
        }

        bool reachedEntrance = agent.remainingDistance <= agent.stoppingDistance + arrivalTolerance;

        if (!reachedEntrance)
        {
            return;
        }

        if (poolManager == null)
        {
            StartWaiting();
            return;
        }

        poolManager.RecycleCitizen(this);
    }

    public void SpawnFromEntrance(Vector3 spawnPosition)
    {
        avoidCoroutine = null;
        isAvoiding = false;
        nextAvoidAllowedTime = 0f;
        currentPoiIndex = -1;
        waitEndTime = 0f;

        if (!agent.Warp(spawnPosition))
        {
            Debug.LogError($"{name}: Entrance Point로 Warp하지 못했습니다.", this);
            return;
        }

        agent.isStopped = false;
        agent.ResetPath();

        animator.SetBool(IsAvoidingHash, false);
        animator.SetBool(IsMovingHash, false);

        SelectRandomMoveType();
        MoveToNextPOI();
    }
}