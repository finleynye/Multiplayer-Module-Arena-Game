using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;
public class PlayerMovement : NetworkBehaviour
{

    [Header("Input")]
    public InputManager inputManager;

    [Header("Move Values")]
    public float movementSpeed = 5f;
    public Transform orientation;
    public float turnSpeed = 540f; //degrees per second

    [Header("Dash Inpulse Speed")]
    public float dashImpulseSpeed = 10f;
    public bool hasDashed;
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

    [Header("Push Force")]
    [SerializeField] private float pushForce = 5f;
    public bool hasPushed;
    private float nextPushTime;
    //check push cooldown separately on the server
    private float nextServerPushTime;
    public LayerMask pushLayer;
    private float pushCheckRadius = 1.5f;

    //give knockback time to move the player before normal movement takes over
    [SerializeField] private float knockbackTime = 0.3f;
    private float knockbackEndTime;

    [Header("Components")]
    public Rigidbody rb;
    private NetworkAnimator networkAnimator;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //cache the network animator so dash and push are sent to other clients
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
    }

    void FixedUpdate()
    {
        //only move the player owned by this client after it has spawned
        if (!IsSpawned || !IsOwner)
            return;
        
        //consume ground check rotation of player and dash impulse on input
        Grounded();
        RotatePlayer();
        Dash();
        Push();


        if (hasDashed && Time.time >= nextDashTime - inputManager.dashCooldownTime + 0.15f)
            hasDashed = false;

        //dont overwrite horizontal velocity while dashing or being pushed
        if (!hasDashed && Time.time >= knockbackEndTime)
            rb.linearVelocity = new Vector3(inputManager.moveInput.x * movementSpeed, rb.linearVelocity.y, inputManager.moveInput.y * movementSpeed);
        

        //if were allowed to jump and were grounded and the currrent y velocity is 0 then we can jump
        if (inputManager.jumpReady && inputManager.isGrounded && rb.linearVelocity.y <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            nextGroundCheckTime = Time.time + groundCheckDelay;
            inputManager.isGrounded = false;
        }
        inputManager.jumpReady = false;

        //when not grounded apply extra downward force on the character
        if (!inputManager.isGrounded)
            rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
        
    }

    //check within a radius around groundCheck transform and ground layer whether we can jump
    private void Grounded()
    {
        inputManager.isGrounded = Time.time >= nextGroundCheckTime && Physics.CheckSphere(groundCheck.position, groundCheckRadius, ground);

        //Debug.Log("Player is on Ground:" + inputManager.isGrounded);
    }
    
    public void RotatePlayer()
    {
        //current move direction is the x and y input of WASD or GamePad Left Stick
        Vector3 moveDirection = new Vector3(inputManager.moveInput.x,0f,inputManager.moveInput.y);

        if (moveDirection.sqrMagnitude > 0f)
        {
            //rotate the player root so netcode syncs the direction
            Quaternion nextRotation = Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(moveDirection), turnSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(nextRotation);
        }
    }
    private void Dash()
    {
        //don't start a dash while were being pushed
        if(inputManager.dashReady && Time.time >= nextDashTime && Time.time >= knockbackEndTime)
        {
            //add force in the direction that the orientation is facing
            rb.AddForce(orientation.forward * dashImpulseSpeed, ForceMode.Impulse);
            nextDashTime = Time.time + inputManager.dashCooldownTime;
            hasDashed = true;

            //send the dash trigger through netcode so everyone sees it
            networkAnimator.SetTrigger("Dash");
        }
        inputManager.dashReady = false;
        
    }

    private void Push()
    {
        if (inputManager.pushReady && Time.time >= nextPushTime)
        {
            //ask the server to check the push
            RequestPushRpc();

            //play the animation straight away for responsive input
            networkAnimator.SetTrigger("Push");

            nextPushTime = Time.time + inputManager.pushCooldownTime;
        }

        inputManager.pushReady = false;
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestPushRpc()
    {
        //check the cooldown on the server as well
        if (Time.time < nextServerPushTime)
            return;

        nextServerPushTime = Time.time + inputManager.pushCooldownTime;

        //use the player's position and direction known by the server
        Vector3 pushDirection = orientation.forward;
        Vector3 checkPosition = orientation.position + pushDirection;

        Collider[] hits = Physics.OverlapSphere(checkPosition, pushCheckRadius, pushLayer);

        //a player with multiple colliders should only get pushed once
        HashSet<PlayerMovement> pushedPlayers = new HashSet<PlayerMovement>();

        foreach (Collider hit in hits)
        {
            PlayerMovement target = hit.GetComponentInParent<PlayerMovement>();

            //ignore ourselves and anything that isn't a spawned player
            if (target == null || target == this || !target.IsSpawned)
                continue;

            if (!pushedPlayers.Add(target))
                continue;

            //tell the targets owner to apply the approved force
            target.ApplyKnockbackRpc(pushDirection * pushForce);
        }
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ApplyKnockbackRpc(Vector3 force)
    {
        //leave horizontal velocity alone briefly so the push can move us
        knockbackEndTime = Time.time + knockbackTime;

        rb.AddForce(force, ForceMode.Impulse);
    }
}
