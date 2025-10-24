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
        // Singleton for easy access
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void SetPlayer(PlayerEntity p)
    {
        playerEnt = p;
    }
    private void Update()
    {
        if (playerEnt == null) return;
            UpdateUI();
    }

    private void UpdateUI()
    {
        healthBar.value = (playerEnt.currentHealth / playerEnt.maxHealth) * 100f;
        xpBar.value = (playerEnt.experience / playerEnt.maxExperience) * 100f;
    }
}
