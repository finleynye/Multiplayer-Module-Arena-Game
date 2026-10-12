using System;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    [Header("Action Asset")]
    //input action asset
    public InputActionAsset actions;

    [Header("Player Input Values")]
    //move
    private InputAction moveAction;
    public Vector2 moveInput;

    //jump
    private InputAction jumpAction;
    public bool isGrounded;
    public bool jumpReady;

    //dash
    private InputAction dashAction;
    public float dashCooldownTime = 1.2f;
    public bool dashReady;

    //push

    private InputAction pushAction;
    public float pushCooldownTime = 2f;
    public bool pushReady;

    void Awake()
    {
        moveAction = actions.FindAction("Move", true);
        jumpAction = actions.FindAction("Jump", true);
        dashAction = actions.FindAction("Dash", true);
        pushAction = actions.FindAction("Interact", true);
    }

    void OnEnable()
    {
        actions.Enable();

        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;

        jumpAction.performed += OnJump;
        jumpAction.canceled += OnJump;

        dashAction.performed += OnDash;
        dashAction.canceled += OnDash;

        pushAction.performed += OnPush;
        pushAction.canceled += OnPush;


    }

    void OnDisable()
    {
        actions.Disable();

        moveAction.canceled -= OnMove;
        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
        pushAction.performed -= OnPush;
    }
    
    void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = moveAction.ReadValue<Vector2>();
       //Debug.Log(moveInput);
    }
    void OnJump(InputAction.CallbackContext ctx)
    {
        if(isGrounded && ctx.performed)
            jumpReady = true;
        
        if(ctx.performed && !isGrounded)
            jumpReady = false;

       //Debug.Log("Dash is :" + jumpReady);
    }

    void OnDash(InputAction.CallbackContext ctx)
    {
        //optional lock for dash to be in air or not
        //if(isGrounded)

        if(ctx.performed)
            dashReady = true;

        Debug.Log(dashReady);
    }

    void OnPush(InputAction.CallbackContext ctx)
    {
        if(ctx.performed)
            pushReady = true;
    }
}
