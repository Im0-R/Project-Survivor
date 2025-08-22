using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int level = 1;
    public int experience = 0;
    public int maxExperience = 100;

    [Header("Player Stats Modifiers")]
    public float speedMultiplier = 1.0f;
    public float damageMultiplier = 1.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
