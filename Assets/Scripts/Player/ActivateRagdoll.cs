using UnityEngine;

public class ActivateRagdoll : MonoBehaviour
{
    Rigidbody[] rigidbodies;
    Collider[] ragdollColliders;
    CapsuleCollider col;
    Animator animator;
   
    void Start()
    {
        col = GetComponentInParent<PlayerMovement>().GetComponent<CapsuleCollider>();
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>();
        DisableRagdoll();
    }

    public void DisableRagdoll()
    {
        foreach (var ragdollCollider in ragdollColliders)
            ragdollCollider.enabled = false;

        foreach (var rigidBody in rigidbodies)
            rigidBody.isKinematic = true;
        
        col.enabled = true;
        animator.enabled = true;
    }
    public void EnableRagdoll()
    {
        col.enabled = false;
        animator.enabled = false;
        foreach (var ragdollCollider in ragdollColliders)
            ragdollCollider.enabled = true;

        foreach (var rigidBody in rigidbodies)
        {
            rigidBody.isKinematic = false;
        }
    }
    public void ApplyImpulse(Vector3 direction, float force)
    {
        foreach (var rb in rigidbodies)
            rb.AddForce(direction * force, ForceMode.Impulse);
        
    }
    public void ActivateTemporary(float duration)
    {
        EnableRagdoll();
        Invoke(nameof(DisableRagdoll), duration);
    }

}
