using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public enum GameState
{
    Lobby,
    GameStarted,
    GameOver,
    Reset
}

public class GameStateManager : NetworkBehaviour
{
    //everyone can read the game state but only the server can change it
    private NetworkVariable<GameState> gameState = new(GameState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameState CurrentGameState => gameState.Value;

    //read this from the current state so we dont need a separate bool
    public bool GameHasStarted =>  CurrentGameState == GameState.GameStarted;

    //let other scripts respond when the game state changes
    public event Action<GameState> StateChanged;

    //players add themselves when enabled and remove themselves when disabled
    [SerializeField] public List<PlayerState> players = new List<PlayerState>();

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

    //the event passes a bool but we count everyones current ready status
    void CheckReadies(bool ready = false)
    {
        //only the server checks whether enough players are ready
        if (!isActiveAndEnabled || CurrentGameState != GameState.Lobby || !IsSpawned || !IsServer)
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

    //only the server changes the state, the network callback notifies listeners
    private void ChangeState(GameState nextState)
    {
        if (!IsSpawned || !IsServer || CurrentGameState == nextState)
            return;

        gameState.Value = nextState;
    }

    public void Lobby()
    {
        //Debug.Log("In Lobby");
        ChangeState(GameState.Lobby);
    }

    public void GameStarted()
    {
        //need enough players before the game can start
        if (players.Count < requiredReadies)
            return;

        //Debug.Log("Game Starterd");
        //only start the game from the lobby
        if (CurrentGameState == GameState.Lobby)
        {
            ChangeState(GameState.GameStarted);
        }
    }

    public void GameOver()
    {
        //Debug.Log("Game Over");
        //only end the game if we're currently playing
        if (CurrentGameState == GameState.GameStarted)
        {
            ChangeState(GameState.GameOver);
        }
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

    public override void OnNetworkSpawn()
    {
        //listen for game state changes shared over the network
        gameState.OnValueChanged += HandleNetworkStateChanged;

        //apply the current state, including when joining late
        StateChanged?.Invoke(CurrentGameState);

        if (IsServer)
            CheckReadies();
    }

    public override void OnNetworkDespawn()
    {
        //stop listening when this network object despawns
        gameState.OnValueChanged -= HandleNetworkStateChanged;
    }

    //let local scripts respond when the shared game state changes
    private void HandleNetworkStateChanged(GameState previousState, GameState nextState)
    {
        Debug.Log($"Game state: {nextState}");
        StateChanged?.Invoke(nextState);
    }
}
