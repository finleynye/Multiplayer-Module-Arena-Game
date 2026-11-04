using UnityEngine;

public class AreaBoundary : MonoBehaviour
{
    public Vector3 padding = new Vector3(25f, 20f, 25f);
    private BoxCollider boxCollider;

    [SerializeField] private GameStateManager gameStateManager;
    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        //how big the area you can play in without being killed 
        boxCollider.size = padding;
        boxCollider.isTrigger = true;

        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();
    }

    private void FixedUpdate()
    {
        if (gameStateManager == null) return;
        
        //check the player root so ragdoll exits and missed trigger events dont decide deaths
        foreach (PlayerState player in gameStateManager.players)
        {
            if (player == null) continue;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();

            if (movement == null) continue;

            Vector3 position = transform.InverseTransformPoint(movement.rb.position) - boxCollider.center;
            Vector3 halfSize = boxCollider.size * 0.5f;

            if (Mathf.Abs(position.x) > halfSize.x || Mathf.Abs(position.y) > halfSize.y || Mathf.Abs(position.z) > halfSize.z)
            {
                if (!movement.IsOwner) continue;

                if (player.Status == PlayerStatus.Lobby)
                {
                    movement.rb.linearVelocity = Vector3.zero;

                    SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
                    if (spawnManager != null)
                        spawnManager.SpawnPlayer(movement);
                }
                else if (gameStateManager.GameHasStarted && player.Status == PlayerStatus.Alive)
                    movement.Die();
                
            }
        }
    }
}
