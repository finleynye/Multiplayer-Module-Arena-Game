using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum MovementState
{
    Idle,
    Move,
    Jump,
    Dash,
    Push,

    Dead
}
public class PlayerMovement : NetworkBehaviour
{

    [Header("Input")]
    public InputManager inputManager;

    [Header("Move Values")]
    [SerializeField] private MovementState movementState;
    public float movementSpeed = 5f;
    public Transform orientation;
    public float turnSpeed = 540f; //degrees per second model turns

    [Header("Dash Inpulse Speed")]
    public float dashImpulseSpeed = 10f;
    [SerializeField] private float dashUpImpulse = 1.5f;
    public bool hasDashed;
    [SerializeField] private float dashDuration = 0.15f;
    private float dashEndTime;
    private float nextDashTime;
    private float serverDiveEndTime;
    private Vector3 serverDiveDirection;

    //hashsets store unique elements, rather than a list it wont allow duplicates of PlayerMovement for dive hits
    private readonly HashSet<PlayerMovement> diveHits = new HashSet<PlayerMovement>();
    private readonly HashSet<PlayerMovement> serverDiveHits = new HashSet<PlayerMovement>();

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
    [SerializeField] private float pushUpForce = 3f;
    public bool hasPushed;
    [SerializeField] private float pushDuration = 0.3f;
    private float pushEndTime;
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
    private RoundManager roundManager;
    private bool isDead;


    void Awake()
    {
        movementState = MovementState.Idle;
        rb = GetComponent<Rigidbody>();
        //cache the network animator so dash and push are sent to other clients
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
        roundManager = FindFirstObjectByType<RoundManager>();
    }

    void FixedUpdate()
    {
        //only move the player owned by this client after it has spawned
        if (!IsSpawned || !IsOwner || isDead)
            return;


        if (roundManager.BetweenRounds)
        {
            inputManager.jumpReady = false;
            inputManager.dashReady = false;
            inputManager.pushReady = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            movementState = MovementState.Idle;
            return;
        }
        
        //consume ground check rotation of player and dash impulse on input
        Grounded();
        RotatePlayer();
        Dash();
        Push();


        if (hasDashed && Time.time >= dashEndTime)
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


        //choose one state each tick, with actions taking priority
        if (hasDashed)
            movementState = MovementState.Dash;
        else if (Time.time < pushEndTime)
            movementState = MovementState.Push;
        else if (!inputManager.isGrounded)
            movementState = MovementState.Jump;
        else if (inputManager.moveInput.sqrMagnitude > 0f)
            movementState = MovementState.Move;
        else
            movementState = MovementState.Idle;
        
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
    //move our player straight away then tell the server were diving
    private void Dash()
    {
        //don't start a dash while were being pushed
        if(inputManager.dashReady && Time.time >= nextDashTime && Time.time >= knockbackEndTime)
        {
            //calculate the dash trajectory and normalize its
            Vector3 dashDirection = Vector3.ProjectOnPlane(orientation.forward, Vector3.up).normalized;

            //add force in the direction that the orientation is facing with a slight upward arc
            rb.AddForce(dashDirection * dashImpulseSpeed + Vector3.up * dashUpImpulse, ForceMode.Impulse);
            
            nextDashTime = Time.time + inputManager.dashCooldownTime;
            hasDashed = true;
            dashEndTime = Time.time + dashDuration;
            diveHits.Clear();
            StartDiveRpc();

            //send the dash trigger through netcode so everyone sees it
            networkAnimator.SetTrigger("Dash");
        }
        inputManager.dashReady = false;
        
    }

    //play our push animation straight away but let the server check who we hit
    private void Push()
    {
        if (inputManager.pushReady && Time.time >= nextPushTime)
        {
            //ask the server to check the push
            RequestPushRpc();

            //play the animation straight away for responsive input
            networkAnimator.SetTrigger("Push");

            pushEndTime = Time.time + pushDuration;

            nextPushTime = Time.time + inputManager.pushCooldownTime;
        }

        inputManager.pushReady = false;
    }

    //check who our player touches while diving then ask the server to knock them back
    private void OnCollisionStay(Collision collision)
    {
        if (!IsSpawned || !IsOwner || isDead || !hasDashed || Time.time >= dashEndTime) return;
        PlayerMovement target = collision.collider.GetComponentInParent<PlayerMovement>();

        //only hit each player once during the dive
        if (target != null && target != this && target.IsSpawned && diveHits.Add(target))
            RequestDiveHitRpc(target.NetworkObject);
    }

    //check the first contact too so quick hits still count
    private void OnCollisionEnter(Collision collision) => OnCollisionStay(collision);

    //runs on the server to track the dive, Dash moves the player on their own client
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void StartDiveRpc()
    {
        //remember the dive direction and hit window on the server
        serverDiveDirection = Vector3.ProjectOnPlane(orientation.forward, Vector3.up).normalized;
        serverDiveEndTime = Time.time + dashDuration;
        serverDiveHits.Clear();
    }

    //runs on the server to check the hit our collision method found
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestDiveHitRpc(NetworkObjectReference targetReference)
    {
        if (Time.time >= serverDiveEndTime || !targetReference.TryGet(out NetworkObject targetObject)) return;
        PlayerMovement target = targetObject.GetComponent<PlayerMovement>();

        if (target == null || target == this || !target.IsSpawned) return;

        //check they are close enough and use the same force as a push
        if (Vector3.Distance(rb.position, target.rb.position) > pushCheckRadius * 2f) return;

        if (serverDiveHits.Add(target))
            target.ApplyKnockbackRpc(serverDiveDirection * pushForce);
    }
    
    //runs on the server to find who gets pushed, Push only handles our input and animation
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

        //a player with multiple colliders should only get pushed once using a hashSet allows us to not have duplicates of pushedPlayers
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

    //the server sends this to the targets owner so their client applies the force
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ApplyKnockbackRpc(Vector3 force)
    {
        //leave horizontal velocity alone briefly so the push can move us
        knockbackEndTime = Time.time + knockbackTime;

        //stop falling first so the hit still lifts us into the air
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(0f, rb.linearVelocity.y), rb.linearVelocity.z);
        rb.AddForce(force + Vector3.up * pushUpForce, ForceMode.Impulse);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(!IsOwner) return;

        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

        if(spawnManager != null)
            spawnManager.SpawnPlayer(this);
    }

    //start our death and respawn locally, EliminateRpc tells everyone were eliminated
    public void Die()
    {
        if (isDead || (IsSpawned && !IsOwner)) return;
        //dont start waiting for a new round while were still in the lobby
        PlayerState playerState = GetComponent<PlayerState>();

        if (playerState == null || playerState.Status != PlayerStatus.Alive) return;

        RoundManager roundManager = FindFirstObjectByType<RoundManager>();
        //the last player has already won this round

        if (roundManager != null && roundManager.RoundWinner != null) return;

        StartCoroutine(RespawnCountdown());
    }

    //runs on everyone so they count the same elimination, Die only starts on our client
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    private void EliminateRpc()
    {
        GetComponent<PlayerState>()?.Eliminate();
    }

    //handle our ragdoll and respawn here, the rpc only shares our eliminated state
    private IEnumerator RespawnCountdown()
    {
        //player dead freeze velocity
        isDead = true;
        movementState = MovementState.Dead;
        RoundManager roundManager = FindFirstObjectByType<RoundManager>();

        int deathRound = roundManager != null ? roundManager.CurrentRound : -1;

        //only send an rpc while networking is running
        if (IsSpawned && NetworkManager != null && NetworkManager.IsListening)
            EliminateRpc();
        else
            GetComponent<PlayerState>()?.Eliminate();
        
        ActivateRagdoll ragdoll = GetComponentInChildren<ActivateRagdoll>();

        ragdoll.EnableRagdoll();
        yield break;
    }

    public void ResetForNextRound()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        GetComponentInChildren<ActivateRagdoll>().DisableRagdoll();

        isDead = false;
        hasDashed = false;
        pushEndTime = 0f;
        knockbackEndTime = 0f;

        inputManager.jumpReady = false;
        inputManager.dashReady = false;
        inputManager.pushReady = false;

        movementState = MovementState.Idle;
    }
}
