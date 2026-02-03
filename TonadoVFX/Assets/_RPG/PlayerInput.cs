using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player Input Handler - Uses New Input System (PlayerInputActions)
/// Handles all player inputs and exposes events for other systems
/// </summary>
public class PlayerInput : MonoBehaviour
{
    #region Settings
    [Header("Input Settings")]
    [SerializeField] private bool enableMouseAndKeyboard = true;
    [SerializeField] private bool enableGamepad = true;
    
    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    #endregion

    #region Properties
    // Movement
    public Vector2 MoveInput { get; private set; }
    // Combat
    public bool IsAttacking { get; private set; }
    #endregion

    #region Events
    public event System.Action OnAttackPressed;
    public event System.Action OnAttackReleased;
    public event System.Action OnSkill1Pressed;
    public event System.Action OnSkill2Pressed;
    public event System.Action OnSkill3Pressed;
    public event System.Action OnSkill4Pressed;
    #endregion

    #region Components
    private PlayerInputActions inputActions;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    
    private void OnEnable()
    {
        inputActions.Enable();
        
        // Movement
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        
        // Combat
        inputActions.Player.Attack.performed += ctx => OnAttack();
        inputActions.Player.Attack.canceled += ctx => OnAttackRelease();
        inputActions.Player.Skill1.performed += ctx => OnSkill1Pressed?.Invoke();
        inputActions.Player.Skill2.performed += ctx => OnSkill2Pressed?.Invoke();
        inputActions.Player.Skill3.performed += ctx => OnSkill3Pressed?.Invoke();
    }
    
    private void OnDisable()
    {
        inputActions.Disable();
        
        // Unsubscribe events
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
    }
    #endregion

    #region Movement Handlers
    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }
    
    #endregion

    #region Combat Handlers
    private void OnAttack()
    {
        IsAttacking = true;
        OnAttackPressed?.Invoke();
    }
    
    private void OnAttackRelease()
    {
        IsAttacking = false;
        OnAttackReleased?.Invoke();
    }
    #endregion

    #region Helper Methods
    public bool HasMoveInput()
    {
        return MoveInput.sqrMagnitude > 0.01f;
    }
    
    public Vector3 GetMovementDirection(Transform cameraTransform)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0f;
        right.y = 0f;
        
        forward.Normalize();
        right.Normalize();
        
        return forward * MoveInput.y + right * MoveInput.x;
    }
    #endregion

    #region Input Control
    public void EnableInput()
    {
        inputActions.Enable();
    }
    
    public void DisableInput()
    {
        inputActions.Disable();
        MoveInput = Vector2.zero;
        IsAttacking = false;
    }
    #endregion
}
