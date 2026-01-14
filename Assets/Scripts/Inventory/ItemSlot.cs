using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    //displaying item image on each slot from an itemInstance
    ItemInstance itemInstance;

    // Stack properties
    public bool isStackable;
    public int maxStackSize;
    public int currentStackSize;
    public int slotIndex;
    private void SetImage()
    {
        // Set the image based on itemInstance.baseId or other properties
    }
}
