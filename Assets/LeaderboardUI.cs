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
        boardText.text = "Loading...";
        boardText.text = await LeaderboardManager.GetTopTen();
        myRankText.text = await LeaderboardManager.GetMyRank();
    }
}