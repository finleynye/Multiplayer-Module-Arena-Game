using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
enum NumberOfRounds
{
    One,
    Two,
    Three,
    Four,
    Five
}

enum RoundPhase
{
    Waiting,
    OpeningCountdown,
    Playing,
    BetweenRounds,
    Finished
}
public class RoundManager : NetworkBehaviour
{
    //only the server chooses the current round
    private NetworkVariable<NumberOfRounds> numOfRounds = new(NumberOfRounds.One, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private GameStateManager gameStateManager;

    public int CurrentRound => (int)numOfRounds.Value;
    public PlayerState RoundWinner { get; private set; }

    //only the server decides when players wait or play
    private NetworkVariable<RoundPhase> phase = new(RoundPhase.Waiting);

    //everyone counts down towards the same server time
    private NetworkVariable<double> countdownEndsAt = new(0d);

    //share the winner's owner ID, max value means no winner
    private NetworkVariable<ulong> winnerClientId = new(ulong.MaxValue);

    public ulong WinnerClientId => winnerClientId.Value;
    public bool HasRoundWinner => winnerClientId.Value != ulong.MaxValue;

    public bool BetweenRounds => phase.Value == RoundPhase.OpeningCountdown || phase.Value == RoundPhase.BetweenRounds;

    public bool OpeningCountdown => phase.Value == RoundPhase.OpeningCountdown;

    public bool RoundIsActive =>
        phase.Value == RoundPhase.Playing &&
        gameStateManager != null &&
        gameStateManager.GameHasStarted;

    public bool ControlsLocked => BetweenRounds || phase.Value == RoundPhase.Finished;

    public float Countdown => !IsSpawned || !BetweenRounds ? 0f : Mathf.Max(0f,(float)(countdownEndsAt.Value - NetworkManager.ServerTime.Time));

    //list of players within a round
    private readonly List<PlayerState> trackedPlayers = new List<PlayerState>();

    public int playersInGame;
    public int alivePlayersLeft;

    void Awake()
    {
        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();
    }

    private void OnEnable()
    {
        if (gameStateManager == null) return;
        gameStateManager.StateChanged += HandleGameStateChanged;
        HandleGameStateChanged(gameStateManager.CurrentGameState);
    }

    public override void OnNetworkSpawn()
    {
        phase.OnValueChanged += HandlePhaseChanged;

        if (gameStateManager != null)
            HandleGameStateChanged(gameStateManager.CurrentGameState);

        //apply the phase already received when joining
        if (!IsServer)
            HandlePhaseChanged(phase.Value, phase.Value);
    }

    public override void OnNetworkDespawn()
    {
        phase.OnValueChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(RoundPhase previousPhase, RoundPhase nextPhase)
    {
        SyncPlayerSubscriptions();

        if (nextPhase == RoundPhase.Playing)
        {
            StartRound();
            return;
        }

        if (nextPhase != RoundPhase.OpeningCountdown &&
            nextPhase != RoundPhase.BetweenRounds)
            return;

        //each owner resets their own player while controls are locked
        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

        foreach (PlayerState player in trackedPlayers)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();

            if (movement == null || !movement.IsOwner)
                continue;

            movement.ResetForNextRound();

            if (spawnManager != null)
                spawnManager.SpawnPlayer(movement);
        }
    }

    //start the round before physics can report a player falling off
    private void HandleGameStateChanged(GameState state)
    {
        if (!IsSpawned || !IsServer)
            return;

        if (state == GameState.GameStarted)
        {
            if (phase.Value != RoundPhase.Waiting)
                return;

            numOfRounds.Value = NumberOfRounds.One;
            RoundWinner = null;
            BeginCountdown(RoundPhase.OpeningCountdown);
        }
        else
        {
            phase.Value = state == GameState.GameOver ? RoundPhase.Finished : RoundPhase.Waiting;
            RoundWinner = null;
        }
    }

    private void BeginCountdown(RoundPhase countdownPhase)
    {
        //set the deadline before telling everyone to prepare
        countdownEndsAt.Value = NetworkManager.ServerTime.Time + 5d;
        phase.Value = countdownPhase;
    }
    void Update()
    {
        SyncPlayerSubscriptions();

        //clients respond to shared phases instead of deciding transitions
        if (!IsSpawned || !IsServer ||
            gameStateManager == null || !gameStateManager.GameHasStarted)
            return;

        if (BetweenRounds)
        {
            if (Countdown <= 0f)
            {
                if (OpeningCountdown)
                    phase.Value = RoundPhase.Playing;
                else
                    NextRound();
            }

            return;
        }

        if (!RoundIsActive)
            return;

        if (RoundWinner != null || (playersInGame >= 2 && alivePlayersLeft <= 1))
        {
            if (RoundWinner == null && alivePlayersLeft == 1)
                RoundWinner = trackedPlayers.Find(player => player != null && player.Status == PlayerStatus.Alive);

            //save the result before starting the between-round countdown
            winnerClientId.Value = RoundWinner != null ? RoundWinner.GetComponent<PlayerMovement>().OwnerClientId : ulong.MaxValue;

            Debug.Log(RoundWinner != null ? $"{RoundWinner.name} wins" : "Round draw");
            BeginCountdown(RoundPhase.BetweenRounds);
        }
    }

    private void OnDisable()
    {
        //stop listening when the manager is disabled
        if (gameStateManager != null)
            gameStateManager.StateChanged -= HandleGameStateChanged;
            
        foreach (PlayerState player in trackedPlayers)
            if (player != null)
                player.StateChanged -= CountAlivePlayers;

        trackedPlayers.Clear();
    }

    private void SyncPlayerSubscriptions()
    {
        //remove players who left, counting backwards so removing is safe
        for (int i = trackedPlayers.Count - 1; i >= 0; i--)
        {
            PlayerState player = trackedPlayers[i];
            if (gameStateManager != null && player != null && gameStateManager.players.Contains(player))
                continue;

            if (player != null)
                player.StateChanged -= CountAlivePlayers;

            trackedPlayers.RemoveAt(i);
            CountAlivePlayers();
        }

        if (gameStateManager == null) return;

        //listen to each new player once and count their current state
        foreach (PlayerState player in gameStateManager.players)
        {
            if (player == null || trackedPlayers.Contains(player)) continue;
            trackedPlayers.Add(player);
            player.StateChanged += CountAlivePlayers;
            CountAlivePlayers();
        }
    }

    //the event passes a state but we count everyone still alive
    private void CountAlivePlayers(PlayerStatus status = PlayerStatus.Lobby)
    {
        playersInGame = trackedPlayers.Count;
        alivePlayersLeft = 0;

        foreach (PlayerState player in trackedPlayers)
            if (player != null && player.Status == PlayerStatus.Alive)
                alivePlayersLeft++;

        //only the server saves a winner during active play
        if (IsServer && RoundIsActive && RoundWinner == null && status == PlayerStatus.Eliminated && playersInGame >= 2 && alivePlayersLeft == 1)
            RoundWinner = trackedPlayers.Find(player => player != null && player.Status == PlayerStatus.Alive);
        
    }

    private void StartRound()
    {
        //clear the previous result when play resumes
        if (IsServer)
            winnerClientId.Value = ulong.MaxValue;

        //put eliminated players back in for the next round
        foreach (PlayerState player in trackedPlayers)
            player.StartRound();
            
        RoundWinner = null;
        CountAlivePlayers();
    }

    public void NextRound()
    {
        if (!IsSpawned || !IsServer ||
            gameStateManager == null || !gameStateManager.GameHasStarted ||
            phase.Value != RoundPhase.BetweenRounds ||
            Countdown > 0f)
            return;

        if (numOfRounds.Value == NumberOfRounds.Five)
        {
            gameStateManager.GameOver();
            return;
        }

        numOfRounds.Value++;
        phase.Value = RoundPhase.Playing;
    }
}
