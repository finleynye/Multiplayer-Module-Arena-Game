using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    public InputActionAsset actions;

    private InputAction moveAction;
    public Vector2 moveInput;

    private InputAction jumpAction;
    public bool isGrounded;
    public bool jumpReady;

    private InputAction dashAction;
    public float dashCooldownTime = 3f;
    public bool dashReady;

    void Awake()
    {
        moveAction = actions.FindAction("Move", true);
        jumpAction = actions.FindAction("Jump", true);
        dashAction = actions.FindAction("Dash", true);
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

    }

    void OnDisable()
    {
        actions.Disable();

        moveAction.canceled -= OnMove;
        jumpAction.performed -= OnJump;
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
        if(isGrounded && ctx.performed)
            dashReady = true;

        Debug.Log(dashReady);
    }
}
