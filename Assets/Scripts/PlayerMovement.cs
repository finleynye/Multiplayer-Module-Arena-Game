using UnityEngine;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{

    [Header("Input")]
    public InputManager inputManager;

    [Header("Move Values")]
    public float movementSpeed = 5f;
    public Transform orientation;
    public float turnSpeed = 540f; //degrees per second

    [Header("Dash Inpulse Speed")]
    public float dashImpulseSpeed = 10f;
    private bool hasDashed;
    private float nextDashTime;

    [Header("Jump Force")]
    public float jumpForce = 5f;

    [Header("Ground Detection")]
    public bool isGrounded;
    public LayerMask ground;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public float groundCheckDelay = 0.2f;
    private float nextGroundCheckTime;

    [Header("Components")]
    public Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Grounded();
        RotatePlayer();
        Dash();

        if (hasDashed &&
            Time.time >= nextDashTime - inputManager.dashCooldownTime + 0.15f)
        {
            hasDashed = false;
        }

        //if we are not dashing move the player character using the input values and move speed
        if (!hasDashed)
        {
            rb.linearVelocity = new Vector3(
                inputManager.moveInput.x * movementSpeed, rb.linearVelocity.y, inputManager.moveInput.y * movementSpeed);
        }
        //if we're allowed to jump and we're grounded and the currrent y velocity is 0 then we can jump
        if (inputManager.jumpReady && inputManager.isGrounded && rb.linearVelocity.y <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            nextGroundCheckTime = Time.time + groundCheckDelay;
            inputManager.isGrounded = false;
        }
        

        inputManager.jumpReady = false;
    }

    //check within a radius around groundCheck transform and ground layer whether we can jump
    private void Grounded()
    {
        inputManager.isGrounded = Time.time >= nextGroundCheckTime && Physics.CheckSphere(groundCheck.position, groundCheckRadius, ground);

        Debug.Log("Player is on Ground:" + inputManager.isGrounded);
    }
    
    public void RotatePlayer()
    {
        Vector3 moveDirection = new Vector3(inputManager.moveInput.x,0f,inputManager.moveInput.y);

        if (moveDirection.sqrMagnitude > 0f)
            orientation.rotation = Quaternion.RotateTowards(orientation.rotation, Quaternion.LookRotation(moveDirection), turnSpeed * Time.fixedDeltaTime);
        
    }
    private void Dash()
    {
        if(inputManager.dashReady && Time.time >= nextDashTime)
        {
            rb.AddForce(orientation.forward * dashImpulseSpeed, ForceMode.Impulse);
            nextDashTime = Time.time + inputManager.dashCooldownTime;
            hasDashed = true;
        }
        inputManager.dashReady = false;
        
    }
}
