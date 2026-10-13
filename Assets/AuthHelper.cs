using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class AuthHelper
{
    // Checks the real service state instead of a static flag that can go stale
    static async Task EnsureInit()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized) return;

        if (UnityServices.State == ServicesInitializationState.Initializing)
        {
            // Another caller is already initializing, so wait for it to finish
            while (UnityServices.State == ServicesInitializationState.Initializing)
                await Task.Yield();
            return;
        }

        await UnityServices.InitializeAsync();
    }

    // Call before any leaderboard request. Safe to call repeatedly.
    public static async Task EnsureSignedIn()
    {
        await EnsureInit();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Fallback for direct scene testing
    }

    public static async Task SignUp(string username, string password)
    {
        await EnsureInit();
        await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
    }

    public static async Task SignIn(string username, string password)
    {
        await EnsureInit();
        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
    }

    public static async Task SignInGuest()
    {
        await EnsureInit();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public static void SignOut()
    {
        if (IsSignedIn) AuthenticationService.Instance.SignOut();
    }

    public static bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized
        && AuthenticationService.Instance.IsSignedIn;
}