using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 10.0f;
    private Vector2 movement;
    private Vector3 playervelocity;

    //..... Jumping .....//

    bool isOnAir; // Saattaa olla myös OnWater joten tehdään oma ilmassaoloehto
    bool isOnGround;
    bool isJumpPressed;
    float initialJumpVelocity;
    [SerializeField] float maxJumpHeight = 1.0f;
    float maxJumpTime = 0.5f;
    bool isJumping;
    float timeToApex;


    // Gravity variables
    float gravity = -0.98f;
    readonly float groundedGravity = -0.05f;

    Material playerSkin;

    private CharacterController controller;
    private InputAction input; // input Class generoidaan Inspectorissa

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        isOnGround = controller.isGrounded;
        timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpTime)/Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
        

        // Assuming you have a "Move" action mapped to your desired keys in the Input Actions asset
        //moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        // input = new InputAction();

        //input.performed += ctx => movement = ctx.ReadValue<Vector2>();

        //moveAction.canceled += ctx => movement = Vector2.zero;

        //    moveAction.performed += ctx =>
        //    {
        //        movement = ctx.ReadValue<Vector2>();
        //        Debug.Log("Move action performed: " + movement);
        //    };
        //    moveAction.canceled += ctx =>
        //    {
        //        movement = Vector2.zero;
        //        Debug.Log("Move action canceled");
        //    };
        //}

        //void OnEnable()
        //{
        //    moveAction.Enable();
        //}

        //void OnDisable()
        //{
        //    moveAction.Disable();
    }
    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = Color.black;
        isOnGround = controller.isGrounded;

    }

    private void GravityControl()
    {
        if (controller.isGrounded)
        {
            playervelocity.y = groundedGravity;
        }
        else
        {
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = Color.red;
            playervelocity.y += gravity * Time.deltaTime;
        }

    }

    private void GroundChecker()
    {
        if (isOnGround)
        {
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = Color.red;
        }
        else
        {
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = Color.green;
        }
        return;

    }

    public void OnMove(InputAction.CallbackContext ctx)
    {


        movement = ctx.ReadValue<Vector2>();
        Debug.Log("Is Grounded");


    }


    // ------------ JUMP ------------ //

    public void OnJump(InputAction.CallbackContext ctx)
    {
        isJumpPressed = ctx.ReadValueAsButton();

        if (isJumpPressed && playervelocity.y < maxJumpHeight)
        {
            playervelocity.y += Mathf.Pow(2, 0.8f);
            Debug.Log("Jumping pressed");
        }

        else if (!isJumpPressed && !isOnGround)
        {
            playervelocity.y -= 2.5f;

            if (playervelocity.y < 0)
            {
                playervelocity.y = 0;

            }

            Debug.Log("Falling");
        }

        Debug.Log("Jumping velocity" + playervelocity.y);

    }
    void Update()
    {
        Vector3 move = new(movement.x, playervelocity.y, movement.y);
        controller.Move(speed * Time.deltaTime * move);
        //GroundChecker();
        GravityControl();   
    }
}
