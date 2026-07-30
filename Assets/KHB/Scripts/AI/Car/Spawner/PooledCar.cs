using UnityEngine;

public class PooledCar : MonoBehaviour
{
    public int PrefabIndex { get; set; }

    public void Despawn()
    {
        // 물리 속도/각속도가 남아있다면 리셋 (Rigidbody 사용 시)
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 풀 매니저로 반납
        TrafficObjectPool.Instance.ReturnCar(gameObject, PrefabIndex);
    }
}