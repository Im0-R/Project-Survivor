using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider xpBar;

    public PlayerEntity playerEnt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //no DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // try to bind immediately
        TryBindLocalPlayer();
    }

    private void Update()
    {
        //if not bound yet, try to bind
        if (playerEnt == null)
        {
            TryBindLocalPlayer();
            return;
        }

        if (playerEnt != null && !CameraFollow.Instance.HasTarget)
        {
            SetCameraTarget();
        }
        UpdateUI();
    }

    private void TryBindLocalPlayer()
    {
        if (!NetworkClient.active) return;
        if (NetworkClient.localPlayer == null) return;

        PlayerEntity ent = NetworkClient.localPlayer.GetComponent<PlayerEntity>();
        if (ent == null) return;

        if (playerEnt == ent) return;

        playerEnt = ent;
        Debug.Log($"[PlayerUI] Bound to local player netId={NetworkClient.localPlayer.netId}");

    }
    private void SetCameraTarget()
    {

        //Bind camera to player
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetTarget(playerEnt.transform);
        Debug.Log($"[PlayerUI] Targetted local player netId={NetworkClient.localPlayer.netId}");


    }

    private void UpdateUI()
    {
        float maxHp = Mathf.Max(1f, playerEnt.maxHealth);
        float maxXp = Mathf.Max(1f, playerEnt.maxExperience);

        healthBar.value = (playerEnt.currentHealth / maxHp) * 100f;
        xpBar.value = (playerEnt.experience / maxXp) * 100f;
    }
}
