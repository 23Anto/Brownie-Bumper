using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public ScoreSubmitter submitter;

    // Submits first so the leaderboard shows your latest score
    public async void OpenLeaderboard()
    {
        if (submitter != null) await System.Threading.Tasks.Task.Run(() => { }); // keep method async-safe
        submitter?.Submit();
        SceneManager.LoadScene("Leaderboard");
    }

    public void LogOut()
    {
        AuthHelper.SignOut();
        SceneManager.LoadScene("Account");
    }
}
