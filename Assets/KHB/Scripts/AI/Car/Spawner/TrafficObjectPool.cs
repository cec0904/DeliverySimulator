using System.Collections.Generic;
using UnityEngine;

public class TrafficObjectPool : MonoBehaviour
{
    public static TrafficObjectPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject[] carPrefabs; // 풀링할 자동차 프리팹들
    [SerializeField] private int initialPoolSizePerPrefab = 5; // 프리팹당 초기 생성 개수

    // 차종별로 비활성화된 차량들을 관리할 딕셔너리 (또는 단일 큐/리스트)
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    public int CarPrefabCount => carPrefabs != null ? carPrefabs.Length : 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < carPrefabs.Length; i++)
        {
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int j = 0; j < initialPoolSizePerPrefab; j++)
            {
                GameObject obj = CreateNewCar(i);
                objectQueue.Enqueue(obj);
            }

            poolDictionary.Add(i, objectQueue);
        }
    }

    // 새로운 차량 오브젝트 인스턴스 생성 헬퍼 함수
    private GameObject CreateNewCar(int prefabIndex)
    {
        GameObject obj = Instantiate(carPrefabs[prefabIndex], transform);

        // 풀 소속 식별용 컴포넌트 데이터 주입
        PooledCar pooledCar = obj.GetComponent<PooledCar>();
        if (pooledCar == null)
        {
            pooledCar = obj.AddComponent<PooledCar>();
        }
        pooledCar.PrefabIndex = prefabIndex;

        obj.SetActive(false);
        return obj;
    }

    public GameObject GetCar(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefabIndex))
        {
            Debug.LogWarning($"Index {prefabIndex}에 해당하는 프리팹 풀이 없습니다.");
            return null;
        }

        GameObject carToSpawn;

        // 풀에 여유가 없으면 새로 생성해서 제공
        if (poolDictionary[prefabIndex].Count == 0)
        {
            carToSpawn = CreateNewCar(prefabIndex);
        }
        else
        {
            carToSpawn = poolDictionary[prefabIndex].Dequeue();
        }

        carToSpawn.transform.SetPositionAndRotation(position, rotation);
        carToSpawn.SetActive(true);

        return carToSpawn;
    }

    public void ReturnCar(GameObject car, int prefabIndex)
    {
        car.SetActive(false);
        car.transform.SetParent(transform, false);

        if (poolDictionary.ContainsKey(prefabIndex))
        {
            poolDictionary[prefabIndex].Enqueue(car);
        }
        else
        {
            Destroy(car);
        }
    }
}