using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AccountUI : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text statusText;

    // Hook each of these to a button's OnClick
    public async void OnSignUpClicked() { await Run(() => AuthHelper.SignUp(usernameInput.text, passwordInput.text)); }
    public async void OnSignInClicked() { await Run(() => AuthHelper.SignIn(usernameInput.text, passwordInput.text)); }
    public async void OnGuestClicked() { await Run(() => AuthHelper.SignInGuest()); }

    async System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> action)
    {
        statusText.text = "Working...";
        try
        {
            await action();
            SceneManager.LoadScene("Game");
        }
        catch (AuthenticationException e) { statusText.text = e.Message; }
        catch (RequestFailedException e) { statusText.text = e.Message; }
        catch (System.Exception e) { statusText.text = "Error: " + e.Message; }
    }
}