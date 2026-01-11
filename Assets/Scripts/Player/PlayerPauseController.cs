using Mirror;

public class PlayerPauseController : NetworkBehaviour
{
    public static PlayerPauseController instance;
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void RequestPause()
    {
        if (!isLocalPlayer) return;
        CmdRequestPause();
    }

    [Command]
    void CmdRequestPause()
    {
        ServerTimeManager.instance.PauseGame();
    }
    public void RequestResume()
    {
        if (!isLocalPlayer) return;
        CmdRequestResume();
    }

    [Command]
    void CmdRequestResume()
    {
        ServerTimeManager.instance.ResumeGame();
    }
}