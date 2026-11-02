using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    public TMP_Text boardText;
    public TMP_Text myRankText;

    async void Start() { await Refresh(); }

    public async void OnRefreshClicked() { await Refresh(); }
    public void OnBackClicked() { SceneManager.LoadScene("Game"); }

    async System.Threading.Tasks.Task Refresh()
    {
        if (boardText != null) boardText.text = "Loading...";
        string board = await LeaderboardManager.GetTopTen();
        string rank = await LeaderboardManager.GetMyRank();

        // The scene may have changed while we were waiting on the network
        if (boardText != null) boardText.text = board;
        if (myRankText != null) myRankText.text = rank;
    }
}