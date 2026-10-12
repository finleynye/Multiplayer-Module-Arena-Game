using System;
using UnityEngine;
using UnityEngine.Events;
public enum PlayerStatus
{
    Lobby,
    Alive,
    Eliminated,
}
public class PlayerState : MonoBehaviour
{
    public UnityEvent isDead = new UnityEvent();
    [SerializeField] private GameStateManager gameStateManager;

    public PlayerStatus Status { get; private set; } = PlayerStatus.Lobby;
    public bool IsReady { get; private set; }

    public event Action<PlayerStatus> StateChanged;
    public event Action<bool> ReadyChanged;

    private void OnEnable()
    {
        if (gameStateManager == null)
        {
            Debug.LogWarning("Assign a GameStateManager to PlayerState in the Inspector", this); return;
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

    public void SetReady(bool ready)
    {
        if (gameStateManager == null || gameStateManager.CurrentGameState != GameState.Lobby)
            return;

        ChangeReady(ready);
    }

    private void ChangeReady(bool ready)
    {
        if (IsReady == ready)
            return;

        IsReady = ready;
        ReadyChanged?.Invoke(ready);
    }

    public void Eliminate()
    {
        if (Status != PlayerStatus.Alive)
            return;

        ChangeState(PlayerStatus.Eliminated);
        isDead?.Invoke();
    }

    private void ChangeState(PlayerStatus nextState)
    {
        if (Status == nextState)
            return;

        Status = nextState;
        StateChanged?.Invoke(nextState);
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
