using System;
using Cinemachine;
using razz;
using UnityEngine;

public interface IPLayer
{
    Transform Transform { get; }
    Transform CameraTarget { get; }
}

public class Player : MonoBehaviour, IPLayer
{
    [SerializeField] private Transform _cameraTarget;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _headLookAtCameraForward;
    
    public PlayerInputActions  _inputActions;
    
    private CinemachineVirtualCamera _virtualCamera;
    private PlayerOutlineInteraction _playerOutlineInteraction;
    private PlayerMovement _playerMovement;

    public Transform Transform => transform;
    public Transform CameraTarget => _cameraTarget;
    
    private void OnEnable()
    {
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }

    private void Awake()
    {
        Base();

        _virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        _inputActions = new PlayerInputActions();
        
        _playerOutlineInteraction = new PlayerOutlineInteraction(Camera.main);
        _playerMovement = new PlayerMovement(GetComponent<CharacterController>(), Camera.main.GetComponent<CinemachineBrain>(), _inputActions);
    }


    private void Update()
    {
        _playerOutlineInteraction.Tick();
        _playerMovement.Tick();

        _animator.SetFloat("Speed", _playerMovement.CurrentSpeed);
    }

    private void FixedUpdate()
    {
        _playerMovement.FixedTick();
    }

    private void Base()
    {
        _headLookAtCameraForward.SetParent(Camera.main.transform);
        _headLookAtCameraForward.position = Vector3.forward;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

public class PlayerMovement
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping
    }
    
    private CharacterController _characterController;
    private readonly Transform _playerTransform;

    private Vector3 _moveDirection = Vector3.zero;
    private float _verticalSpeed = 0.0f;

    private float _walkSpeed = 3.0f;
    private float _runSpeed = 6.0f;
    private float _jumpForce = 7.0f;  
    private float _gravity = 20.0f;   
    private float _fallMultiplier = 2.5f; 

    private PlayerState _currentState = PlayerState.Idle;
    private readonly PlayerInputActions _inputActions;
    private readonly CinemachineBrain _cinemachineBrain;
    private bool _isRunning;
    private bool _jumpPressed;
    private float _moveHorizontal;
    private float _moveVertical;

    public float CurrentSpeed { get; private set; }

    public PlayerMovement(CharacterController characterController, CinemachineBrain cinemachineBrain, PlayerInputActions inputActions)
    {
        _characterController = characterController;
        _playerTransform = _characterController.transform;
        _inputActions = inputActions;
        _cinemachineBrain = cinemachineBrain;
        CurrentSpeed = 0f;
    }

    public void Tick()
    {
        Vector2 movementInput = _inputActions.Player.Move.ReadValue<Vector2>();
        _isRunning = _inputActions.Player.Run.IsPressed();
        _jumpPressed = _inputActions.Player.Jump.triggered;

        _moveHorizontal = movementInput.x;
        _moveVertical = movementInput.y;
    }

    public void FixedTick()
    {
        if (_characterController == null || _cinemachineBrain == null) return;

        // Вычисляем движение
        Vector3 forwardMovement = _cinemachineBrain.transform.forward * _moveVertical;
        Vector3 rightMovement = _cinemachineBrain.transform.right * _moveHorizontal;

        forwardMovement.y = 0f;
        rightMovement.y = 0f;

        forwardMovement.Normalize();
        rightMovement.Normalize();

        Vector3 desiredMoveDirection = (forwardMovement + rightMovement).normalized;
        float currentSpeed = _isRunning ? _runSpeed : _walkSpeed;
        Vector3 horizontalMove = desiredMoveDirection * currentSpeed;

        // Поворот игрока в направлении камеры
        Vector3 cameraLookDirection = _cinemachineBrain.transform.forward;
        cameraLookDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(cameraLookDirection);
        _playerTransform.rotation = Quaternion.Slerp(_playerTransform.rotation, targetRotation, Time.deltaTime * 15f);

        // Обработка прыжков и гравитации
        if (_characterController.isGrounded)
        {
            _verticalSpeed = -_gravity * Time.fixedDeltaTime;

            if (_jumpPressed)
            {
                _verticalSpeed = _jumpForce;
                _currentState = PlayerState.Jumping;
            }
            else
            {
                _currentState = desiredMoveDirection != Vector3.zero ? (_isRunning ? PlayerState.Running : PlayerState.Walking) : PlayerState.Idle;
            }
        }
        else
        {
            if (_verticalSpeed < 0)
            {
                _verticalSpeed -= _gravity * (_fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            _verticalSpeed -= _gravity * Time.fixedDeltaTime;
        }


        _moveDirection = horizontalMove;
        _moveDirection.y = _verticalSpeed;
        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        // Обновление текущей скорости
        if (_currentState == PlayerState.Idle || _currentState == PlayerState.Jumping)
            CurrentSpeed = 0;
        else
            CurrentSpeed = currentSpeed;
    }

    public PlayerState GetCurrentState()
    {
        return _currentState;
    }
}


public class PlayerOutlineInteraction
{
    private float _interactionDistance = 3f;  
    private LayerMask _interactableLayer;     
    private Color _outlineColor = Color.white;  
    private float _outlineWidth = 8f;           

    private Camera _camera;
    private GameObject _highlightedObject;  
    private Outline _currentOutline;

    public PlayerOutlineInteraction(Camera camera)
    {
        _camera = camera;
        _interactableLayer = LayerMask.GetMask(Layers.Interactable);
    }

    public void Tick()
    {
        HandleHighlighting();
    }

    void HandleHighlighting()
    {
        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _interactionDistance, _interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (_highlightedObject != hitObject)
            {
                RemoveOutline(); 
                
                _highlightedObject = hitObject;
                AddOutline(_highlightedObject);
            }
        }
        else
        {
            RemoveOutline();
        }
    }

    void AddOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }
        
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = _outlineColor;
        outline.OutlineWidth = _outlineWidth;
        outline.enabled = true;
        
        _currentOutline = outline;
    }

    void RemoveOutline()
    {
        if (_currentOutline != null)
        {
            _currentOutline.enabled = false; 
            _currentOutline = null;
            _highlightedObject = null;
        }
    }
}
