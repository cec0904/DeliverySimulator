using UnityEngine;
using System.IO;

public class MapCapture : MonoBehaviour
{
    public Camera captureCamera;

    public int width = 1280;
    public int height = 720;

    [ContextMenu("Capture Map")]
    public void CaptureMap()
    {
        if (captureCamera == null)
            captureCamera = GetComponent<Camera>();

        // 1280 x 720 Render Texture 생성
        RenderTexture rt = new RenderTexture(
            width,
            height,
            24
        );

        captureCamera.targetTexture = rt;

        RenderTexture.active = rt;
        captureCamera.Render();

        // 1280 x 720 이미지 생성
        Texture2D image = new Texture2D(
            width,
            height,
            TextureFormat.RGB24,
            false
        );

        image.ReadPixels(
            new Rect(0, 0, width, height),
            0,
            0
        );

        image.Apply();

        // 카메라 및 RenderTexture 초기화
        captureCamera.targetTexture = null;
        RenderTexture.active = null;

        DestroyImmediate(rt);

        // PNG 변환
        byte[] bytes = image.EncodeToPNG();

        // Assets/Map.png
        string path = Application.dataPath + "/Map.png";

        File.WriteAllBytes(path, bytes);

        DestroyImmediate(image);

        Debug.Log("====================================");
        Debug.Log("지도 이미지 저장 완료");
        Debug.Log($"해상도 : {width} x {height}");
        Debug.Log($"경로 : {path}");
        Debug.Log("====================================");
    }
}