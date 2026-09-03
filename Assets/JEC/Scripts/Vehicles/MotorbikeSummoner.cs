using System;
using rayzngames;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MotorbikeSummoner : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode summonKey = KeyCode.V;
    [SerializeField, Min(0.1f)] private float holdDuration = 2f;

    [Header("Distance")]
    [SerializeField, Min(0f)] private float existingBikeBlockDistance = 50f;
    [SerializeField, Min(0.1f)] private float summonSearchRadius = 10f;
    [SerializeField, Min(0.1f)] private float minimumSpawnDistance = 3f;
    [SerializeField, Range(8, 64)] private int candidateCount = 24;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayerMask = 65;
    [SerializeField] private LayerMask obstacleLayerMask = ~0;
    [SerializeField, Min(0.1f)] private float groundProbeHeight = 8f;
    [SerializeField, Min(0.1f)] private float groundProbeDepth = 16f;
    [SerializeField, Min(0f)] private float maximumGroundHeightDifference = 2f;
    [SerializeField, Range(0f, 60f)] private float maximumGroundSlope = 30f;
    [SerializeField, Min(0f)] private float placementClearance = 0.08f;

    [Header("Bike")]
    [SerializeField] private GameObject motorbikePrefab;

    [Header("Notification")]
    [SerializeField, Min(0f)] private float notificationDuration = 3f;

    private MotorbikeMount currentBike;
    private float heldTime;
    private bool holdConsumed;

    private void Start()
    {
        ResolveCurrentBike();
    }

    private void Update()
    {
        AdvanceHold(Input.GetKey(summonKey), Time.deltaTime);
    }

    private void AdvanceHold(bool keyHeld, float deltaTime)
    {
        if (!keyHeld)
        {
            heldTime = 0f;
            holdConsumed = false;
            return;
        }

        if (RespawnManager.IsTransitionActive)
        {
            heldTime = 0f;

            if (keyHeld)
            {
                holdConsumed = true;
            }

            return;
        }

        if (holdConsumed)
        {
            return;
        }

        heldTime += Mathf.Max(0f, deltaTime);

        if (heldTime < holdDuration)
        {
            return;
        }

        holdConsumed = true;
        TrySummon();
    }

    private void TrySummon()
    {
        if (RespawnManager.IsTransitionActive) return;
        ResolveCurrentBike();

        if (currentBike != null && currentBike.IsMounted)
        {
            ShowNotification(false);
            return;
        }

        if (currentBike != null)
        {
            float blockDistanceSquared = existingBikeBlockDistance * existingBikeBlockDistance;

            if ((currentBike.transform.position - transform.position).sqrMagnitude < blockDistanceSquared)
            {
                ShowNotification(false);
                return;
            }
        }

        GameObject boundsSource = currentBike != null ? currentBike.gameObject : motorbikePrefab;
        if (boundsSource == null || boundsSource.GetComponent<BicycleVehicle>() == null ||
            !MotorbikePlacementBounds.TryGetLocalBounds(boundsSource, out Bounds localBikeBounds))
        {
            Debug.LogError("MotorbikeSummoner: 유효한 오토바이 프리팹/Collider 참조가 필요합니다.", this);
            ShowNotification(false);
            return;
        }
        Quaternion placementRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (!TryFindSafePlacement(localBikeBounds, placementRotation, out Vector3 placementPosition))
        {
            ShowNotification(false);
            return;
        }

        if (currentBike != null)
        {
            currentBike.gameObject.SetActive(true);
            if (!currentBike.gameObject.activeInHierarchy)
            {
                ShowNotification(false);
                return;
            }
            currentBike.Relocate(placementPosition, placementRotation);
            ShowNotification(true);
            return;
        }

        if (motorbikePrefab == null)
        {
            Debug.LogError("MotorbikeSummoner의 Motorbike Prefab 참조가 비어 있습니다.", this);
            ShowNotification(false);
            return;
        }

        GameObject spawnedBike = Instantiate(motorbikePrefab, placementPosition, placementRotation);
        currentBike = spawnedBike.GetComponent<MotorbikeMount>();

        if (currentBike == null)
        {
            currentBike = spawnedBike.AddComponent<MotorbikeMount>();
        }

        if (spawnedBike.GetComponent<MotorbikeCrashDetector>() == null)
        {
            spawnedBike.AddComponent<MotorbikeCrashDetector>();
        }

        ShowNotification(true);
    }

    private void ResolveCurrentBike()
    {
        if (MotorbikeMount.MountedBike != null && MotorbikeMount.MountedBike.CurrentRider == transform)
        {
            currentBike = MotorbikeMount.MountedBike;
        }
        if (currentBike != null)
        {
            return;
        }

        currentBike = FindAnyObjectByType<MotorbikeMount>(FindObjectsInactive.Include);

        if (currentBike != null)
        {
            return;
        }

        // Include disabled instances and only repair the authored motorbike,
        // never turn an unrelated BicycleSystem example into the player's bike.
        foreach (MotorbikeCrashDetector detector in FindObjectsByType<MotorbikeCrashDetector>(FindObjectsInactive.Include))
        {
            if (detector.GetComponent<BicycleVehicle>() != null)
            {
                currentBike = detector.GetComponent<MotorbikeMount>();
                if (currentBike == null) currentBike = detector.gameObject.AddComponent<MotorbikeMount>();
                return;
            }
        }
    }

    private bool TryFindSafePlacement(
        Bounds localBikeBounds,
        Quaternion placementRotation,
        out Vector3 placementPosition)
    {
        Vector3 scale = Abs(currentBike != null ? currentBike.transform.lossyScale : motorbikePrefab.transform.localScale);
        Vector3 halfExtents = Vector3.Scale(localBikeBounds.extents, scale);
        halfExtents += Vector3.one * placementClearance;

        Vector3 localCenter = localBikeBounds.center;
        int safeCandidateCount = Mathf.Max(8, candidateCount);
        float minDistance = Mathf.Min(minimumSpawnDistance, summonSearchRadius);

        for (int i = 0; i < safeCandidateCount; i++)
        {
            float fraction = safeCandidateCount <= 1 ? 1f : i / (float)(safeCandidateCount - 1);
            float radius = Mathf.Lerp(minDistance, summonSearchRadius, fraction);
            float angle = i * 137.50776f;
            Vector3 horizontal = Quaternion.Euler(0f, angle, 0f) * (placementRotation * Vector3.forward) * radius;
            Vector3 rayOrigin = transform.position + horizontal + Vector3.up * groundProbeHeight;

            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                groundProbeHeight + groundProbeDepth,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            );

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                RaycastHit hit = hits[hitIndex];

                if (Mathf.Abs(hit.point.y - transform.position.y) > maximumGroundHeightDifference ||
                    Vector3.Angle(hit.normal, Vector3.up) > maximumGroundSlope ||
                    !IsGround(hit.collider))
                {
                    continue;
                }

                Vector3 checkCenter = hit.point + Vector3.up * (halfExtents.y + placementClearance);

                Collider[] overlaps = Physics.OverlapBox(
                    checkCenter,
                    halfExtents,
                    placementRotation,
                    obstacleLayerMask,
                    QueryTriggerInteraction.Ignore
                );

                if (HasBlockingOverlap(overlaps) || !HasGroundSupport(hit.point, halfExtents, placementRotation))
                {
                    continue;
                }

                Vector3 rotatedCenter = placementRotation * Vector3.Scale(
                    localCenter,
                    scale
                );
                placementPosition = checkCenter - rotatedCenter;
                Vector3 horizontalDistance = placementPosition - transform.position;
                horizontalDistance.y = 0f;
                if (horizontalDistance.magnitude > summonSearchRadius ||
                    horizontalDistance.magnitude < minimumSpawnDistance) continue;
                return true;
            }
        }

        placementPosition = default;
        return false;
    }

    private bool HasBlockingOverlap(Collider[] overlaps)
    {
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];

            if (overlap == null || IsCurrentBikeCollider(overlap))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsCurrentBikeCollider(Collider collider)
    {
        return currentBike != null &&
               collider != null &&
               collider.transform.IsChildOf(currentBike.transform);
    }

    private bool IsGround(Collider collider)
    {
        return collider != null && !IsCurrentBikeCollider(collider) &&
               collider.attachedRigidbody == null &&
               collider.GetComponentInParent<CitizenAI>() == null &&
               collider.GetComponentInParent<TrafficCarAI>() == null &&
               collider.GetComponentInParent<PoliceCarAI>() == null &&
               !collider.transform.IsChildOf(transform);
    }

    private bool HasGroundSupport(Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        // A single ray can hit a tiny ledge. Check all footprint corners too.
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = center + rotation * new Vector3(
                ((i & 1) == 0 ? -1f : 1f) * halfExtents.x * 0.8f,
                0f, ((i & 2) == 0 ? -1f : 1f) * halfExtents.z * 0.8f);
            if (!Physics.Raycast(corner + Vector3.up * 0.5f, Vector3.down,
                    out RaycastHit support, 1f, groundLayerMask, QueryTriggerInteraction.Ignore) ||
                !IsGround(support.collider) ||
                Vector3.Angle(support.normal, Vector3.up) > maximumGroundSlope)
                return false;
        }
        return true;
    }

    private void ShowNotification(bool success)
    {
        NpcQuestUIController.CreateIfMissing()?.ShowTimedInteractionPrompt(
            success ? "오토바이를 소환했습니다." : "오토바이를 소환할 수 없습니다.",
            notificationDuration
        );
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
