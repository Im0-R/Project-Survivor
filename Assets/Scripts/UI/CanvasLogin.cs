using Mirror;
using UnityEngine;
using TMPro;
public class CanvasLogin : MonoBehaviour
{
    private NetworkManager manager;
    public TMP_InputField addressInput;
    void Start()
    {
        manager = FindAnyObjectByType<NetworkManager>();
        addressInput.text = NetworkManager.singleton.networkAddress;
        if (addressInput == null)
        {
            Debug.LogError("[CanvasLogin] Aucun InputField trouvé dans les enfants !");
        }
        if (manager == null)
        {
            Debug.LogError("[CustomNetworkGUI] Aucun NetworkManager trouvé sur cet objet !");
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
        manager.networkAddress = addressInput.text;
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
}