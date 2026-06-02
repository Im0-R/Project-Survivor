using UnityEngine;

public class DeathCanvas : MonoBehaviour
{
    public static DeathCanvas Instance { get; private set; }

    [SerializeField] private GameObject panel;

    private PlayerEntity localPlayer;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    public void Open(PlayerEntity player)
    {
        localPlayer = player;

        if (panel != null)
            panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        localPlayer = null;
    }

    public void RespawnToTown()
    {
        if (localPlayer == null)
        {
            Debug.LogWarning("[DeathCanvas] No local player assigned");
            return;
        }

        localPlayer.CmdRespawnToTown();
        Close();
    }
}