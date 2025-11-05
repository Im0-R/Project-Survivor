using Mirror;
using UnityEngine;
using TMPro;
public class CanvasLogin : MonoBehaviour
{
    private NetworkManager manager;
    [SerializeField] TMP_InputField IF_username;
    [SerializeField] TMP_InputField IF_password;
    void Start()
    {
        manager = FindAnyObjectByType<NetworkManager>();
    
        if (manager == null)
        {
            Debug.LogError("[CustomNetworkGUI] No NetworkManager found in this object !");
        }

    }
    public void StartHost()
    {
        manager.StartHost();
        UIManager.Instance.ShowGameUI();
    }
    public void StartServer()
    {
        manager.StartServer();
        UIManager.Instance.ShowGameUI();
    }
    public void StartClient()
    {
        manager.StartClient();
        UIManager.Instance.ShowGameUI();
    }
    public void QuitApplication()
    {
        Application.Quit();
    }
    public void Disconnect()
    {
        manager.StopHost();
        manager.StopServer();
        manager.StopClient();
        UIManager.Instance.ShowLoginUI();
    }

    //Localhost client for testing
    public void StartLocalClient()
    {
        manager.networkAddress = "localhost";
        manager.StartClient();
        UIManager.Instance.ShowGameUI();
    }
    public void TryLogin()
    {

    }
}