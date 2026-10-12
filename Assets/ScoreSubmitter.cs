using UnityEngine;

public class ScoreSubmitter : MonoBehaviour
{
    public float intervalSeconds = 30f;
    float timer;
    long lastSubmitted = -1;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= intervalSeconds) { timer = 0; Submit(); }
    }

    // Also submit when leaving the app or opening the leaderboard
    void OnApplicationPause(bool paused) { if (paused) Submit(); }
    void OnApplicationQuit() { Submit(); }

    public async void Submit()
    {
        long score = GameManager.Instance.TotalEarned;
        if (score == lastSubmitted) return; // Avoid redundant network calls
        lastSubmitted = score;
        await LeaderboardManager.SubmitScore(score);
    }
}