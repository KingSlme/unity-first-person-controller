using UnityEngine;

[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerInputManager))]
public class PlayerCameraController : MonoBehaviour
{   
    [SerializeField] private Camera _playerCamera;
    private FirstPersonController _firstPersonController;
    private PlayerInputManager _playerInputManager;
    private float _cameraXRotation = 0.0f;

    [Header("Field of View")] 
    [SerializeField] private bool _dynamicFOV = true;
    [SerializeField] [Range(30.0f, 110.0f)] private float _walkFOV = 60.0f;
    [SerializeField] [Range(30.0f, 110.0f)] private float _sprintFOV = 70.0f;
    [SerializeField] [Range(30.0f, 110.0f)] private float _crouchFOV = 50.0f;
    [SerializeField] [Range(0.1f, 10.0f)] private float _FOVTransitionRate = 5.0f;
    private float _epsilon = 0.01f;

    // [Header("View Bobbing")]



    private void Start()
    {
        _playerCamera.fieldOfView = _walkFOV;
    }

    private void Awake()
    {
        _firstPersonController = GetComponent<FirstPersonController>();
        _playerInputManager = GetComponent<PlayerInputManager>();
    }

    private void LateUpdate()
    {
        HandleMouseLook();
    } 

    private void Update()
    {
        if (_dynamicFOV)
            DynamicFOV();
    }

    private void HandleMouseLook()
    {
        Vector2 mouseLookInput = _playerInputManager.GetMouseLookInput();
        // Rotates Camera Up and Down
        _cameraXRotation -= mouseLookInput.y;
        _cameraXRotation = Mathf.Clamp(_cameraXRotation, -90.0f, 90.0f);
        _playerCamera.transform.localRotation = Quaternion.Euler(_cameraXRotation, 0.0f, 0.0f);
        // Rotates Player Left and Right
        transform.Rotate(Vector3.up * mouseLookInput.x);
    }

    private void DynamicFOV()
    {
        if (_firstPersonController.State == PlayerMovementState.walking)
            TransitionFOV(_walkFOV);
        else if (_firstPersonController.State == PlayerMovementState.sprinting)
            TransitionFOV(_sprintFOV);
        else if (_firstPersonController.State == PlayerMovementState.crouching)
            TransitionFOV(_crouchFOV);
    }

    private void TransitionFOV(float targetFOV)
    {
        _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, targetFOV, _FOVTransitionRate * Time.deltaTime);
        // Ensures targetFOV is reached
        if (Mathf.Abs(_playerCamera.fieldOfView - targetFOV) < _epsilon)
            _playerCamera.fieldOfView = targetFOV;
    }

    public Camera GetPlayerCamera()
    {
        return _playerCamera;
    }
}