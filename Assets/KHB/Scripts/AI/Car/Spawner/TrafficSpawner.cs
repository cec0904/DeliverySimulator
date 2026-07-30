using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public float spawnInterval = 3f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnVehicle();
            timer = 0f;
        }
    }

    private void SpawnVehicle()
    {
        if (spawnPoints.Length == 0) return;

        // 랜덤 위치 및 랜덤 차량 종류 선택
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        int totalCarTypes = TrafficObjectPool.Instance.CarPrefabCount;
        if (totalCarTypes == 0) return;

        int randomPrefabIndex = Random.Range(0, totalCarTypes);

        // 풀에서 꺼내기
        TrafficObjectPool.Instance.GetCar(randomPrefabIndex, spawnPoint.position, spawnPoint.rotation);
    }
}