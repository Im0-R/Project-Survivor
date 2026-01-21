using UnityEngine;
using UnityEngine.EventSystems;

public class LootUIManager : MonoBehaviour
{
    public static LootUIManager Instance;

    [SerializeField] private LootLabelUI labelPrefab;
    [SerializeField] private Canvas canvas;

    [SerializeField]
    private

    void Awake() => Instance = this;

    public void RegisterLoot(LootPickup loot)
    {
        LootLabelUI label = Instantiate(labelPrefab, canvas.transform);
        label.Bind(loot);

        Debug.Log($"[LootUIManager] Registered loot UI for {loot.GetItem().itemName}");
    }

    public void RequestPickup(LootPickup loot)
    {
        // Appelé depuis UI
        if (PlayerPickupController.Local == null)
        {
            Debug.LogError("[LootUIManager] No local PlayerPickupController found!");
            return;
        }
        PlayerPickupController.Local.RequestPickup(loot);
    }
}
