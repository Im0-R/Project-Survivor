using UnityEngine;

public enum EquipmentSlot { Weapon, Helmet, Chest, Boots }

[CreateAssetMenu(menuName = "Game/ItemData")]
public class ItemDataSO : ScriptableObject
{
    public int itemId;
    public string itemName;
    public EquipmentSlot slot;

    [Header("Stats")]
    public int attack;
    public int defense;
    public int vitality;

}
