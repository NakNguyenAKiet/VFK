using UnityEngine;

/// <summary>
/// Complete Player Controller - Compatible with New Input System (PlayerInputActions)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    #region Settings
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    
    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -2f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    #endregion

    #region Components
    private CharacterController characterController;
    private Animator animator;
    private PlayerInput playerInput;
    private PlayerCombat playerCombat;
    private Camera mainCamera;
    #endregion

    #region State
    private Vector3 currentVelocity;
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isGrounded;
    #endregion

    #region Animation Parameters
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private readonly int isRunningHash = Animator.StringToHash("IsRunning");
    private readonly int groundedHash = Animator.StringToHash("Grounded");
    private readonly int velocityXHash = Animator.StringToHash("VelocityX");
    private readonly int velocityZHash = Animator.StringToHash("VelocityZ");
    #endregion

    #region Lifecycle
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerCombat = GetComponent<PlayerCombat>();
        mainCamera = Camera.main;
    }
    
    private void OnEnable()
    {
        // Input subscriptions handled elsewhere
    }
    
    private void OnDisable()
    {
        // Unsubscribe input events
    }
    
    private void Update()
    {
        if (playerCombat != null && !playerCombat.IsDead)
        {
            HandleMovement();
            HandleRotation();
        }
        
        ApplyGravity();
        UpdateAnimator();
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        // Không di chuyển khi đang attack (optional - comment out nếu muốn move while attacking)
        if (playerCombat != null && playerCombat.IsAttacking)
        {
            // Slow down when attacking
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
            moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, deceleration * Time.deltaTime);
        }
        else
        {
            // Get input direction from PlayerInput (New Input System)
            Vector3 inputDirection = playerInput.GetMovementDirection(mainCamera.transform);
            
            if (inputDirection.magnitude > 0.1f)
            {
                // Calculate target speed
                float targetSpeed = walkSpeed;
                
                // Smoothly accelerate
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
                
                // Update move direction
                moveDirection = Vector3.Lerp(moveDirection, inputDirection.normalized, acceleration * Time.deltaTime);
            }
            else
            {
                // Smoothly decelerate
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, deceleration * Time.deltaTime);
            }
        }
        
        // Apply movement
        Vector3 movement = moveDirection * currentSpeed;
        characterController.Move(movement * Time.deltaTime);
    }

    private void HandleRotation()
    {
        // Không rotate khi đang attack
        if (playerCombat != null && playerCombat.IsAttacking)
        {
            return;
        }
        
        Vector3 inputDirection = playerInput.GetMovementDirection(mainCamera.transform);
        
        if (inputDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
    #endregion

    #region Gravity
    private void ApplyGravity()
    {
        if (isGrounded)
        {
            currentVelocity.y = groundedGravity;
        }
        else
        {
            currentVelocity.y += gravity * Time.deltaTime;
        }
        
        characterController.Move(currentVelocity * Time.deltaTime);
    }
    #endregion

    #region Animation
    private void UpdateAnimator()
    {
        if (animator == null) return;
        
        // Speed parameter (0-1 normalized)
        float normalizedSpeed = currentSpeed / runSpeed;
        animator.SetFloat(speedHash, normalizedSpeed, 0.1f, Time.deltaTime);
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * (groundCheckDistance + 0.1f));
        
        // Movement direction
        if (moveDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, moveDirection * 2f);
        }
    }
    #endregion
}
