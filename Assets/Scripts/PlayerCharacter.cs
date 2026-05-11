using UnityEngine;
using KinematicCharacterController;

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool Crouch;
    public bool Interact;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    float height;
    Vector3 center;
    float radius;

    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform cameraTargetInitialPosition;

    [Space]
    [SerializeField] private float walkSpeed   = 20f;
    [SerializeField] private float jumpHight   = 8f;
    [SerializeField] private float gravity     = -9.81f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchYOffset = -0.5f;
    [SerializeField] private Transform crouchTarget;
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [SerializeField, Min(0f)] private float interactionRadius = 0.25f;

    private Quaternion requestedRotation;
    private Vector3    requestedMovement;
    private bool       requestedJump;
    private bool       requestedCrouch;
    private bool       requestedInteract;

    public Player Player { get; set; }

    private bool movementLocked;

    public void Initialize()
    {
        motor.CharacterController = this;
        height = motor.Capsule.height;
        center = motor.Capsule.center;
        radius = motor.Capsule.radius;
        crouchTarget.localPosition = cameraTarget.localPosition + new Vector3(0f, crouchYOffset, 0f);
        cameraTargetInitialPosition = cameraTarget;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    public void UpdateInput(CharacterInput input)
    {
        if (movementLocked) return;

        requestedRotation = input.Rotation;
        requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        requestedMovement = Vector3.ClampMagnitude(requestedMovement, 1f);
        requestedMovement = input.Rotation * requestedMovement;
        requestedJump     = requestedJump || input.Jump;
        requestedCrouch   = input.Crouch;
        requestedInteract = input.Interact;
    }

    public IInteractable PlayerRaycast(Ray ray, out bool hitSomething)
    {
        hitSomething = false;

        RaycastHit[] hits = Physics.SphereCastAll(ray, interactionRadius, 2f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            hitSomething = true;

            if (interactable is Computer computer)
            {
                computer.Player = Player;
            }

            return interactable;
        }

        return null;
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (movementLocked) return;

        var forward = Vector3.ProjectOnPlane(requestedRotation * Vector3.forward, motor.CharacterUp).normalized;
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (movementLocked)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        if (motor.GroundingStatus.IsStableOnGround)
        {
            var groundedMovement = motor.GetDirectionTangentToSurface(
                requestedMovement, motor.GroundingStatus.GroundNormal) * requestedMovement.magnitude;
            currentVelocity = groundedMovement * walkSpeed;
        }
        else
        {
            currentVelocity += Vector3.up * gravity * deltaTime;
            if(requestedMovement.sqrMagnitude > 0f)
            {
                var planarMovement = Vector3.ProjectOnPlane(requestedMovement, motor.CharacterUp).normalized * requestedMovement.magnitude;
                var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                var movementForce = planarMovement * airAcceleration * deltaTime;
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
            }
        }

        if (requestedJump)
        {
            requestedJump = false;
            if (motor.GroundingStatus.IsStableOnGround)
            {
                motor.ForceUnground(time: 0f);
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpHight);

                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
        }

        if (requestedCrouch)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                cameraTarget = crouchTarget;
                motor.SetCapsuleDimensions(radius, crouchHeight, center.y + crouchYOffset);
            }
        }
        else
        {
            cameraTarget = cameraTargetInitialPosition;
            motor.SetCapsuleDimensions(radius, height, center.y);
        }
    }

    public void AfterCharacterUpdate(float deltaTime)  { }
    public void BeforeCharacterUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public bool IsWalking()
    {
        if (movementLocked || motor == null)
            return false;

        if (!motor.GroundingStatus.IsStableOnGround)
            return false;

        return requestedMovement.sqrMagnitude > 0.001f;
    }

    public Transform GetCameraTarget() => cameraTarget;
}