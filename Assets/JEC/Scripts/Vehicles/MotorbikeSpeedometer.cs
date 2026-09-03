using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MotorbikeSpeedometer : MonoBehaviour
{
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text unitText;
    [SerializeField] private string unitLabel = "km/h";
    [SerializeField] private bool showUnit = true;
    [Tooltip("Uses the parent phone controller when left empty.")]
    [SerializeField] private PhoneUIController phoneUIController;
    private int displayedSpeed = int.MinValue;

    private void Awake()
    {
        if (unitText != null)
        {
            unitText.text = unitLabel;
            unitText.gameObject.SetActive(showUnit);
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (phoneUIController == null)
        {
            phoneUIController = GetComponentInParent<PhoneUIController>(true);
        }

        if (phoneUIController != null)
        {
            // Hide before the opening animation moves the phone, regardless of Update order.
            phoneUIController.TransitionStarted += RefreshDisplay;
            phoneUIController.StateChanged += RefreshDisplay;
        }

        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (phoneUIController != null)
        {
            phoneUIController.TransitionStarted -= RefreshDisplay;
            phoneUIController.StateChanged -= RefreshDisplay;
        }

        SetVisible(false);
    }

    private void Update()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        MotorbikeMount motorbike = MotorbikeMount.MountedBike;
        bool phoneClosed = phoneUIController == null ||
                           (!phoneUIController.IsOpen && !phoneUIController.IsAnimating);
        bool visible = motorbike != null && motorbike.IsMounted && motorbike.Bicycle != null && phoneClosed;
        SetVisible(visible);

        if (!visible || speedText == null)
        {
            return;
        }

        int roundedSpeed = Mathf.Max(0, Mathf.RoundToInt(motorbike.Bicycle.currentSpeedKmh));

        if (roundedSpeed != displayedSpeed)
        {
            displayedSpeed = roundedSpeed;
            speedText.text = roundedSpeed.ToString();
        }
    }

    private void SetVisible(bool visible)
    {
        if (contentGroup == null)
        {
            return;
        }

        contentGroup.alpha = visible ? 1f : 0f;
        contentGroup.interactable = false;
        contentGroup.blocksRaycasts = false;
    }

    private void OnValidate()
    {
        if (unitText != null)
        {
            unitText.text = unitLabel;
            unitText.gameObject.SetActive(showUnit);
        }
    }
}
