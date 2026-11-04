using UnityEngine;
using TMPro;
public class RoundUI : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private GameObject roundPanel;
    [SerializeField] private TMP_Text roundBeginningLabel;
    [SerializeField] private TMP_Text countdownLabel;

    private int lastRound = -1;
    private float beginningTime;

    void Update()
    {
        if (roundManager == null || gameStateManager == null) return;

        if (!gameStateManager.GameHasStarted)
        {
            roundPanel.SetActive(false);
            lastRound = -1;
            return;
        }

        //show the next round while waiting
        if (roundManager.BetweenRounds)
        {
            roundPanel.SetActive(true);
            roundBeginningLabel.text = roundManager.CurrentRound == 4 ? "GAME ENDS IN" : $"ROUND {roundManager.CurrentRound + 2} BEGINS IN";
            countdownLabel.text = Mathf.CeilToInt(roundManager.Countdown).ToString();
            return;
        }

        //show the beginning of each round briefly
        if (lastRound != roundManager.CurrentRound)
        {
            lastRound = roundManager.CurrentRound;
            beginningTime = 2f;
        }

        beginningTime -= Time.deltaTime;
        roundPanel.SetActive(beginningTime > 0);
        roundBeginningLabel.text = $"ROUND {roundManager.CurrentRound + 1} BEGINS";
        countdownLabel.text = "";
    }
}
