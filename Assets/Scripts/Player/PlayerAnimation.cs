using Unity.Netcode;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private InputManager inputManager;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        //only the owner sets animation values so other clients keep the synced values
        if (!IsSpawned || !IsOwner)
            return;

        Vector3 velocity = playerMovement.rb.linearVelocity;
        velocity.y = 0f;

        anim.SetFloat("movementSpeed", velocity.magnitude, 0.025f, Time.deltaTime);
        anim.SetBool("isGrounded", inputManager.isGrounded);
    }
}
