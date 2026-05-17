using UnityEngine;

public class ArcanaLoadoutSlotUI : MonoBehaviour
{
    [SerializeField] private ArcanaButtonUI arcanaSlot;
    [SerializeField] private RuneButtonUI[] runeSlots;

    public ArcanaButtonUI ArcanaSlot => arcanaSlot;
    public RuneButtonUI[] RuneSlots => runeSlots;
}