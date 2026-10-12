using System;
using UnityEngine;
using UnityEngine.Events;
public enum GameState
{
    Lobby,
    GameStarted,
    GameOver,
    Reset
}
public class GameStateManager : MonoBehaviour
{
    public GameState currentGameState;
    [SerializeField] private bool allReady = false;
    void Awake()
    {
        currentGameState = GameState.Lobby;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentGameState)
        {
            case GameState.Lobby:
                Lobby();
                break;
            
            case GameState.GameStarted:
                GameStarted();
                break;

            case GameState.GameOver:
                GameOver();
                break;

            case GameState.Reset:
                ResetGame();
                break;
        }
    }

    public void Lobby()
    {
        if (allReady)
            currentGameState = GameState.GameStarted;
        
    }

    public void GameStarted()
    {
        Debug.Log("Game Started");
    }

    public void GameOver()
    {
        Debug.Log("Game Over");
    }

    public void ResetGame()
    {
        Debug.Log("Game Resetting");
    }
}
