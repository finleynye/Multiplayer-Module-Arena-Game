using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private InputManager inputManager;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 velocity = playerMovement.rb.linearVelocity;
        velocity.y = 0f;

        anim.SetFloat("movementSpeed", velocity.magnitude, 0.025f, Time.deltaTime);
        anim.SetBool("isGrounded", inputManager.isGrounded);
    }
}
