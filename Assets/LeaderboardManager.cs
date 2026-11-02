using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;

public static class LeaderboardManager
{
    const string LeaderboardId = "brownie_total";

    public static async Task SubmitScore(long score)
    {
        try
        {
            await AuthHelper.EnsureSignedIn(); // Initializes services and signs in if needed
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
        }
        catch (System.Exception e) { Debug.LogError("Submit failed: " + e.Message); }
    }

    public static async Task<string> GetTopTen()
    {
        try
        {
            await AuthHelper.EnsureSignedIn();
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
            await AuthHelper.EnsureSignedIn();
            var me = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
            return $"Your rank: {me.Rank + 1} ({me.Score})";
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            return "You haven't submitted a score yet."; // Genuine "no entry" case only
        }
        catch (System.Exception e)
        {
            return "Couldn't load your rank: " + e.Message; // Surfaces real problems
        }
    }
}