using Mirror;
using System.Diagnostics;

public class PlayerPauseController : NetworkBehaviour
{
    public static PlayerPauseController instance;
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    // Pause Request
    public void RequestPause()
    {
        if (!isLocalPlayer) return;
        CmdRequestPause();
        Debug.WriteLine("Pause requested from PlayerPauseController");
    }

    [Command]
    void CmdRequestPause()
    {
        ServerTimeManager.instance.PauseGame();
    }
    // Resume Request
    public void RequestResume()
    {
        if (!isLocalPlayer) return;
        CmdRequestResume();
        Debug.WriteLine("Resume requested from PlayerPauseController");
    }

    [Command]
    void CmdRequestResume()
    {
        ServerTimeManager.instance.ResumeGame();
    }
}