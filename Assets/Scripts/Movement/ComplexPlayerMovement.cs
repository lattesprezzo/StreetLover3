using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))] // Good practise. New Input System implementation
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(InputControl))] // Instead of StarterAssets
#endif
[RequireComponent(typeof(CharacterController))]

public class ComplexPlayerMovement : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    [Header("Testing")]
    public float targetSpeedInAnimator;
    public float maxSpeed;
    public float minSpeed;
    public bool isIdle;
    public bool hasMovedOnce = false;

    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    [SerializeField] float NormalWalkingSpeed = 2.0f;
    [SerializeField] float WalkingOnStairsSpeed = 0.55f;
    public float MoveSpeed;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    public Vector2 lookSensitivity;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false
#endif   
        }
    }

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private readonly float _terminalVelocity = 53.0f;

    // animation control
    public bool isAscending; // Walking up the stairs, a slope or climbing. Anything that is happening above the ground except jumping or falling.
    public bool isDescending;   // Walking down on any object above the ground.
    public bool isOnObjectAboveGround;

    // health
    public bool isAlive;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private Animator _animator;

    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;

    [SerializeField] float speed = 10.0f;
    private Vector2 movement;
    private Vector3 playervelocity;

    //..... Jumping .....//

    bool isOnAir; // Saattaa olla my�s OnWater joten tehd��n oma ilmassaoloehto
    bool isFalling;
    bool isJumping;
    bool isOnGround;
    bool isJumpPressed;
    [SerializeField] float initialJumpVelocity;
    [SerializeField] float maxJumpHeight;
    [SerializeField] float maxJumpTime;
    [SerializeField] float timeToReachTop;
    [SerializeField] float jumpForce;
    [SerializeField] float fallingForce;

    // -------- Gravity variables --------- //
    [SerializeField] float gravity;
    readonly float groundedGravity = -0.05f;

    Material playerSkin;

    private CharacterController _controller;
    private InputControl _input; // input Class generoidaan Inspectorissa


    void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _controller = GetComponent<CharacterController>();
        timeToReachTop = maxJumpTime / 2;
        gravity = (-4 * maxJumpTime) / timeToReachTop;
        initialJumpVelocity = (2 * maxJumpHeight) / timeToReachTop;
    }
    void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _input = GetComponent<InputControl>();

        isAlive = true;
        MoveSpeed = NormalWalkingSpeed;
        _animIDMotionSpeed = 1;
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
	Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;


        StartCoroutine(GroundChecker());

    }
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
       // _animIDJump = Animator.StringToHash("Jump");
       // _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);

        // EnvironmentScanner.cs controls isAscending
        if (!isOnObjectAboveGround) // Ray hits empty or doesn't hit the stairs - WHAT IF DESCENDING?
        {
            MoveSpeed = NormalWalkingSpeed;
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
        }
        else // Ray hits the stairs
        {
            MoveSpeed = WalkingOnStairsSpeed;
        }
        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraRotation()
    {
        Debug.Log("-------- input.look" + _input.look);
        // if there is an input and camera position is not fixed
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            // Mouse sensitivity
            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * lookSensitivity.x;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * lookSensitivity.y;
            Debug.Log("_cinemachineTargetYaw: " + _cinemachineTargetYaw);
            Debug.Log("Pitch: " + _cinemachineTargetPitch);
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    void MaterialColorControl(Color color)
    {
        Transform nose = transform.Find("Nose");
        if (nose != null)
        {
            if (nose.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = color;
            }
        }
    }

    private void GravityControl()
    {
        if (isOnGround)
        {
            isFalling = false;
            if (playervelocity.y >= 0) playervelocity.y += groundedGravity;
            MaterialColorControl(Color.green);

        }
        else
        {
            playervelocity.y += gravity * Time.deltaTime;
            MaterialColorControl(Color.blue);

        }
    }
    // ------------- Events register -------------//

    private void Move() {

        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }

    }
    /*
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            movement = ctx.ReadValue<Vector2>();
        }
        if (ctx.performed)
        {

            Debug.Log("Moving " + movement);
        }
        if (ctx.canceled)
        {
            movement = Vector2.zero;
        }
    } */
  /*  public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isJumpPressed = ctx.ReadValueAsButton(); // Voisi olla my�s vain isJumpPressed = true;
        }
        if (ctx.canceled)
        {
            isJumpPressed = ctx.ReadValueAsButton(); // Voisi olla my�s vain isJumpPressed = false;
        }
    }*/

    // ------------ Move ----------- //

    //void Move()
    //{
    //    Vector3 move = new(movement.x, playervelocity.y, movement.y);
    //    _controller.Move(speed * Time.deltaTime * move);
    //}

    // ------------ JUMP ------------ //

   /* void JumpControl()
    {
        if (isJumpPressed)
        {
            isJumping = true;
            playervelocity.y = initialJumpVelocity;

            // playervelocity.y += jumpForce * Time.deltaTime; ;
            // Debug.Log("Jumping pressed. Playervelocity.y = " + playervelocity.y);
        }
        else if (!isJumpPressed || !isOnGround)
        {
            isJumping = false;
            //playervelocity.y -= fallingForce;
        }
    }*/
    IEnumerator GroundChecker()
    {
        while (true)
        {
            isOnGround = _controller.isGrounded;
            Debug.Log("Coroutine says: isOnGround ===== " + isOnGround);
            yield return new WaitForSeconds(0.1f); // Ei yrit� checkaa joka framella 
        }
    }
    void Update()
    {
        Move();
        GravityControl();
       
       // JumpControl();
  
    }

    private void LateUpdate()
    {
        CameraRotation();
    }
}
