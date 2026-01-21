using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    //displaying item image on each slot from an itemInstance
   [SerializeField] private ItemInstance itemInstance;

    // Stack properties
    public bool isStackable;
    public int maxStackSize;
    public int currentStackSize;
    public int slotIndex;
}
