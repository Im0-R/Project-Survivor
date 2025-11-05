using UnityEngine;

public class CanvasCreateAccount : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_InputField inputUsername;
    [SerializeField]
    private TMPro.TMP_InputField inputPassword;

   public void CreateAccount()
    {
        string username = inputUsername.text;
        string password = inputPassword.text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Username or Password cannot be empty.");
            return;
        }
        DatabaseManager.InsertUser(username, password);
        Debug.Log($"Account created for user: {username}");
        GetComponentInParent<CanvasSwitcher>().SwitchCanvas("CanvasLogin");
    }
}
