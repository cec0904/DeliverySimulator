using UnityEngine;

public class TrafficDespawner : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        
        // 태그 확인
        if (other.CompareTag("Car"))
        {
            if (other.TryGetComponent<PooledCar>(out var car))
            {
               car.Despawn(); // Destroy 대신 풀에 반납!
            }
            else
            {
                // 풀링 오브젝트가 아닌 경우   방어 코드
                 Destroy(other.gameObject);
            }
        }
    }
}