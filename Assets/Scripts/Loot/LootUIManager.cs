using Mirror;
using UnityEngine;

public class LootUIManager : MonoBehaviour
{
    public static LootUIManager Instance;

    [SerializeField] private LootLabelUI labelPrefab;
    [SerializeField] private Canvas canvas;

    void Awake() => Instance = this;

    void Update()                                      
    {
        bool show = Input.GetKey(KeyCode.LeftAlt);
        canvas.enabled = show;
    }

    public void RegisterLoot(LootPickup loot)
    {
        var label = Instantiate(labelPrefab, canvas.transform);
        label.Bind(loot);
    }

    public void RequestPickup(LootPickup loot)
    {
        // Appelé depuis UI
        PlayerPickupController.Local.RequestPickup(loot);
    }
}
