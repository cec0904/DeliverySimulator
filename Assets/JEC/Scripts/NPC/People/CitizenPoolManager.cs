using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class CitizenPoolManager : MonoBehaviour
{
    [Header("Citizen")]
    [SerializeField] private CitizenAI[] citizenPrefab;
    [SerializeField] private int citizenCount = 100;

    [Header("Points")]
    [SerializeField] private Transform[] poiPoints;
    [SerializeField] private Transform[] entrancePoints;

    [Header("NavMesh")]
    [SerializeField] private float entranceSampleDistance = 0.5f;

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
        for (int i = 0; i < citizenCount; i++)
        {
            if (!TryGetRandomEntrancePosition(out Vector3 spawnPosition))
            {
                Debug.LogError($"{name}: 시민을 생성할 Entrance Point를 찾지 못했습니다.", this);
                return;
            }

            //CitizenAI citizen = Instantiate(citizenPrefab, spawnPosition, Quaternion.identity, transform);
            //citizen.InitializePool(this, poiPoints);
            //citizen.name = $"Citizen_{i + 1}";

            CitizenAI selectedPrefab = citizenPrefab[Random.Range(0, citizenPrefab.Length)];

            if(selectedPrefab == null)
            {
                Debug.LogError($"{name}: Citizen Prefab 이 비어있습니다.", this);
                return;
            }

            CitizenAI citizen = Instantiate(selectedPrefab, spawnPosition, selectedPrefab.transform.rotation, transform);
            citizen.InitializePool(this, poiPoints);
            citizen.name = $"{selectedPrefab.name}_{i + 1}";
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

            if (!NavMesh.SamplePosition(entrancePoint.position, out NavMeshHit hit, entranceSampleDistance, NavMesh.AllAreas))
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