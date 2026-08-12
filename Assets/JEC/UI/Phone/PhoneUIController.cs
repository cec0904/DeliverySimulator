using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.CullingGroup;

public class PhoneUIController : MonoBehaviour
{
    [Header("움직일 UI")]
    [SerializeField] private RectTransform phoneMotionRoot;

    [Header("상승 연출")]
    [SerializeField] private float liftDistance = 100f;
    [SerializeField] private float liftDuration = 0.2f;

    [Header("열린 상태")]
    [SerializeField] private Vector2 openPosition = Vector2.zero;
    [SerializeField] private float openRotationZ = 90f;
    [SerializeField] private Vector3 openScale = new Vector3(2.1f, 2.1f, 2.1f);

    [Header("열기·닫기 연출")]
    [SerializeField] private float transitionDuration = 0.55f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector2 closedPosition;
    private float closedRotationZ;
    private Vector3 closedScale;

    private bool isOpen;
    private bool isAnimating;

    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;

    public event Action StateChanged;

    private void Awake()
    {
        if (phoneMotionRoot == null)
        {
            Debug.LogError("[PhoneUIController] PhoneMotionRoot가 연결되지 않았습니다.", this);

            enabled = false;
            return;
        }

        // 현재 Inspector에서 맞춰놓은 상태를 닫힌 상태로 저장
        closedPosition = phoneMotionRoot.anchoredPosition;
        closedRotationZ = phoneMotionRoot.localEulerAngles.z;
        closedScale = phoneMotionRoot.localScale;
    }

    public void TogglePhone()
    {
        if (isAnimating)
        {
            return;
        }

        if (isOpen)
        {
            StartCoroutine(ClosePhoneRoutine());
        }
        else
        {
            StartCoroutine(OpenPhoneRoutine());
        }
    }

    public void OpenPhone()
    {
        if (isAnimating || isOpen)
        {
            return;
        }

        StartCoroutine(OpenPhoneRoutine());
    }

    public void ClosePhone()
    {
        if (isAnimating || !isOpen)
        {
            return;
        }
            

        StartCoroutine(ClosePhoneRoutine());
    }

    private IEnumerator OpenPhoneRoutine()
    {
        isAnimating = true;

        Vector2 liftedPosition = closedPosition + Vector2.up * liftDistance;

        // 1단계: 현재 위치에서 살짝 위로 올라오기
        yield return AnimateTo(liftedPosition, closedRotationZ, closedScale, liftDuration);

        // 2단계: 중앙 이동 + 반시계 90도 회전 + 확대
        yield return AnimateTo(openPosition, openRotationZ, openScale, transitionDuration);

        isOpen = true;
        isAnimating = false;

        StateChanged?.Invoke();
    }

    private IEnumerator ClosePhoneRoutine()
    {
        isAnimating = true;

        Vector2 liftedPosition = closedPosition + Vector2.up * liftDistance;

        // 열기 연출의 역순
        yield return AnimateTo(liftedPosition, closedRotationZ, closedScale, transitionDuration);

        yield return AnimateTo(closedPosition, closedRotationZ, closedScale, liftDuration);

        isOpen = false;
        isAnimating = false;

        StateChanged?.Invoke();
    }

    private IEnumerator AnimateTo(Vector2 targetPosition, float targetRotationZ, Vector3 targetScale, float duration)
    {
        Vector2 startPosition = phoneMotionRoot.anchoredPosition;
        float startRotationZ = phoneMotionRoot.localEulerAngles.z;
        Vector3 startScale = phoneMotionRoot.localScale;

        if (duration <= 0f)
        {
            ApplyPose(targetPosition, targetRotationZ, targetScale);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

            float curveTime = transitionCurve.Evaluate(normalizedTime);

            phoneMotionRoot.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveTime);

            float rotationZ = Mathf.LerpAngle(startRotationZ, targetRotationZ, curveTime);

            phoneMotionRoot.localRotation =Quaternion.Euler(0f, 0f, rotationZ);

            phoneMotionRoot.localScale = Vector3.Lerp(startScale, targetScale, curveTime);

            yield return null;
        }

        ApplyPose(targetPosition, targetRotationZ, targetScale);
    }

    private void ApplyPose( Vector2 position, float rotationZ, Vector3 scale)
    {
        phoneMotionRoot.anchoredPosition = position;
        phoneMotionRoot.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        phoneMotionRoot.localScale = scale;
    }
}