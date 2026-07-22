using UnityEngine;


namespace Invector.vCharacterController
{

    public class vThirdPersonController : vThirdPersonAnimator
    {
        public LayerMask obstacleLayer;
        public float detectionDistance = 1.2f;

        private PlayerParkourController parkourController;

        public float maxObstacleCheckHeight = 2.5f;
        public float topCheckStep = 0.1f;
        public float maxObstacleLength = 5.0f;

        public bool isParkouring;

        [Header("Parkour Target")]
        public float parkourFrontOffset = 0.08f;
        public float parkourTopInset = 0.45f;
        public float parkourLandingOffset = 0.8f;
        public float parkourLandingCheckHeight = 3f;
        public float parkourDetectionHeight = 0.45f;
        public float parkourDetectionRadius = 0.15f;
        public float maxParkourHeight = 4.25f;
        public float topProbePadding = 0.5f;

        [Header("Vault Detection")]
        public float vaultProbeHeight = 0.75f;
        public float vaultSupportProbeHeight = 0.25f;

        [Header("Climb Detection")]
        public float climbHandProbeHeight = 1.7f;
        public float climbSupportProbeHeight = 0.6f;
        [Range(0f, 1f)] public float climbMaximumUpNormal = 0.3f;

        [Header("Slide Detection")]
        public float slideStandingProbeHeight = 1.7f;
        public float slideEntryDepth = 0.45f;
        public float slideClearancePadding = 0.05f;
        public float slideGroundProbeHeight = 1f;


        private bool previousUseGravity;
        private bool previousIsKinematic;
        private RigidbodyConstraints previousConstraints;

        private float previousColliderHeight;
        private Vector3 previousColliderCenter;
        private Collider currentParkourObstacle;

        [SerializeField] private float parkourColliderHeight = 1.2f;


        public virtual void ControlAnimatorRootMotion()
        {
            if (!enabled || animator == null || _rigidbody == null)
            {
                return;
            }

            if (isParkouring)
            {
                _rigidbody.angularVelocity = Vector3.zero;
                return;
            }



            if (useRootMotion)
            {
                MoveCharacter(moveDirection);
            }
        }

        public virtual void ControlLocomotionType()
        {
            if (lockMovement) return;

            if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
            {
                SetControllerMoveSpeed(freeSpeed);
                SetAnimatorMoveSpeed(freeSpeed);
            }
            else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
            {
                isStrafing = true;
                SetControllerMoveSpeed(strafeSpeed);
                SetAnimatorMoveSpeed(strafeSpeed);
            }

            if (!useRootMotion)
                MoveCharacter(moveDirection);
        }

        public virtual void ControlRotationType()
        {
            if (lockRotation)
            {
                return;
            }

            bool validInput = input.sqrMagnitude > 0.001f;

            if (validInput)
            {
                // calculate input smooth
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

                Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ? rotateTarget.forward : moveDirection;
                RotateToDirection(dir);
            }
        }

        public virtual void UpdateMoveDirection(Transform referenceTransform = null)
        {
            if (input.magnitude <= 0.01)
            {
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                return;
            }

            if (referenceTransform && !rotateByWorld)
            {
                //get the right-facing direction of the referenceTransform
                var right = referenceTransform.right;
                right.y = 0;
                //get the forward direction relative to referenceTransform Right
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                // determine the direction the player will face based on input and the referenceTransform's right and forward directions
                moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
            }
            else
            {
                moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
            }
        }

        public virtual void Sprint(bool value)
        {
            var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded &&
                !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

            if (value && sprintConditions)
            {
                if (input.sqrMagnitude > 0.1f)
                {
                    if (isGrounded && useContinuousSprint)
                    {
                        isSprinting = !isSprinting;
                    }
                    else if (!isSprinting)
                    {
                        isSprinting = true;
                    }
                }
                else if (!useContinuousSprint && isSprinting)
                {
                    isSprinting = false;
                }
            }
            else if (isSprinting)
            {
                isSprinting = false;
            }
        }

        public virtual void Strafe()
        {
            isStrafing = !isStrafing;
        }

        public virtual void Jump()
        {
            if (DetectParkourObstacle(false))
            {
                return;
            }

            // trigger jump behaviour
            jumpCounter = jumpTimer;
            isJumping = true;

            // trigger jump animations
            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        public virtual void Climb()
        {
            DetectParkourObstacle(true);
        }

        private bool DetectParkourObstacle(bool climbRequested)
        {
            parkourController ??= GetComponent<PlayerParkourController>();

            if (parkourController == null || isParkouring)
            {
                return false;
            }

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            if (climbRequested)
            {
                return TryStartClimb(forward);
            }

            if (TryStartSlide(forward))
            {
                return true;
            }

            if (TryStartVault(forward))
            {
                return true;
            }

            if (isSprinting)
            {
                isSprintJumping = true;
            }

            return false;
        }

        private bool TryStartClimb(Vector3 forward)
        {
            Vector3 handOrigin = transform.position + Vector3.up * climbHandProbeHeight;

            if (!Physics.SphereCast(handOrigin, parkourDetectionRadius, forward, out RaycastHit handHit, detectionDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (Mathf.Abs(Vector3.Dot(handHit.normal, Vector3.up)) > climbMaximumUpNormal)
            {
                return false;
            }

            Vector3 supportOrigin = transform.position + Vector3.up * climbSupportProbeHeight;

            if (!Physics.Raycast(supportOrigin, forward, out RaycastHit supportHit, detectionDistance, obstacleLayer, QueryTriggerInteraction.Ignore) || supportHit.collider != handHit.collider)
            {
                return false;
            }

            float obstacleHeight = handHit.collider.bounds.max.y - transform.position.y;
            ParkourAction action = parkourController.DivisionClimb(obstacleHeight);

            if (action == ParkourAction.None || obstacleHeight > maxParkourHeight)
            {
                return false;
            }

            if (!TryFindObstacleTopAndLength(handHit, forward, out Vector3 topPoint, out float obstacleLength))
            {
                return false;
            }

            ParkourTargetData targetData = BuildBasicParkourTarget(handHit, forward, topPoint, obstacleHeight, obstacleLength);
            parkourController.StartParkour(action, targetData);
            return true;
        }

        private bool TryStartVault(Vector3 forward)
        {
            Vector3 vaultOrigin = transform.position + Vector3.up * vaultProbeHeight;

            if (!Physics.SphereCast(vaultOrigin, parkourDetectionRadius, forward, out RaycastHit frontHit, detectionDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 supportOrigin = transform.position + Vector3.up * vaultSupportProbeHeight;

            if (!Physics.Raycast(supportOrigin, forward, out RaycastHit supportHit, detectionDistance, obstacleLayer, QueryTriggerInteraction.Ignore) || supportHit.collider != frontHit.collider)
            {
                return false;
            }

            float obstacleHeight = frontHit.collider.bounds.max.y - transform.position.y;

            if (!TryFindObstacleTopAndLength(frontHit, forward, out Vector3 topPoint, out float obstacleLength))
            {
                return false;
            }

            ParkourAction action = parkourController.DivisionVault(obstacleHeight, input.magnitude);

            if (action == ParkourAction.None)
            {
                return false;
            }

            ParkourTargetData targetData = BuildBasicParkourTarget(frontHit, forward, topPoint, obstacleHeight, obstacleLength);

            if (action == ParkourAction.VaultOver)
            {
                if (!TryFindLandingPoint(topPoint, forward, obstacleLength, out Vector3 landingPoint))
                {
                    return false;
                }

                targetData.landingPosition = landingPoint;
            }

            parkourController.StartParkour(action, targetData);
            return true;
        }

        private bool TryStartSlide(Vector3 forward)
        {
            Vector3 standingOrigin = transform.position + Vector3.up * slideStandingProbeHeight;

            if (!Physics.SphereCast(standingOrigin, parkourDetectionRadius, forward, out RaycastHit ceilingHit, detectionDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float ceilingBottom = ceilingHit.collider.bounds.min.y - transform.position.y;

            if (ceilingBottom <= parkourColliderHeight + slideClearancePadding || ceilingBottom >= _capsuleCollider.height - slideClearancePadding)
            {
                return false;
            }

            GetCapsulePoints(transform.position, parkourColliderHeight, parkourColliderHeight * 0.5f, out Vector3 capsuleBottom, out Vector3 capsuleTop);
            RaycastHit[] passageHits = Physics.CapsuleCastAll(capsuleBottom, capsuleTop, _capsuleCollider.radius * 0.95f, forward, detectionDistance + slideEntryDepth, obstacleLayer, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit passageHit in passageHits)
            {
                if (passageHit.collider != null && passageHit.collider != _capsuleCollider)
                {
                    return false;
                }
            }

            Vector3 frontPosition = ceilingHit.point - forward * (_capsuleCollider.radius + parkourFrontOffset);
            frontPosition.y = transform.position.y;
            Vector3 entryPosition = frontPosition + forward * (_capsuleCollider.radius + slideEntryDepth);

            if (!TryProjectToSlideGround(entryPosition, out entryPosition))
            {
                return false;
            }

            Vector3 wallForward = Vector3.ProjectOnPlane(-ceilingHit.normal, Vector3.up).normalized;

            if (wallForward.sqrMagnitude < 0.001f)
            {
                wallForward = forward;
            }

            ParkourTargetData targetData = new ParkourTargetData
            {
                obstacleCollider = ceilingHit.collider,
                frontPosition = frontPosition,
                landingPosition = entryPosition,
                slideEntryPosition = entryPosition,
                slideDirection = forward,
                facingRotation = Quaternion.LookRotation(wallForward)
            };

            parkourController.StartParkour(ParkourAction.VaultSlide, targetData);
            return true;
        }

        protected override void CheckGroundDistance()
        {
            if (_capsuleCollider == null)
            {
                return;
            }

            int walkableLayer = groundLayer.value | obstacleLayer.value;
            float radius = _capsuleCollider.radius * 0.9f;
            float distance = 10f;
            Ray centerRay = new Ray(transform.position + new Vector3(0f, colliderHeight * 0.5f, 0f), Vector3.down);

            if (Physics.Raycast(centerRay, out groundHit, colliderHeight * 0.5f + distance, walkableLayer, QueryTriggerInteraction.Ignore) && !groundHit.collider.isTrigger)
            {
                distance = transform.position.y - groundHit.point.y;
            }

            if (distance >= groundMinDistance)
            {
                Vector3 sphereOrigin = transform.position + Vector3.up * _capsuleCollider.radius;
                Ray sphereRay = new Ray(sphereOrigin, Vector3.down);

                if (Physics.SphereCast(sphereRay, radius, out groundHit, _capsuleCollider.radius + groundMaxDistance, walkableLayer, QueryTriggerInteraction.Ignore) && !groundHit.collider.isTrigger)
                {
                    Physics.Linecast(groundHit.point + Vector3.up * 0.1f, groundHit.point + Vector3.down * 0.15f, out groundHit, walkableLayer, QueryTriggerInteraction.Ignore);
                    float newDistance = transform.position.y - groundHit.point.y;

                    if (distance > newDistance)
                    {
                        distance = newDistance;
                    }
                }
            }

            groundDistance = (float)System.Math.Round(distance, 2);
        }

        public bool TryGetSlidePosition(Vector3 currentPosition, Vector3 direction, float distance, out Vector3 slidePosition)
        {
            Vector3 targetPosition = currentPosition + direction.normalized * distance;
            return TryProjectToSlideGround(targetPosition, out slidePosition);
        }

        public bool HasSlideCeiling(Vector3 position, Collider ceilingCollider)
        {
            Vector3 rayOrigin = position + Vector3.up * (parkourColliderHeight + slideClearancePadding);
            float rayDistance = Mathf.Max(0.1f, previousColliderHeight - parkourColliderHeight);

            if (!Physics.Raycast(rayOrigin, Vector3.up, out RaycastHit ceilingHit, rayDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return ceilingCollider == null || ceilingHit.collider == ceilingCollider;
        }

        public bool CanStandAt(Vector3 position)
        {
            GetCapsulePoints(position, previousColliderHeight, previousColliderCenter.y, out Vector3 capsuleBottom, out Vector3 capsuleTop);
            Collider[] overlaps = Physics.OverlapCapsule(capsuleBottom, capsuleTop, previousColliderHeight > 0f ? _capsuleCollider.radius * 0.95f : 0.1f, obstacleLayer, QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap != null && overlap != _capsuleCollider)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryProjectToSlideGround(Vector3 position, out Vector3 groundedPosition)
        {
            int walkableLayer = groundLayer.value | obstacleLayer.value;
            Vector3 rayOrigin = position + Vector3.up * slideGroundProbeHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundPoint, slideGroundProbeHeight + groundMaxDistance, walkableLayer, QueryTriggerInteraction.Ignore))
            {
                groundedPosition = default;
                return false;
            }

            groundedPosition = position;
            groundedPosition.y = groundPoint.point.y + 0.03f;
            return true;
        }

        private void GetCapsulePoints(Vector3 position, float height, float centerY, out Vector3 capsuleBottom, out Vector3 capsuleTop)
        {
            float radius = _capsuleCollider.radius * 0.95f;
            float halfLine = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 center = position + Vector3.up * centerY;

            capsuleBottom = center - Vector3.up * halfLine;
            capsuleTop = center + Vector3.up * halfLine;
        }

        private bool TryFindObstacleTopAndLength(RaycastHit frontHit, Vector3 forward, out Vector3 topPoint, out float obstacleLength)
        {
            Collider obstacle = frontHit.collider;
            topPoint = default;
            obstacleLength = 0f;

            Vector3 topRayOrigin = new Vector3(frontHit.point.x, obstacle.bounds.max.y + topProbePadding, frontHit.point.z) + forward * Mathf.Max(0.05f, parkourDetectionRadius);

            if (!Physics.Raycast(topRayOrigin, Vector3.down, out RaycastHit topHit, obstacle.bounds.size.y + topProbePadding + 1f, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (topHit.collider != obstacle)
            {
                return false;
            }

            topPoint = topHit.point;
            bool foundFarEdge = false;

            for (float distance = topCheckStep; distance <= maxObstacleLength; distance += topCheckStep)
            {
                Vector3 checkOrigin = topPoint + forward * distance + Vector3.up * 0.5f;

                bool hasSurface = Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit surfaceHit, 1f, obstacleLayer, QueryTriggerInteraction.Ignore);

                if (!hasSurface || surfaceHit.collider != obstacle)
                {
                    obstacleLength = Mathf.Max(topCheckStep, distance - topCheckStep);

                    foundFarEdge = true;
                    break;
                }
            }

            if (!foundFarEdge)
            {
                obstacleLength = maxObstacleLength;
            }

            return true;
        }

        private ParkourTargetData BuildBasicParkourTarget(RaycastHit frontHit, Vector3 forward, Vector3 topPoint, float obstacleHeight, float obstacleLength)
        {
            Vector3 frontPosition = frontHit.point - forward * (_capsuleCollider.radius + parkourFrontOffset);

            frontPosition.y = transform.position.y;

            float maximumInset = Mathf.Max(0.1f, obstacleLength - _capsuleCollider.radius - 0.05f);

            float safeInset = Mathf.Clamp(parkourTopInset, 0.1f, maximumInset);
            Vector3 topPosition = topPoint + forward * safeInset;
            topPosition.y = topPoint.y + 0.03f;

            float exitInset = Mathf.Min(safeInset, obstacleLength * 0.5f);
            Vector3 topExitPosition = topPoint + forward * Mathf.Max(safeInset, obstacleLength - exitInset);
            topExitPosition.y = topPoint.y + 0.03f;

            Vector3 wallForward = Vector3.ProjectOnPlane(-frontHit.normal, Vector3.up).normalized;

            if (wallForward.sqrMagnitude < 0.001f)
            {
                wallForward = forward;
            }

            return new ParkourTargetData
            {
                obstacleCollider = frontHit.collider,
                frontPosition = frontPosition,
                topPosition = topPosition,
                topExitPosition = topExitPosition,
                landingPosition = topPosition,
                facingRotation = Quaternion.LookRotation(wallForward),
                obstacleHeight = obstacleHeight,
                obstacleLength = obstacleLength
            };
        }

        private bool TryFindLandingPoint(Vector3 topPoint, Vector3 forward, float obstacleLength, out Vector3 landingPosition)
        {
            landingPosition = default;

            Vector3 farEdgePoint = topPoint + forward * obstacleLength;

            Vector3 landingRayOrigin = farEdgePoint + forward * parkourLandingOffset + Vector3.up * parkourLandingCheckHeight;

            int landingMask = groundLayer.value | obstacleLayer.value;

            if (!Physics.Raycast(landingRayOrigin, Vector3.down, out RaycastHit landingHit, parkourLandingCheckHeight + maxObstacleCheckHeight, landingMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            landingPosition = landingHit.point;
            landingPosition.y += 0.03f;

            return true;
        }

        private bool TryMeasureObstacleLength(Collider obstacle, Vector3 frontHitPoint, out float obstacleLength)
        {
            Vector3 topRayOrigin = frontHitPoint + transform.forward * 0.1f + Vector3.up * maxObstacleCheckHeight;

            if (!Physics.Raycast(topRayOrigin, Vector3.down, out RaycastHit topHit, maxObstacleCheckHeight + 1.0f, obstacleLayer))
            {
                obstacleLength = 0f;
                return false;
            }

            if (topHit.collider != obstacle)
            {
                obstacleLength = 0f;
                return false;
            }

            Vector3 topStartPoint = topHit.point;

            for (float distance = topCheckStep; distance <= maxObstacleLength; distance += topCheckStep)
            {
                Vector3 checkOrigin = topStartPoint + transform.forward * distance + Vector3.up * 0.5f;

                bool hasSurface = Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit surfaceHit, 1.0f, obstacleLayer);

                if (!hasSurface || surfaceHit.collider != obstacle)
                {
                    obstacleLength = distance - topCheckStep;
                    return true;
                }
            }

            obstacleLength = maxObstacleLength;
            return true;
        }

        public Vector3 ParkourPosition => _rigidbody.position;
        public Quaternion ParkourRotation => _rigidbody.rotation;

        public void BeginParkour(Collider obstacleCollider)
        {
            if (isParkouring)
            {
                return;
            }

            isParkouring = true;
            lockMovement = true;
            lockRotation = true; 

            isSprinting = false;
            isJumping = false;
            isSprintJumping = false;
            isGrounded = true;
            groundDistance = 0f;
            verticalVelocity = 0f;

            input = Vector3.zero;
            inputSmooth = Vector3.zero;
            moveDirection = Vector3.zero;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            previousUseGravity = _rigidbody.useGravity;
            previousIsKinematic = _rigidbody.isKinematic;
            previousConstraints = _rigidbody.constraints;

            previousColliderHeight = _capsuleCollider.height;
            previousColliderCenter = _capsuleCollider.center;
            currentParkourObstacle = obstacleCollider;

            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            if (currentParkourObstacle != null)
            {
                Physics.IgnoreCollision(_capsuleCollider, currentParkourObstacle, true);
            }

            float newHeight = Mathf.Min(previousColliderHeight, parkourColliderHeight);

            _capsuleCollider.height = newHeight;
            _capsuleCollider.center = new Vector3(previousColliderCenter.x, newHeight * 0.5f, previousColliderCenter.z);
        }

        public void SetParkourPose(Vector3 position, Quaternion rotation)
        {
            if (!isParkouring)
            {
                return;
            }

            position = GetCollisionLimitedParkourPosition(position, rotation);

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        private Vector3 GetCollisionLimitedParkourPosition(Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 movement = targetPosition - _rigidbody.position;

            if (movement.sqrMagnitude <= 0.000001f)
            {
                return targetPosition;
            }

            Vector3 capsuleUp = targetRotation * Vector3.up;
            Vector3 capsuleCenter = _rigidbody.position + targetRotation * _capsuleCollider.center;
            float radius = _capsuleCollider.radius * 0.95f;
            float halfLine = Mathf.Max(0f, _capsuleCollider.height * 0.5f - radius);
            Vector3 capsuleBottom = capsuleCenter - capsuleUp * halfLine;
            Vector3 capsuleTop = capsuleCenter + capsuleUp * halfLine;
            float movementDistance = movement.magnitude;
            Vector3 movementDirection = movement / movementDistance;
            int collisionLayer = obstacleLayer.value;

            if (movement.y < -0.001f)
            {
                collisionLayer |= groundLayer.value;
            }

            RaycastHit[] hits = Physics.CapsuleCastAll(capsuleBottom, capsuleTop, radius, movementDirection, movementDistance, collisionLayer, QueryTriggerInteraction.Ignore);
            float allowedDistance = movementDistance;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider == currentParkourObstacle || hit.collider == _capsuleCollider || hit.distance <= 0.001f)
                {
                    continue;
                }

                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hit.distance - 0.02f));
            }

            return _rigidbody.position + movementDirection * allowedDistance;
        }

        public void EndParkour()
        {
            if (!isParkouring)
            {
                return;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            _capsuleCollider.height = previousColliderHeight;
            _capsuleCollider.center = previousColliderCenter;

            if (currentParkourObstacle != null)
            {
                Physics.IgnoreCollision(_capsuleCollider, currentParkourObstacle, false);
                currentParkourObstacle = null;
            }

            _rigidbody.constraints = previousConstraints | RigidbodyConstraints.FreezeRotation;
            _rigidbody.isKinematic = previousIsKinematic;
            _rigidbody.useGravity = previousUseGravity;

            isGrounded = true;
            groundDistance = 0f;
            verticalVelocity = 0f;
            heightReached = _rigidbody.position.y;

            input = Vector3.zero;
            inputSmooth = Vector3.zero;
            moveDirection = Vector3.zero;

            lockMovement = false;
            lockRotation = false;
            isParkouring = false;
        }

        private void StartClimbAction(int stateID)
        {
            // 파쿠르 변수 활성화 (HandleInvectorIntegration에 의해 인벡터가 자동 정지됨)
            //ClimbingUp = true;

            // 애니메이터 설정
            //Anim.SetInteger("ParkourState", stateID);
            //Anim.SetTrigger("DoParkour");

            //// 벽을 타는 동안 리지드바디 속도 초기화 (위로 튀는 현상 방지)
            //rb.velocity = Vector3.zero;
        }

    }

}
