using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))] // Good practise. Ei tunnista OnMovea ja antaa errorin.
                                        // Jos ei tätä ole, erroria ei tule eikä tiedetä, miksi hahmo ei liiku.
#if ENABLE_INPUT_SYSTEM
[RequireComponent (typeof(InputControl))]
#endif
[RequireComponent(typeof(CharacterController))]

public class ComplexPlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 10.0f;
    private Vector2 movement;
    private Vector3 playervelocity;

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
    [SerializeField] float gravity;
    readonly float groundedGravity = -0.05f;

    Material playerSkin;

    private CharacterController _controller;
    private InputAction input; // input Class generoidaan Inspectorissa

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        timeToReachTop = maxJumpTime / 2;
        gravity = (-4 * maxJumpTime) / timeToReachTop;
        initialJumpVelocity = (2 * maxJumpHeight) / timeToReachTop;
    }
    void Start()
    {

        StartCoroutine(GroundChecker());

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
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isJumpPressed = ctx.ReadValueAsButton(); // Voisi olla myös vain isJumpPressed = true;
        }
        if (ctx.canceled)
        {
            isJumpPressed = ctx.ReadValueAsButton(); // Voisi olla myös vain isJumpPressed = false;
        }
    }

    // ------------ Move ----------- //

    void Move()
    {
        Vector3 move = new(movement.x, playervelocity.y, movement.y);
        _controller.Move(speed * Time.deltaTime * move);
    }

    // ------------ JUMP ------------ //

    void JumpControl()
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
    }
    IEnumerator GroundChecker()
    {
        while (true)
        {
            isOnGround = _controller.isGrounded;
            Debug.Log("Coroutine says: isOnGround ===== " + isOnGround);
            yield return new WaitForSeconds(0.1f); // Ei yritä checkaa joka framella 
        }
    }
    void Update()
    {
        Move();
        GravityControl();
        JumpControl();
    }
}
