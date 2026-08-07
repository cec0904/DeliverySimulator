using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrafficSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPointData
    {
        public Transform point;
        [HideInInspector] public float timer;
        [HideInInspector] public float currentInterval;
    }

    [Header("Spawn Points")]
    public List<SpawnPointData> spawnPointDatas = new List<SpawnPointData>();

    [Header("Spawn Interval Settings")]
    [SerializeField] private float minSpawnInterval = 6f;  // 최소 대기 시간
    [SerializeField] private float maxSpawnInterval = 12f; // 최대 대기 시간

    private void Start()
    {
        foreach (var spData in spawnPointDatas)
        {
            SetRandomInterval(spData);
            spData.timer = Random.Range(0f, spData.currentInterval);
        }
    }

    private void Update()
    {
        foreach (var spData in spawnPointDatas)
        {
            if (spData.point == null) continue;

            spData.timer += Time.deltaTime;
            if (spData.timer >= spData.currentInterval)
            {
                SpawnVehicleAt(spData.point);
                spData.timer = 0f;
                SetRandomInterval(spData);
            }
        }
    }

    private void SetRandomInterval(SpawnPointData spData)
    {
        spData.currentInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnVehicleAt(Transform spawnPoint)
    {
        if (TrafficObjectPool.Instance == null) return;

        int totalCarTypes = TrafficObjectPool.Instance.CarPrefabCount;
        if (totalCarTypes == 0) return;

        int randomPrefabIndex = Random.Range(0, totalCarTypes);

        TrafficObjectPool.Instance.GetCar(randomPrefabIndex, spawnPoint.position, spawnPoint.rotation);
    }
}