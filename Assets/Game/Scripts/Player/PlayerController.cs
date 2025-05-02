using KinematicCharacterController;
using UnityEngine;

public class PlayerController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    public float MoveSpeed = 5f;
    public float RotationSpeed = 20f;
    public bool CanBePushed = false;
    public float Gravity = -30f;
    public float FallSpeedClamp = -50f;

    private Vector3 _internalVelocity;
    private bool _isForcedRotation;
    private Quaternion _forcedRotation;
    private float _rotationForceDuration;
    public Transform CameraTransform;

    private Vector3 _moveInput;

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;
    }

    public void ForceLookAt(Vector3 worldPosition, float duration = 0.5f)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            _isForcedRotation = true;
            _forcedRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
            _rotationForceDuration = duration;
        }
    }   

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_isForcedRotation)
        {
            currentRotation = Quaternion.Slerp(
                currentRotation,
                _forcedRotation,
                RotationSpeed * 2f * deltaTime
            );
            
            _rotationForceDuration -= deltaTime;
            if (_rotationForceDuration <= 0) _isForcedRotation = false;
            return;
        }

        if (_moveInput != Vector3.zero)
        {
            Vector3 camForward = CameraTransform.forward;
            Vector3 camRight = CameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * _moveInput.z + camRight * _moveInput.x);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0, -90, 0);
            
            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                RotationSpeed * deltaTime
            );
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        //get camera directions because player moves based on camera, not xz worldspace
        Vector3 camForward = CameraTransform.forward;
        Vector3 camRight = CameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        
        Vector3 moveDirection = (camForward * _moveInput.z + camRight * _moveInput.x).normalized;
        
        currentVelocity.x = moveDirection.x * MoveSpeed;
        currentVelocity.z = moveDirection.z * MoveSpeed;

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
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
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
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        _moveInput = new Vector3(horizontal, 0f, vertical).normalized;

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