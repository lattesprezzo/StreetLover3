using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 10.0f;
    private Vector2 movement;
    private Vector3 playervelocity;
    bool isJumpPressed = false; 
    private CharacterController controller;
    private InputAction moveAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Assuming you have a "Move" action mapped to your desired keys in the Input Actions asset
        //moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
     //   moveAction = new InputAction();

        //moveAction.performed += ctx => movement = ctx.ReadValue<Vector2>();
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
    public void OnMove(InputAction.CallbackContext ctx)
    {
        movement = ctx.ReadValue<Vector2>();
        Debug.Log("Move action performed: " + movement);
    }

    // ------------ JUMP ------------ //
   
    public void OnJump(InputAction.CallbackContext ctx)
    {

        isJumpPressed = ctx.ReadValueAsButton();
            //  controller.Move(movement);
           playervelocity.y += 0.1f;
            Debug.Log("Jumping");
        
    }
    void Update()
    {
        Vector3 move = new(movement.x, playervelocity.y, movement.y);
        controller.Move(speed * Time.deltaTime * move);
        //Debug.Log(move);
    }
}
