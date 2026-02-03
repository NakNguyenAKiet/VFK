using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool enableMouseAndKeyboard = true;
    [SerializeField] private bool enableGamepad = true;
    
    // Movement
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsDodging { get; private set; }
    
    // Combat
    public bool IsAttacking { get; private set; }
    
    // Events
    public event System.Action OnAttackPressed;
    public event System.Action OnAttackReleased;
    public event System.Action OnDodgePressed;
    public event System.Action OnInteractPressed;
    public event System.Action OnSkill1Pressed;
    public event System.Action OnSkill2Pressed;
    public event System.Action OnSkill3Pressed;
    public event System.Action OnSkill4Pressed;
    public event System.Action OnUltimatePressed;
    public event System.Action OnInventoryToggled;
    public event System.Action OnPauseToggled;
    
    // Input Actions (using new Input System)
    private PlayerInputActions inputActions;
    
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
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }
    
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
    
    private void OnDodge()
    {
        IsDodging = true;
        OnDodgePressed?.Invoke();
        
        // Reset dodge flag sau 0.5s
        Invoke(nameof(ResetDodge), 0.5f);
    }
    
    private void ResetDodge()
    {
        IsDodging = false;
    }
    
    // Helper methods
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
    
    public void EnableInput()
    {
        inputActions.Enable();
    }
    
    public void DisableInput()
    {
        inputActions.Disable();
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        IsRunning = false;
        IsAttacking = false;
    }
}