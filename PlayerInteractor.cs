using UnityEngine;

public interface IInteractable
{
    public void Interact();
}

[RequireComponent(typeof(PlayerCameraController))]
[RequireComponent(typeof(PlayerInputManager))]
[RequireComponent(typeof(PlayerUIManager))]
public class PlayerInteractor : MonoBehaviour
{   
    [SerializeField] [Range(1.0f, 10.0f)] private float _interactRange = 5.0f;
    private PlayerCameraController _cameraController;
    private PlayerInputManager _playerInputManager;
    private PlayerUIManager _playerUIManager;
    private Camera _playerCamera;

    private void Awake()
    {
        _cameraController = GetComponent<PlayerCameraController>();
        _playerInputManager = GetComponent<PlayerInputManager>();
        _playerUIManager = GetComponent<PlayerUIManager>();
    }

    private void Start()
    {
        _playerCamera = _cameraController.GetPlayerCamera();
    }

    private void Update()
    {
        DetectInteractables();
    }

    private void DetectInteractables()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, _interactRange))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactableObject))
            {   
                _playerUIManager.ShowInteractionUI();
                if (_playerInputManager.GetInteractInput())
                    interactableObject.Interact();
            }
            else
            {
                _playerUIManager.HideInteractionUI();
            }
        }
        else
        {
            _playerUIManager.HideInteractionUI();
        }
    }
}