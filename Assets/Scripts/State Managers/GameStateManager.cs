using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Lobby,
    GameStarted,
    GameOver,
    Reset
}

public class GameStateManager : MonoBehaviour
{
    //other scripts can read the state but only this script can change it
    public GameState CurrentGameState { get; private set; } = GameState.Lobby;

    //let other scripts respond when the game state changes
    public event Action<GameState> StateChanged;

    //players add themselves when enabled and remove themselves when disabled
    [SerializeField] private List<PlayerState> players = new List<PlayerState>();
    //how many players need to be ready before the game starts
    [SerializeField, Min(1)] private int requiredReadies = 2;

    void OnEnable()
    {
        //listen for each player's ready status changing
        foreach (PlayerState player in players)
        {
            if (player == null)
                continue;

            ListenToPlayer(player);
        }

        //check players who might already be ready
        CheckReadies();
    }

    void OnDisable()
    {
        //stop listening when the manager is disabled
        foreach (PlayerState player in players)
        {
            if (player != null)
                player.ReadyChanged -= CheckReadies;
        }
    }

    public void RegisterPlayer(PlayerState player)
    {
        if (player == null)
            return;

        //add each player once when they become active
        if (!players.Contains(player))
            players.Add(player);

        if (!isActiveAndEnabled)
            return;

        ListenToPlayer(player);
        CheckReadies();
    }

    public void UnregisterPlayer(PlayerState player)
    {
        if (player == null)
            return;

        //stop counting players who are disabled or leave the game
        player.ReadyChanged -= CheckReadies;
        players.Remove(player);
    }

    void ListenToPlayer(PlayerState player)
    {
        //remove first so registering or re enabling doesn't add the listener twice
        player.ReadyChanged -= CheckReadies;
        player.ReadyChanged += CheckReadies;
    }

    //the event passes a bool but we count everyone's current ready status
    void CheckReadies(bool ready = false)
    {
        //only check readiness while we're in the lobby
        if (!isActiveAndEnabled || CurrentGameState != GameState.Lobby)
            return;

        int readyCount = 0;

        //count players instead of adding one for every event
        foreach (PlayerState player in players)
        {
            if (player != null && player.IsReady)
                readyCount++;
        }

        if (readyCount >= requiredReadies)
            GameStarted();
    }

    //all state changes go through here so listeners are notified once
    private void ChangeState(GameState nextState)
    {
        //don't send the event again if we're already in this state
        if (CurrentGameState == nextState)
            return;

        CurrentGameState = nextState;
        Debug.Log($"Game state: {nextState}");
        StateChanged?.Invoke(nextState);
    }

    public void Lobby()
    {
        //Debug.Log("In Lobby");
        ChangeState(GameState.Lobby);
    }

    public void GameStarted()
    {
        //Debug.Log("Game Starterd");
        //only start the game from the lobby
        if (CurrentGameState == GameState.Lobby)
            ChangeState(GameState.GameStarted);
    }

    public void GameOver()
    {
        //Debug.Log("Game Over");
        //only end the game if we're currently playing
        if (CurrentGameState == GameState.GameStarted)
            ChangeState(GameState.GameOver);
    }

    public void ResetGame()
    {
        //Debug.Log("Game Resetting");
        //wait for the game to end before resetting
        if (CurrentGameState != GameState.GameOver)
            return;

        //let the players reset then return to the lobby
        ChangeState(GameState.Reset);
        Lobby();
    }
}
