using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{   
    [Header("Settings")]
    [SerializeField] [Range(0.1f, 10.0f)] private float _mouseSensitivity = 1.0f;

    [Header("Keybinds")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode _interactKey =  KeyCode.E;

    public Vector2 GetMouseLookInput()
    {
        float mouseXInput = Input.GetAxis("Mouse X") * _mouseSensitivity;
        float mouseYInput = Input.GetAxis("Mouse Y") * _mouseSensitivity;
        return new Vector2(mouseXInput, mouseYInput);
    }
    public Vector2 GetMovementInput()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        float zInput = Input.GetAxisRaw("Vertical");
        return new Vector2(xInput, zInput).normalized; // Normalizing ensures vector magnitude is constant when receiving multiple input directions
    }
    public bool GetJumpInput() => Input.GetKey(_jumpKey);
    public bool GetSprintInput() => Input.GetKey(_sprintKey);
    public bool GetCrouchInput() => Input.GetKey(_crouchKey);
    public bool GetInteractInput() => Input.GetKeyDown(_interactKey);
}