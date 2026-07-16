using UnityEngine;
using UnityEngine.UI;

public class UIButtonHitboxResizer : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("이 값보다 알파(투명도)가 낮은 영역은 클릭을 무시합니다. (0이면 전부 클릭, 1이면 완전 불투명만 클릭)")]
    public float alphaThreshold = 0.5f;

    void Start()
    {
        Image[] childImages = GetComponentsInChildren<Image>(true);

        foreach (Image img in childImages)
        {
            img.alphaHitTestMinimumThreshold = alphaThreshold;
        }
    }
}
