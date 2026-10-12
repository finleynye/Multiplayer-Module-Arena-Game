using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public Transform endPos;
    public bool isPressed = false;

    private Vector3 originalPosition;
    [SerializeField] private float moveSpeed = 2f;

    void Start()
    {
        //save the position the plate returns to
        originalPosition = transform.position;
    }

    void Update()
    {
        //choose the target depending on whether the plate is pressed
        Vector3 targetPosition = isPressed ? endPos.position : originalPosition;

        //move towards the target each frame
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerState player = other.GetComponentInParent<PlayerState>();

        if (player == null)
            return;

        //mark this player ready when they step on
        isPressed = true;
        player.SetReady(true);
    }

    void OnTriggerExit(Collider other)
    {
        PlayerState player = other.GetComponentInParent<PlayerState>();

        if (player == null)
            return;

        //remove their ready status when they step off
        isPressed = false;
        player.SetReady(false);
    }
}