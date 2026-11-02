using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    bool loading;

    public async void OpenLeaderboard()
    {
        if (loading) return;
        loading = true;
        try { await LeaderboardManager.SubmitScore(GameManager.Instance.TotalEarned); }
        finally { SceneManager.LoadScene("Leaderboard"); }
    }

    public void LogOut()
    {
        AuthHelper.SignOut();
        SceneManager.LoadScene("Account");
    }
}