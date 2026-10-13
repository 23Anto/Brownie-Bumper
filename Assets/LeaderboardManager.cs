using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;

public static class LeaderboardManager
{
    const string LeaderboardId = "brownie_total";

    public static async Task SubmitScore(long score)
    {
        if (!AuthHelper.IsSignedIn) return; // Skip if testing the Game scene directly
        try { await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score); }
        catch (System.Exception e) { Debug.LogError("Submit failed: " + e.Message); }
    }

    public static async Task<string> GetTopTen()
    {
        try
        {
            var result = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId,
                new GetScoresOptions { Offset = 0, Limit = 10 });

            string text = "";
            foreach (var entry in result.Results)
            {
                bool isMe = entry.PlayerId == AuthenticationService.Instance.PlayerId;
                // Rank is zero-indexed, so add 1. Arrow marks the current player.
                text += $"{(isMe ? "> " : "")}{entry.Rank + 1}. {entry.PlayerName}: {entry.Score}\n";
            }
            return text.Length > 0 ? text : "No scores yet.";
        }
        catch (System.Exception e) { return "Failed to load: " + e.Message; }
    }

    public static async Task<string> GetMyRank()
    {
        try
        {
            var me = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
            return $"Your rank: {me.Rank + 1} ({me.Score})";
        }
        catch { return "You haven't submitted a score yet."; } // Throws if the player has no entry
    }
}