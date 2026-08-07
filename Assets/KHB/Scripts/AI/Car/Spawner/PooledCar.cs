using UnityEngine;

public class PooledCar : MonoBehaviour
{
    public int PrefabIndex { get; set; }

    public void Despawn()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 풀 매니저로 반납
        TrafficObjectPool.Instance.ReturnCar(gameObject, PrefabIndex);
    }
}