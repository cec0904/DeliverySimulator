//using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class QuestOfferUI : MonoBehaviour
{
    [Header("[ UI 컴포넌트 연결 ]")]
    [SerializeField] private GameObject offerPanel;        // 팝업 전체 패널
    [SerializeField] private TextMeshProUGUI titleText;    // 퀘스트 제목/정보 텍스트
    [SerializeField] private TextMeshProUGUI contentsText;

    [SerializeField] private Image timerBarFill;            // timner 바

    [SerializeField] private Button acceptButton;          // 수락 버튼
    [SerializeField] private Button declineButton;        // 거절 버튼

    [Header("[ 시간 설정 ]")]
    [SerializeField] private float timeLimit = 10.0f;      // 대기 시간 (10초)

    private Coroutine timerCoroutine;
    private System.Action<bool> onDecisionCallback;       // true: 수락, false: 거절

    private void Start()
    {
        if (offerPanel != null) offerPanel.SetActive(false);

        // 버튼 리스너 연결
        if (acceptButton != null) acceptButton.onClick.AddListener(OnClickAccept);
        if (declineButton != null) declineButton.onClick.AddListener(OnClickDecline);
    }

    public void ShowQuestOffer(QuestDataSO quest, System.Action<bool> callback)
    {
        if (quest == null || offerPanel == null) return;

        onDecisionCallback = callback;

        if (titleText != null)
        {
            titleText.text = $"<b>[{quest.questTitle}]</b>\n보상: {quest.rewardGold} G | 제한시간: {quest.timeLimit}초";
        }

        if (contentsText != null)
        {
            contentsText.text = $"<b>배달 품목:</b> {quest.deliveryItemName}\n" +
                                $"<b>목적지:</b> {quest.deliveryLocation}\n\n" +
                                $"{quest.questDescription}";
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = 1.0f;
        }

        offerPanel.SetActive(true);

        // 이전 타이머가 있다면 멈추고 새 카운트다운 시작
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(Co_TimerRoutine());
    }

    private IEnumerator Co_TimerRoutine()
    {
        float currentTimer = timeLimit;

        if (timerBarFill != null)
        {
            timerBarFill.color = Color.green; // 또는 원하시는 기본 Color 지정
        }

        while (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;

            if (timerBarFill != null)
            {
                float fillRatio = Mathf.Clamp01(currentTimer / timeLimit);
                timerBarFill.fillAmount = fillRatio;

                if (timerBarFill.fillAmount < 0.3f)
                {
                    float pingPong = Mathf.PingPong(Time.time * 5.0f, 1.0f);
                    timerBarFill.color = Color.Lerp(Color.red, Color.yellow, pingPong);
                }
                else
                {
                    timerBarFill.color = Color.green;

                }

            }

            yield return null;
        }

        if (timerBarFill != null) timerBarFill.fillAmount = 0.0f;

        // 10초 동안 아무 입력이 없었을 경우 -> 자동 거절 처리!
        Debug.Log("[타임아웃] 10초간 응답이 없어 의뢰가 자동으로 거절되었습니다.");
        FinalizeDecision(false);
    }

    // [수락] 버튼 눌렀을 때
    private void OnClickAccept()
    {
        FinalizeDecision(true);
    }

    // [거절] 버튼 눌렀을 때
    private void OnClickDecline()
    {
        FinalizeDecision(false);
    }

    // 결정 마무리 함수
    private void FinalizeDecision(bool isAccepted)
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        offerPanel.SetActive(false); // 팝업 닫기
        onDecisionCallback?.Invoke(isAccepted); // 결과를 매니저에게 전달
    }
}
