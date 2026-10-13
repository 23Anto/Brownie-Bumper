using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public async void OpenLeaderboard()
    {
        await LeaderboardManager.SubmitScore(GameManager.Instance.TotalEarned);
        SceneManager.LoadScene("Leaderboard"); 
    }

    public void LogOut()
    {
        AuthHelper.SignOut();
        SceneManager.LoadScene("Account");
    }
}