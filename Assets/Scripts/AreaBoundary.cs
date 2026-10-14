using UnityEngine;
using UnityEngine.InputSystem;

public class AreaBoundary : MonoBehaviour
{
    public Vector3 padding = new Vector3(25f, 20f, 25f);
    private BoxCollider boxCollider;

    [SerializeField] private SpawnManager spawnManager;
    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.size = padding;
        boxCollider.isTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) return;

        movement.Die();
    }
}
