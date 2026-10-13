using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class AuthHelper
{
    static bool initialized;

    // Initialize Unity Services once, no matter how many scenes call it
    static async Task EnsureInit()
    {
        if (initialized) return;
        await UnityServices.InitializeAsync();
        initialized = true;
    }

    public static async Task SignUp(string username, string password)
    {
        await EnsureInit();
        await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        // The leaderboard shows the player name, which is separate from the login username
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
        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut();
    }

    public static bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized
        && AuthenticationService.Instance.IsSignedIn;
}