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
    #endregion

    #region Components
    [SerializeField]
    private CharacterController characterController;
    
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private PlayerCombat playerCombat;

    [SerializeField]
    private Camera mainCamera;
    #endregion

    #region State
    private Vector3 moveDirection;
    private float currentSpeed;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        playerCombat = GetComponent<PlayerCombat>();
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        if (playerCombat != null && !playerCombat.IsDead)
        {
            HandleMovement();
            HandleRotation();
        }      
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        // Get input direction from PlayerInput (New Input System)
        Vector3 inputDirection = playerInput.GetMovementDirection(mainCamera.transform);
        
        // Check if player is attacking and should stop
        bool isAttacking = playerCombat != null && playerCombat.IsAttacking;
        
        if (inputDirection.magnitude > 0.1f && !isAttacking)
        {
            // Calculate target speed based on input
            currentSpeed = walkSpeed;
            
            // Set move direction
            moveDirection = inputDirection.normalized;
        }
        else
        {
            // Stop immediately when no input or attacking
            currentSpeed = 0f;
            moveDirection = Vector3.zero;
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

}
