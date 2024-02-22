using System;
using System.Collections;
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

    private PlayerInput _playerInput; // Receives input from PlayerControls (Input Action Asset)
#endif

    [Header("Testing")]

    public float targetSpeedInAnimator;
    public float maxSpeed;
    public float minSpeed;
    public bool isIdle;
    public bool hasMovedOnce = false;

    [Header("-------- Input --------")]
    [Space(10)]
    [SerializeField]
    private InputControl _input; // input Classia ei generoida Send Message-tapauksessa
    [SerializeField]
    float _inputLookY;
    public Vector2 input_move;

    public bool jump;
    public bool sprint;


    [Header("-------- Player --------")]
    [Space(10)]
    private CharacterController _controller;
    private GameObject _mainCamera;
    // health
    public bool isAlive;
    private float _speed;
    private float _targetRotation = 0.0f;

    [SerializeField] float speed = 10.0f;
    private Vector3 playervelocity;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private readonly float _terminalVelocity = 53.0f;
    [Tooltip("Move speed of the character in m/s")]
    [SerializeField] float NormalWalkingSpeed;
    [SerializeField] float WalkingOnStairsSpeed = 0.55f;
    public float MoveSpeed;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Header("-------- Jump --------")]
    [Space(10)]
    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    //..... Jumping .....//

    bool isOnAir; // Saattaa olla myös OnWater joten tehdään oma ilmassaoloehto
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
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;
    readonly float groundedGravity = -0.05f;

    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;



    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("-------- Player Grounded --------")]
    [Space(10)]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset =0f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("-------- Cinemachine --------")]
    [Space(10)]

    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    public Vector2 lookSensitivity;

    [Tooltip("Looking sideways")]
    [SerializeField]
    private float _cinemachineTargetYaw;
    [Tooltip("Looking up and down")]
    [SerializeField]
    private float _cinemachineTargetPitch;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    [Header("-------- Animation control --------")]
    [Space(10)]

    [SerializeField]
    private Animator _animator;
    private bool _hasAnimator;
    private float _animationBlend;
    public bool isAscending; // Walking up the stairs, a slope or climbing. Anything that is happening above the ground except jumping or falling.
    public bool isDescending;   // Walking down on any object above the ground.
    public bool isOnObjectAboveGround;
    // animation IDs

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    // Events for the LayerWeightChanger
    public event Action OnStartSprint;
    public event Action OnStopSprint;


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

    private const float _threshold = 0.01f;

    public GameObject nose;
    Material playerSkin;

    void Awake()
    {


        // Reference to the main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _controller = GetComponent<CharacterController>();
        //timeToReachTop = maxJumpTime / 2;
        //gravity = (-4 * maxJumpTime) / timeToReachTop;
        //initialJumpVelocity = (2 * maxJumpHeight) / timeToReachTop;
    }
    void Start()
    {
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
	Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        _input = GetComponent<InputControl>();
        _controller = GetComponent<CharacterController>();

        isAlive = true;
        MoveSpeed = NormalWalkingSpeed;
        _animIDMotionSpeed = 1;
        _hasAnimator = TryGetComponent(out _animator);
        AssignAnimationIDs();
        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        //StartCoroutine(GroundChecker());
     

    }

    //private void OnMovePerformed(InputAction.CallbackContext context)
    //{
    //    // context.ReadValue<Vector2>() gives you the input value
    //    _input.move = context.ReadValue<Vector2>();
    //    // You can call your Move() function here

    //}

    //private void OnMoveCanceled(InputAction.CallbackContext context)
    //{
    //    // When the move action is canceled, you might want to stop the movement
    //    _input.move = Vector2.zero;
    //    // You can call your Move() function here

    //}
    //private void OnDestroy()
    //{
    //    // Don't forget to unsubscribe from the events when the object is destroyed
    //   // _input.move.performed -= OnMovePerformed;
    //    //_input.move.canceled -= OnMoveCanceled;
    //}
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }
    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);

        // EnvironmentScanner.cs controls isAscending
        //if (!isOnObjectAboveGround) // Ray hits empty or doesn't hit the stairs - WHAT IF DESCENDING?
        //{
        // MoveSpeed = NormalWalkingSpeed * Time.deltaTime;
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);
        // }
        // else // Ray hits the stairs
        {
            //  MoveSpeed = WalkingOnStairsSpeed;
        }
        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }
    private void CameraRotation()
    {
        //Debug.Log("-------- input.look" + _input.look);
        //Debug.Log("_cinemachineTargetYaw: " + _cinemachineTargetYaw);
        //Debug.Log("Pitch: " + _cinemachineTargetPitch);
        //Debug.Log("SQR.MAGNITUDE: " + _input.look.sqrMagnitude);

        // if there is the minimum input and camera position is not fixed
        //if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        //{
        // Mouse sensitivity
        _cinemachineTargetYaw += _input.look.x * lookSensitivity.x;
        _cinemachineTargetPitch += _input.look.y * lookSensitivity.y;
        //   }
        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
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
            if (jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of JH * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                Debug.Log("VerticalVelocity: " + _verticalVelocity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // Start counting jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;

            }
        } // Else if NOT grounded:
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;
            MaterialColorControl(Color.green);

            // Start counting fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
                MaterialColorControl(Color.red);
            }
            else
            {
                // update animator if using character and do FreeFall-animation
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            jump = false;
            MaterialColorControl(Color.grey);

        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    // Control the Camera Rotation angles
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
        //Transform nose = transform.Find("PlayerCameraTarget/CameraLookAtSphere/Nose");
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
            playervelocity.y += Gravity * Time.deltaTime;
            MaterialColorControl(Color.blue);
        }
    }
    // ------------- Events register -------------//

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            _input.move = ctx.ReadValue<Vector2>();
        }
        if (ctx.performed)
        {
            _input.move = ctx.ReadValue<Vector2>();
            Debug.Log("Moving " + _input.move);
        }
        if (ctx.canceled)
        {
            _input.move = Vector2.zero;
        }
    }
    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            _input.look = ctx.ReadValue<Vector2>();
        }
        if (ctx.performed)
        {
            //_input.look = ctx.ReadValue<Vector2>();
            // Debug.Log("Looking around " + _input.look);
        }

        if (ctx.canceled)
        {
            _input.look = Vector2.zero; // Jos tätä ei ole, niin kamera elää ja pyörii loputtomiin Clampin sallimissa rajoissa
        }
    }
    //public void OnJump(InputValue value)
    //{
    //    _input.jump = value.isPressed;
    //   Debug.Log(_input.jump);  
    //}
    //public void JumpInput(bool newJumpState)
    //{
    //    jump = newJumpState;
    //    Debug.Log(jump);
    //}

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {

            jump = ctx.ReadValueAsButton(); // Voisi olla myös vain isJumpPressed = true;
        }
        if (ctx.performed)
        {
        }
        if (ctx.canceled)
        {
            jump = ctx.ReadValueAsButton(); // Voisi olla myös vain isJumpPressed = false;
        }
    }

    // ------------ Move ----------- //

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = sprint ? SprintSpeed : MoveSpeed;

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
            _speed = targetSpeed; // If no need to acceleration or deceleration, keep this speed
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

    // ------------ Sprint ------------ //

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            sprint = ctx.ReadValueAsButton();
            OnStartSprint?.Invoke();
        }
        if (ctx.canceled)
        {
            Debug.Log(" Sprint canceled");
            sprint = ctx.ReadValueAsButton();
            OnStopSprint?.Invoke(); 
        }
    }



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
    //IEnumerator GroundChecker()
    //{
    //    while (true)
    //    {
    //        isOnGround = _controller.isGrounded;
    //        Debug.Log("Coroutine says: isOnGround ===== " + isOnGround);
    //        yield return new WaitForSeconds(0.1f); // Ei yritä checkaa joka framella 
    //    }
    //}
    void Update()
    {
        GroundedCheck();

        Move();
        JumpAndGravity();


        // JumpControl();

    }

    private void LateUpdate()
    {

        CameraRotation();

    }
}
