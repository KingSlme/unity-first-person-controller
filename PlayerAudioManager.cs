using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{   
    [Header("Footsteps")]
    [SerializeField] private bool _enableFootsteps;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private Grass _grassFootsteps;
    [SerializeField] private Tile _tileFootsteps;
    private float _footstepFrequencyScale = 2.0f; 
    private float _footstepFrequency;
    private bool _canPlayFootstep = true;
    private PlayerMovementState _lastState;
    private Coroutine _playRandomFootstepCoroutine;

    [System.Serializable] private struct Grass
    {
        public AudioClip[] Walk;
        public AudioClip[] JumpStart;
        public AudioClip[] JumpLand;
    }

    [System.Serializable] private struct Tile
    {
        public AudioClip[] Walk;
        public AudioClip[] JumpStart;
        public AudioClip[] JumpLand;
    }






    // require this later
    private AudioSource _audioSource;
    private FirstPersonController _firstPersonController;
    private PlayerInputManager _playerInputManager;




    private void Awake()
    {   
        _audioSource = GetComponent<AudioSource>();
        _firstPersonController = GetComponent<FirstPersonController>();
        _playerInputManager = GetComponent<PlayerInputManager>();
    }

    private void Update()
    {
        if (_enableFootsteps) 
            HandleFootsteps();
    }

    private void HandleFootsteps()
    {   
        Debug.Log(_lastState + " " + _firstPersonController.State);
        if (_lastState != _firstPersonController.State)
            if (_playRandomFootstepCoroutine != null)
            {    
                StopCoroutine(_playRandomFootstepCoroutine);
                _playRandomFootstepCoroutine = null;
                _canPlayFootstep = true;
                // faster transition? if you stop moving OR change states, your coroutine + bool reset (almost done)
                // need to get rid of the double sfx when switching (some sort of delay before we can start it up again)
                // also need to ensure it's snappy
            }

        // are these returns acceptable for the jumping 2 parter?
        if (!_canPlayFootstep) return;
        if (!_firstPersonController.Grounded) return; 
        if (_playerInputManager.GetMovementInput().magnitude == 0) return;

        switch (_firstPersonController.State)
        {   
            case PlayerMovementState.walking:
                _lastState = PlayerMovementState.walking;
                _footstepFrequency = _footstepFrequencyScale / _firstPersonController.GetWalkSpeed();
                break;
            case PlayerMovementState.sprinting:
                _lastState = PlayerMovementState.sprinting;
                _footstepFrequency = _footstepFrequencyScale / _firstPersonController.GetSprintSpeed();
                break;
            case PlayerMovementState.crouching:
                _lastState = PlayerMovementState.crouching;
                _footstepFrequency = _footstepFrequencyScale / _firstPersonController.GetCrouchSpeed();
                break;
            case PlayerMovementState.jumping:
                // THIS SHOULD BE CHANGED BTW
                _lastState = PlayerMovementState.jumping;
                // _footstepFrequency = _firstPersonController.GetWalkSpeed() * _footstepFrequencyScale;
                break;
        }

        if (Physics.Raycast(Camera.main.transform.position, Vector3.down, out RaycastHit hit, 3, _groundLayerMask))
        {
            switch (hit.collider.tag)
            {
                case "Footsteps/Grass": 
                    _playRandomFootstepCoroutine = StartCoroutine(PlayRandomFootstep(GetRandomAudioClip(_grassFootsteps, "Walk")));
                    break;
                default:
                    break;
            }
        }
    }

    private IEnumerator PlayRandomFootstep(AudioClip audioClip)
    {   
        _canPlayFootstep = false;
        _audioSource.PlayOneShot(audioClip);
        yield return new WaitForSeconds(_footstepFrequency);
        _canPlayFootstep = true;
    }

    private AudioClip GetRandomAudioClip<T>(T groundType, string footstepType) where T: struct
    {
        var field = typeof(T).GetField(footstepType);
        if (field != null)
        {
            var value = field.GetValue(groundType);
            AudioClip[] audioClips = (AudioClip[])value;
            if (audioClips.Length == 0)
            {
                Debug.LogError($"No AudioClip in {typeof(T)} {footstepType} Field!");
                return null;
            }
            return audioClips[Random.Range(0, audioClips.Length)];
        }
        Debug.LogError($"Missing {typeof(T)} {footstepType} Field!");
        return null;
    }
}
