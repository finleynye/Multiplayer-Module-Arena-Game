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
    private bool roundsStarted;

    public int playersInGame;
    public int alivePlayersLeft;

    void Awake()
    {

        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();
    }
    
    void Update()
    {
        if (gameStateManager == null || !gameStateManager.GameHasStarted)
        {
            roundsStarted = false;
            playersInGame = gameStateManager != null ? gameStateManager.players.Count : 0;
            return;
        }

        if (gameStateManager.GameHasStarted && !roundsStarted)
        {
            playersInGame = gameStateManager.players.Count;
            alivePlayersLeft = playersInGame;
            Debug.Log($"Round started: {playersInGame} total, {alivePlayersLeft} alive");
            numOfRounds = NumberOfRounds.One;
            roundsStarted = true;
        }

        if (playersInGame >= 2 && alivePlayersLeft == 1)
        {
            //announce the winner here
            Debug.Log("Player Wins");
            NextRound();
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
    public void NextRound()
    {
        if (!roundsStarted || !gameStateManager.GameHasStarted)
            return;

        if (numOfRounds == NumberOfRounds.Five)
        {
            roundsStarted = false;
            gameStateManager.GameOver();
            return;
        }

        numOfRounds++;
        playersInGame = gameStateManager.players.Count;
        alivePlayersLeft = playersInGame;
        Debug.Log($"Round started: {playersInGame} total, {alivePlayersLeft} alive");
    }
}
