using UnityEngine;
using System.Collections;

public enum PlayerMovementState
    {
        walking,
        sprinting,
        crouching,
        jumping,
        falling
    }

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerCameraController))]
[RequireComponent(typeof(PlayerInputManager))]
[RequireComponent(typeof(PlayerUIManager))]
[RequireComponent(typeof(PlayerInteractor))]
public class FirstPersonController : MonoBehaviour
{   
    [Header("Movement")]
    [SerializeField] [Range(1.0f, 25.0f)] private float _walkSpeed = 3.0f;
    [SerializeField] [Range(1.0f, 25.0f)] private float _sprintSpeed = 5.0f; 
    [SerializeField] [Range(1.0f, 25.0f)] private float _crouchSpeed = 1.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float _airSpeedMultiplier = 0.5f; // 1.0f for no speed loss in air
    [SerializeField] [Range(0.0f, 5.0f)] private float _airControlMultiplier = 3.0f; // 0.0f for no control in air
    public float GetWalkSpeed() { return _walkSpeed; }
    public float GetSprintSpeed() { return _sprintSpeed; }
    public float GetCrouchSpeed() { return _crouchSpeed; }
    private float _movementSpeed;
    private Vector3 _preservedMomentum;
    private Vector3 _airborneVelocity;
    private float _stepOffset;

    [Header("Stamina")]
    [SerializeField] [Range(1.0f, 10.0f)] private float _maxStamina = 4.0f;
    [SerializeField] [Range(1.0f, 5.0f)] private float _staminaDecreaseRate = 1.0f;
    [SerializeField] [Range(1.0f, 5.0f)] private float _staminaRegenerationRate = 2.0f;
    [SerializeField] [Range(1.0f, 5.0f)] private float _staminaRegenerationCooldown = 2.0f;
    private float _currentStamina;
    private bool _hasSufficientStamina;
    private bool _canRegenerateStamina;
    private Coroutine _staminaRegenerationCooldownCoroutine;
    
    [Header("Jumping")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private Transform _groundCheckObject;
    [SerializeField] [Range(0.1f, 1.0f)] private float _groundDistance = 0.4f;
    [SerializeField] [Range(0.0f, 10.0f)] private float _jumpHeight = 1.5f;
    [SerializeField] [Range(0.0f, 5.0f)] private float _jumpCooldown = 0.1f;
    [SerializeField] [Range(0.0f, 100.0f)] private float _gravity = 20.0f;
    private float _verticalVelocity;
    public  bool Grounded { get; private set; }
    private bool _canJump = true;

    [Header("Crouching")]
    [SerializeField] private Transform _ceilingCheckObject;
    [SerializeField] [Range(0.1f, 1.0f)] private float _ceilingDistance = 0.4f;
    [SerializeField] [Range(0.1f, 1.0f)] private float _crouchingYScale = 0.5f;
    private float _standingYScale;
    private bool _isCrouching;

    private CharacterController _characterController;
    private PlayerInputManager _playerInputManager;
    private PlayerUIManager _playerUIManager;
    public PlayerMovementState State { get; private set; }

    private void Awake()
    {   
        _characterController = GetComponent<CharacterController>();
        _playerInputManager = GetComponent<PlayerInputManager>();
        _playerUIManager = GetComponent<PlayerUIManager>();
    }   

    private void Start()
    {
        InitializePlayer();
    }

    private void Update()
    {   
        // State
        HandleState();

        // Player Actions
        HandleMovement();
        HandleJumping();
        HandleCrouching();

        // External Factors
        HandleGravity();
        HandleStamina();
    }

    private void InitializePlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _stepOffset = _characterController.stepOffset;
        SetStepOffset(_stepOffset);
        _currentStamina = _maxStamina;
        _playerUIManager.SetStaminaSliderMaxValue(_maxStamina);
        _playerUIManager.SetStaminaSliderValue(_maxStamina);
        _standingYScale = transform.localScale.y;
    }

    private void HandleState()
    {   
        // Each state has precedence over each succeeding state
        if (_isCrouching)
        {
            State = PlayerMovementState.crouching;
            _movementSpeed = _crouchSpeed;
        }
        else if ((Grounded || _canJump) && _hasSufficientStamina && _playerInputManager.GetSprintInput())
        {
            State = PlayerMovementState.sprinting;
            _movementSpeed = _sprintSpeed;
        }
        else if (Grounded)
        {
            State = PlayerMovementState.walking;
            _movementSpeed = _walkSpeed;
        }
        else if (!_canJump) 
        {
            State = PlayerMovementState.jumping;
        }
        else
        {
            State = PlayerMovementState.falling;
        }
    }

    private void HandleGravity()
    {
        // Δy = 1/2 g * t^2 (Vertical displacement due to gravity)
        _verticalVelocity -= _gravity * Time.deltaTime;
        Vector3 verticalMovementVector = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
        _characterController.Move(verticalMovementVector);
    }

    private void HandleMovement()
    {   
        Vector2 movementInput = _playerInputManager.GetMovementInput();
        // Normal Movement
        if (Grounded)
        {
            // Returns step offset to normal
            SetStepOffset(_stepOffset);
            Vector3 directionVector = (transform.right * movementInput.x) + (transform.forward * movementInput.y);
            Vector3 movementVector = directionVector * _movementSpeed * Time.deltaTime;
            _characterController.Move(movementVector);
            // Preserve momentum in preparation for any jump 
            _preservedMomentum = directionVector * _movementSpeed;
            _airborneVelocity = Vector3.zero;
        }
        // Airborne Movement
        else
        { 
            // Ensures player cannot get stuck on edges while in the air
            SetStepOffset(0.0f);
            // How fast the player can move in the air depends on their speed prior to jumping (Defaults to walkSpeed * _airSpeedMultiplier if player was not moving prior to jumping)
            float maxAirborneSpeed = _preservedMomentum == Vector3.zero ? _walkSpeed * _airSpeedMultiplier : (_preservedMomentum * _airSpeedMultiplier).magnitude;
            // _airborneVelocity represents the aggregate of the original momentum + all new air movement
            if (_airborneVelocity == Vector3.zero) // Initialize _airborneVelocity if not initialized
            {
                _airborneVelocity = _preservedMomentum;
            }
            Vector3 airControlVector = ((transform.right * movementInput.x) + (transform.forward * movementInput.y)) * maxAirborneSpeed * _airControlMultiplier * Time.deltaTime;
            // Aggregates _airborneVelocity and ensures it cannot exceed maxAirborneSpeed
            _airborneVelocity = (_airborneVelocity + airControlVector).normalized * maxAirborneSpeed;
            _characterController.Move(_airborneVelocity * Time.deltaTime);
        }
    }

    private void HandleJumping()
    {   
        Grounded = Physics.CheckSphere(_groundCheckObject.position, _groundDistance, _groundLayerMask);
        if (Grounded && _verticalVelocity < 0.0f)
        {
            _verticalVelocity = -2.0f; // Offset to prevent player floating before fully reaching ground
        }
        if (Grounded && _canJump && _playerInputManager.GetJumpInput() )
        {   
            StartCoroutine(StartJumpCooldown());
            // v = sqrt(2 * g * h) (Initial vertical velocity to reach certain maximum height)
            _verticalVelocity = Mathf.Sqrt(2.0f * _gravity * _jumpHeight);
        }
    }   

    private IEnumerator StartJumpCooldown()
    {
        _canJump = false;
        while (Grounded) // Waits until player has left the ground
            yield return null;
        while (!Grounded) // Waits until player has returned to the ground
            yield return null;
        yield return new WaitForSeconds(_jumpCooldown); // Begins jump cooldown
        _canJump = true;
    }

    private void HandleCrouching()
    {
        if (_playerInputManager.GetCrouchInput())
        {   
            _isCrouching = true;
            transform.localScale = new Vector3(transform.localScale.x, _crouchingYScale, transform.localScale.z);
        }
        else if (!Physics.CheckSphere(_ceilingCheckObject.position, _ceilingDistance, _groundLayerMask))
        {
            _isCrouching = false;
            transform.localScale = new Vector3(transform.localScale.x, _standingYScale, transform.localScale.z);
        }
    }

    private void HandleStamina()
    {   
        if (State == PlayerMovementState.sprinting)
        {   
            if (_staminaRegenerationCooldownCoroutine != null)
            {
                StopCoroutine(_staminaRegenerationCooldownCoroutine);
            }
            _staminaRegenerationCooldownCoroutine = StartCoroutine(StartStaminaRegenerationCooldown());
            _currentStamina = Mathf.Clamp(_currentStamina - _staminaDecreaseRate * Time.deltaTime, 0.0f, _maxStamina);
            _playerUIManager.ShowStaminaSlider();
        }
        else if (_canRegenerateStamina && _currentStamina < _maxStamina)
        {
            _currentStamina = Mathf.Clamp(_currentStamina + _staminaRegenerationRate * Time.deltaTime, 0.0f, _maxStamina);
        }
        else if (_currentStamina >= _maxStamina)
        {
            _playerUIManager.HideStaminaSlider();
        }

        _playerUIManager.SetStaminaSliderValue(_currentStamina);
        _hasSufficientStamina = _currentStamina > 0.0f;
    }

    private IEnumerator StartStaminaRegenerationCooldown()
    {
        _canRegenerateStamina = false;
        yield return new WaitForSeconds(_staminaRegenerationCooldown);
        _canRegenerateStamina = true;
        _staminaRegenerationCooldownCoroutine = null;
    }

    private void SetStepOffset(float value)
    {
        _characterController.stepOffset = value;
    }
}