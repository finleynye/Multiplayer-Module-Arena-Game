using System.Collections.Generic;
using UnityEngine;

enum NumberOfRounds
{
    One,
    Two,
    Three,
    Four,
    Five
}
public class RoundManager : MonoBehaviour
{
    [SerializeField] private NumberOfRounds numOfRounds;
    [SerializeField] private GameStateManager gameStateManager;

    public int CurrentRound => (int)numOfRounds;
    public PlayerState RoundWinner { get; private set; }
    private bool roundsStarted;

    private float countdownDuration = 5f;
    private bool betweenRounds = false;

    public bool BetweenRounds => betweenRounds;
    public float Countdown => countdownDuration;


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

    //start the round before physics can report a player falling off
    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.GameStarted)
        {
            roundsStarted = false;
            betweenRounds = false;
            RoundWinner = null;
            return;
        }
        SyncPlayerSubscriptions();
        //keep eliminations if the manager was only disabled for a moment
        if (roundsStarted) return;
        numOfRounds = NumberOfRounds.One;
        StartRound();
        roundsStarted = true;
        Debug.Log($"Round started: {playersInGame} total, {alivePlayersLeft} alive");
    }
    
    void Update()
    {
        SyncPlayerSubscriptions();

        if (gameStateManager == null || !gameStateManager.GameHasStarted)
        {
            roundsStarted = false;
            playersInGame = gameStateManager != null ? gameStateManager.players.Count : 0;
            return;
        }

        if (betweenRounds)
        {
            countdownDuration -= Time.deltaTime;

            if (countdownDuration <= 0)
                NextRound();

            return;
        }

        //also check the count in case both players died before we saved a winner
        if (RoundWinner != null || (playersInGame >= 2 && alivePlayersLeft <= 1))
        {
            if (RoundWinner == null && alivePlayersLeft == 1)
                RoundWinner = trackedPlayers.Find(player => player != null && player.Status == PlayerStatus.Alive);

            Debug.Log(RoundWinner != null ? $"{RoundWinner.name} wins" : "Round draw");

            betweenRounds = true;
            countdownDuration = 5f;

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


        //round specific behaviour
        switch (numOfRounds)
        {
            case NumberOfRounds.One:
                //round one behaviour
            break;

            case NumberOfRounds.Two:
                //round two behaviour
            break;

            case NumberOfRounds.Three:
                //round threee behaviour
            break;

            case NumberOfRounds.Four:
                //round four behaviour
            break;

            case NumberOfRounds.Five:
                //round five behaviour
            break; 
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

        //save the winner as soon as the other player dies
        if (roundsStarted && RoundWinner == null && status == PlayerStatus.Eliminated && playersInGame >= 2 && alivePlayersLeft == 1)
            RoundWinner = trackedPlayers.Find(player => player != null && player.Status == PlayerStatus.Alive);
    }

    private void StartRound()
    {
        //put eliminated players back in for the next round
        foreach (PlayerState player in trackedPlayers)
            player.StartRound();
            
        RoundWinner = null;
        CountAlivePlayers();
    }

    public void NextRound()
    {
        if (!roundsStarted || !gameStateManager.GameHasStarted)
            return;


        betweenRounds = false;
        if (numOfRounds == NumberOfRounds.Five)
        {
            roundsStarted = false;
            RoundWinner = null;
            gameStateManager.GameOver();
            return;
        }

        numOfRounds++;
        playersInGame = gameStateManager.players.Count;

        StartRound();
        Debug.Log($"Round started: {playersInGame} total, {alivePlayersLeft} alive");
        
    }
}
