using UnityEngine;
using UnityEngine.Events;
public enum currentState
{
    Lobby,
    Alive,
    Eliminated,
}
public class PlayerState : MonoBehaviour
{
    public UnityEvent isDead;
    public currentState currentState;

    private GameStateManager gameStateManager;
    private bool isReady = false;



    void Awake()
    {
        gameStateManager = GameObject.Find("GameStateManager").GetComponent<GameStateManager>();
    }

    void Update()
    {
        if(gameStateManager.currentGameState == GameState.Lobby)
        {
            //in lobby
            isReady = true;
        }
    }
}
