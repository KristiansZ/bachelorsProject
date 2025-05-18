using KinematicCharacterController;
using UnityEngine;

public class PlayerController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    public float MoveSpeed = 4f;
    public float RotationSpeed = 20f;
    public bool CanBePushed = false;
    public float Gravity = -30f;
    public float FallSpeedClamp = -50f;

    private Vector3 internalVelocity;
    private bool isForcedRotation;
    private Quaternion forcedRotation;
    private float rotationForceDuration;
    public Transform CameraTransform;

    [HideInInspector]
    public Vector3 moveInput;
    
    private bool movementLocked = false;

    private PlayerAnimationManager animationManager;

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;
        
        animationManager = GetComponent<PlayerAnimationManager>();
        if (animationManager == null)
        {
            Debug.LogWarning("No PlayerAnimationManager found on player. Animations won't work.");
        }
    }

    public void ForceLookAt(Vector3 worldPosition, float duration = 0.5f)
    {
        Debug.Log("ForceLookAt called ");
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            isForcedRotation = true;
            forcedRotation = Quaternion.LookRotation(direction);
            rotationForceDuration = duration;
        }
    }   

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (isForcedRotation)
        {
            currentRotation = Quaternion.Slerp(
                currentRotation,
                forcedRotation,
                RotationSpeed * 2f * deltaTime
            );
            
            rotationForceDuration -= deltaTime;
            if (rotationForceDuration <= 0) isForcedRotation = false;
            return;
        }

        if (moveInput != Vector3.zero && !movementLocked)
        {
            Vector3 camForward = CameraTransform.forward;
            Vector3 camRight = CameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                RotationSpeed * deltaTime
            );
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (movementLocked || (animationManager != null && animationManager.IsAttacking()))
        {
            currentVelocity.x = 0;
            currentVelocity.z = 0;
            
            if (animationManager != null)
            {
                animationManager.UpdateMovementAnimation(false);
            }
        }
        else
        {
            //get camera directions because player moves based on camera, not xz worldspace
            Vector3 camForward = CameraTransform.forward;
            Vector3 camRight = CameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();
            
            Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;
            
            currentVelocity.x = moveDirection.x * MoveSpeed;
            currentVelocity.z = moveDirection.z * MoveSpeed;

            if (animationManager != null)
            {
                animationManager.UpdateMovementAnimation(moveInput != Vector3.zero);
            }
        }

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            currentVelocity.y = 0f;
        }
        else
        {
            currentVelocity.y += Gravity * deltaTime;

            //prevent infinite speed fall
            if (currentVelocity.y < FallSpeedClamp)
                currentVelocity.y = FallSpeedClamp;
        }
    }

    public void LookAt(Vector3 worldPosition)
    {
        Debug.Log("LookAt called ");
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    public void LockMovement(bool locked)
    {
        movementLocked = locked;
        
        if (locked)
        {
            moveInput = Vector3.zero;
        }
    }
    
    //kcc methods
    public void AfterCharacterUpdate(float deltaTime) { }
    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void HandleExternalForces(ref Vector3 currentVelocity, ref Vector3 currentExternalForce, float deltaTime)
    {
        if (!CanBePushed)
        {
            currentExternalForce = Vector3.zero;
        }
        else
        {
            //in case i want pushing
        }
    }

    private void Update()
    {
        //only accept input if movement isn't locked
        if (!movementLocked && (animationManager == null || !animationManager.IsAttacking()))
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;
        }
        else
        {
            moveInput = Vector3.zero;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) TomeController.Instance.SetActiveTome(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TomeController.Instance.SetActiveTome(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TomeController.Instance.SetActiveTome(2);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        if (UIConnector.Instance.pauseMenuPanel != null)
        {
            bool isActive = UIConnector.Instance.pauseMenuPanel.activeSelf;
            UIConnector.Instance.pauseMenuPanel.SetActive(!isActive);
            Time.timeScale = isActive ? 1f : 0f;
        }
    }
}