using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance;

    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider xpBar;
    [SerializeField] public PlayerStats playerStats;
    private PlayerEntity playerEnt;

    private void Awake()
    {
        // Singleton for easy access
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetPlayer(PlayerEntity p)
    {
        playerEnt = p;
        UpdateUI();
    }

    private void Update()
    {
        if (playerEnt != null)
            UpdateUI();
    }

    private void UpdateUI()
    {
        healthBar.value = playerEnt.health.Value / playerStats.maxHealth; 
        xpBar.value = playerStats.experience / playerStats.experience;
    }
}
