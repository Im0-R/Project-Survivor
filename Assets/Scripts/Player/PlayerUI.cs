using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

    [Header("Bars")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider xpBar;

    [Header("Stats Panel")]
    [SerializeField] private PlayerStatsPanelUI statsPanelUI;

    public PlayerEntity playerEnt;

    private StatsComponent boundStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TryBindLocalPlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (playerEnt == null)
        {
            TryBindLocalPlayer();
            return;
        }

        if (CameraFollow.Instance != null && !CameraFollow.Instance.HasTarget)
            SetCameraTarget();

        UpdateUI();
    }

    private void TryBindLocalPlayer()
    {
        if (!NetworkClient.active) return;
        if (NetworkClient.localPlayer == null) return;

        PlayerEntity ent = NetworkClient.localPlayer.GetComponent<PlayerEntity>();
        if (ent == null) return;

        if (playerEnt == ent) return;

        Bind(ent);
    }

    public void Bind(PlayerEntity ent)
    {
        if (ent == null) return;

        playerEnt = ent;

        Debug.Log($"[PlayerUI] Bound to local player netId={ent.netId}");

        BindStatsPanel();
        SetCameraTarget();
        UpdateUI();
    }

    private void BindStatsPanel()
    {
        if (playerEnt == null) return;

        StatsComponent stats = playerEnt.StatComp;

        if (stats == null)
        {
            Debug.LogError("[PlayerUI] PlayerEntity.StatComp is null.");
            return;
        }

        if (boundStats == stats) return;

        boundStats = stats;

        if (statsPanelUI != null)
            statsPanelUI.Bind(stats);
        else
            Debug.LogWarning("[PlayerUI] statsPanelUI is not assigned in inspector.");
    }

    private void SetCameraTarget()
    {
        if (playerEnt == null) return;

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetTarget(playerEnt.transform);

        Debug.Log($"[PlayerUI] Targeted local player netId={playerEnt.netId}");
    }

    private void UpdateUI()
    {
        if (playerEnt == null || playerEnt.StatComp == null) return;

        float maxHp = Mathf.Max(1f, playerEnt.StatComp.Get(StatId.MaxHealth));
        float maxXp = Mathf.Max(1f, playerEnt.StatComp.Get(StatId.MaxExperience));

        if (healthBar != null)
            healthBar.value = (playerEnt.StatComp.Get(StatId.CurrentHealth) / maxHp) * 100f;

        if (xpBar != null)
            xpBar.value = (playerEnt.StatComp.Get(StatId.Experience) / maxXp) * 100f;
    }
}