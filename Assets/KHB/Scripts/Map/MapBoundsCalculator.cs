using UnityEngine;

[ExecuteInEditMode]
public class MapBoundsCalculator : MonoBehaviour
{
    public Camera captureCamera;

    [ContextMenu("Calculate World Bounds")]
    public void CalculateBounds()
    {
        if (captureCamera == null)
            captureCamera = GetComponent<Camera>();

        if (captureCamera == null || !captureCamera.orthographic)
        {
            Debug.LogError("🚨 Orthographic(직교) 카메라를 할당해 주세요!");
            return;
        }

        Vector3 camPos = captureCamera.transform.position;
        float size = captureCamera.orthographicSize;
        float aspect = captureCamera.aspect;

        float minX = camPos.x - (size * aspect);
        float maxX = camPos.x + (size * aspect);
        float minZ = camPos.z - size;
        float maxZ = camPos.z + size;

        Debug.Log($"========================================");
        Debug.Log($"📸 [Map World Bounds 계산 결과]");
        Debug.Log($"• World Min Bounds : X = {minX:F2}, Y = {minZ:F2}");
        Debug.Log($"• World Max Bounds : X = {maxX:F2}, Y = {maxZ:F2}");
        Debug.Log($"========================================");
    }
}