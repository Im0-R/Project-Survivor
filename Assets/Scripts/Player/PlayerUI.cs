using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider xpBar;


    [SerializeField] private PlayerEntity playerEnt;

    private void Awake()
    {
        // Singleton for easy access
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetPlayer(PlayerEntity p)
    {
        playerEnt = p;
        //Hide 
    }

    private void Update()
    {
        if (playerEnt != null)
            UpdateUI();
    }

    private void UpdateUI()
    {
        healthBar.value = (playerEnt.currentHealth.Value / playerEnt.currentHealth.Value) * 100f;
        xpBar.value = (playerEnt.experience.Value / playerEnt.maxExperience.Value) * 100f;
    }
}
