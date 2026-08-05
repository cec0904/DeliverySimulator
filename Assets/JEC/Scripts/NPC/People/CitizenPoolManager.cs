using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class CitizenPoolManager : MonoBehaviour
{
    // 플레이어 참조
    [SerializeField] private Transform player;


    // 풀에 사용할 시민 프리팹
    [Header("Citizen")]
    [SerializeField] private CitizenAI[] citizenPrefab;
    [SerializeField] private int citizenCount = 100;

    // entrance 검색
    // poi 도 검색
    [Header("Points")]
    [SerializeField] private Transform[] poiPoints;
    [SerializeField] private Transform[] entrancePoints;

    // npc 와 entrance 의 최소 거리
    [Header("NavMesh")]
    [SerializeField] private float entranceDistance = 0.5f;


    // 플레이어 반경 최소 최대 거리. radius 사용
    // 카메라 말고 실제 플레이어 캐릭터 기준
    [Header("PlayerRadius")]
    [SerializeField] private float MinPlayerRadius = 20.0f;
    [SerializeField] private float MaxPlayerRadius = 100.0f;




    // 최소 최대거리 안에 있는 npc 들을 활성화
    // 무작정 entrance 에서 활성화 하는게 아님.
    // 그러면 여기서 radius 안에 내비메시 안에 무작위 포인트에서 생성시켜 주면 되겠다.
    private Vector3[] NavMeshPoints;


    // 플레이어 주변에 필요한 수 만큼 활성화 미리 시켜둠
    // 목표 npc 와 실제 npc 값 비교
    // 목표 npc > 실제 npc ? 목표가 될 때까지 실제 npc 늘리기 : 그냥 냅두기
    [Header("CitizenDensity")]
    [SerializeField] private int MaxDensity = 70;
    [SerializeField] private int MinDensity = 40;     // 초기 npc 생성 수

    // 이 npc들을 플레이어 범위안에 다 보여줄 것인가 아니면 안보이는 곳들은 계속 풀에 둘 것인가



    // 최대거리 벗어났을 때
    // 카메라에 보인다 ?
    // entrance 로 가서 반환
    // 반환할 때 npc 근방에 있는 entrance 로 반환시키기

    // 카메라 및 화면에 아예 안보인다 ?
    // 즉시 반환

    private void Start()
    {
        if (!ValidateSettings())
        {
            enabled = false;
            return;
        }

        CreateCitizens();
    }

    private bool ValidateSettings()
    {
        if (citizenPrefab == null || citizenPrefab.Length == 0)
        {
            Debug.LogError($"{name}: Citizen Prefab이 지정되지 않았습니다.", this);
            return false;
        }

        if (citizenCount <= 0)
        {
            Debug.LogError($"{name}: Citizen Count는 1 이상이어야 합니다.", this);
            return false;
        }

        if (poiPoints == null || poiPoints.Length == 0)
        {
            Debug.LogError($"{name}: POI가 등록되지 않았습니다.", this);
            return false;
        }

        if (entrancePoints == null || entrancePoints.Length == 0)
        {
            Debug.LogError($"{name}: Entrance Point가 등록되지 않았습니다.", this);
            return false;
        }

        return true;
    }

    private void CreateCitizens()
    {
        NavMeshPoints = new Vector3[MinDensity];

        int NavMeshPointCount = 0;

        // 내비메시에 무작위 포인트 찍기
        int attemptCount = MinDensity * 10;

        for (int i = 0; i < attemptCount; i++)
        {
            if (NavMeshPointCount >= MinDensity)
            {
                break;
            }

            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(MinPlayerRadius, MaxPlayerRadius);

            Vector3 candidate = player.position + new Vector3(direction.x, 0f, direction.y) * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, entranceDistance, NavMesh.AllAreas))
            {
                continue;
            }

            NavMeshPoints[NavMeshPointCount] = hit.position;

            Vector3 spawnPosition = hit.position;

            // NavMeshPoint 또는 Entrance에서 시민 생성
            if (Random.Range(0, 2) == 0)
            {
                if (TryGetRandomEntrancePosition(out Vector3 entrancePosition))
                {
                    spawnPosition = entrancePosition;
                }
            }

            SpawnCitizen(spawnPosition);

            NavMeshPointCount++;
        }
    }

    private void SpawnCitizen(Vector3 spawnPosition)
    {
        // 시민과 플레이어의 거리
        float citizenDistance = Vector3.Distance(player.position, spawnPosition);

        CitizenAI selectedPrefab = citizenPrefab[Random.Range(0, citizenPrefab.Length)];


        if (selectedPrefab == null)
        {
            Debug.LogError($"{name}: Citizen Prefab 이 비어있습니다.", this);
            return;
        }


        CitizenAI citizen = Instantiate(selectedPrefab, spawnPosition, selectedPrefab.transform.rotation, transform);

        

        // 위에서 선언한 변수가 Radius 안에 들어와있는가를 확인
        if (citizenDistance < MaxPlayerRadius && citizenDistance > MinPlayerRadius)
        {
            citizen.InitializePool(this, poiPoints);
            citizen.name = $"{selectedPrefab.name}_Citizen";
            citizen.gameObject.SetActive(true);
        }
    }

    public bool TryGetRandomEntrancePosition(out Vector3 position)
    {
        position = Vector3.zero;

        if (entrancePoints == null || entrancePoints.Length == 0)
        {
            return false;
        }

        int attemptCount = entrancePoints.Length * 2;

        for (int i = 0; i < attemptCount; i++)
        {
            int randomIndex = Random.Range(0, entrancePoints.Length);
            Transform entrancePoint = entrancePoints[randomIndex];

            if (entrancePoint == null)
            {
                continue;
            }

            if (!NavMesh.SamplePosition(entrancePoint.position, out NavMeshHit hit, entranceDistance, NavMesh.AllAreas))
            {
                continue;
            }

            position = hit.position;
            return true;
        }

        return false;
    }

    public void RecycleCitizen(CitizenAI citizen)
    {
        if (citizen == null)
        {
            return;
        }

        if (!TryGetRandomEntrancePosition(out Vector3 spawnPosition))
        {
            Debug.LogError($"{name}: 시민을 재배치할 Entrance Point를 찾지 못했습니다.", this);
            return;
        }

        citizen.gameObject.SetActive(false);
        citizen.transform.position = spawnPosition;
        citizen.gameObject.SetActive(true);

        citizen.SpawnFromEntrance(spawnPosition);
    }

}