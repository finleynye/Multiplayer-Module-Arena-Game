using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
public enum PlayerStatus
{
    Lobby,
    Alive,
    Eliminated,
}
public class PlayerState : NetworkBehaviour
{
    public UnityEvent isDead = new UnityEvent();
    [SerializeField] private GameStateManager gameStateManager;

    //everyone reads player status but only the server changes it
    private NetworkVariable<PlayerStatus> playerStatus = new(PlayerStatus.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public PlayerStatus Status => playerStatus.Value;

    //everyone reads readiness but only the server changes it
    private NetworkVariable<bool> readyState = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsReady => readyState.Value;

    public event Action<PlayerStatus> StateChanged;
    public event Action<bool> ReadyChanged;

    private void OnEnable()
    {
        //Spawned player prefabs find the manager in the current scene.
        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();

        if (gameStateManager == null)
        {
            Debug.LogWarning("No GameStateManager found in the scene", this); return;
        }

        gameStateManager.StateChanged += HandleGameStateChanged;

        //events describe future changes; also apply the current state on enable.
        HandleGameStateChanged(gameStateManager.CurrentGameState);

        //add this player to the manager before they can ready up
        gameStateManager.RegisterPlayer(this);
    }

    private void OnDisable()
    {
        if (gameStateManager == null)
            return;

        //remove this player from the manager and clear their ready status
        gameStateManager.UnregisterPlayer(this);
        gameStateManager.StateChanged -= HandleGameStateChanged;
        ChangeReady(false);
    }

    public override void OnNetworkSpawn()
    {
        readyState.OnValueChanged += HandleReadyChanged;
        playerStatus.OnValueChanged += HandleStatusChanged;

        if (gameStateManager != null)
            HandleGameStateChanged(gameStateManager.CurrentGameState);

        //apply the status already received when joining
        HandleStatusChanged(Status, Status);

        //apply readiness already received when joining
        ReadyChanged?.Invoke(IsReady);
    }

    public override void OnNetworkDespawn()
    {
        readyState.OnValueChanged -= HandleReadyChanged;
        playerStatus.OnValueChanged -= HandleStatusChanged;
    }

    private void HandleReadyChanged(bool previousReady, bool nextReady)
    {
        //notify the manager when shared readiness changes
        ReadyChanged?.Invoke(nextReady);
    }

    public void SetReady(bool ready)
    {
        //only our own player sends readiness requests
        if (!IsSpawned || !IsOwner)
            return;

        RequestReadyRpc(ready);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestReadyRpc(bool ready)
    {
        //the server only accepts readiness changes in the lobby
        if (gameStateManager == null ||
            gameStateManager.CurrentGameState != GameState.Lobby)
            return;

        ChangeReady(ready);
    }

    private void ChangeReady(bool ready)
    {
        //internal resets also go through the server
        if (!IsSpawned || !IsServer || IsReady == ready)
            return;

        readyState.Value = ready;
    }

    public void StartRound()
    {
        if (gameStateManager == null || gameStateManager.CurrentGameState != GameState.GameStarted)
            return;

        ChangeReady(false);
        ChangeState(PlayerStatus.Alive);
    }

    public void Eliminate()
    {
        if (!IsSpawned || !IsServer || Status != PlayerStatus.Alive)
            return;

        RoundManager roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager == null || !roundManager.RoundIsActive || roundManager.RoundWinner != null)
            return;

        ChangeState(PlayerStatus.Eliminated);
    }

    private void ChangeState(PlayerStatus nextState)
    {
        if (!IsSpawned || !IsServer || Status == nextState)
            return;

        playerStatus.Value = nextState;
    }

    private void HandleStatusChanged(PlayerStatus previousStatus, PlayerStatus nextStatus)
    {
        StateChanged?.Invoke(nextStatus);

        if (nextStatus == PlayerStatus.Eliminated)
            isDead?.Invoke();
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Lobby:
            //lobby
            break;

            case GameState.Reset:
                //reset round
                ChangeReady(false);
                ChangeState(PlayerStatus.Lobby);
            break;

            case GameState.GameStarted:
                //only enter the round from the lobby, preserving elimination on re enable
                if (Status == PlayerStatus.Lobby)
                {
                    ChangeReady(false);
                    ChangeState(PlayerStatus.Alive);
                }
            break;
        }
    }
}
