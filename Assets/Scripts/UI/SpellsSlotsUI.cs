using System.Collections.Generic;
using UnityEngine;

public class SpellsSlotsUI : MonoBehaviour
{
    public static SpellsSlotsUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private SpellSlotUI spellSlotPrefab;

    private readonly List<SpellSlotUI> slots = new();
    private NetworkEntity currentEntity;

    private void Awake()
    {
        Instance = this;
    }

    public void Bind(NetworkEntity entity)
    {
        if (currentEntity != null)
            currentEntity.OnSpellsChanged -= Refresh;

        currentEntity = entity;

        if (currentEntity != null)
            currentEntity.OnSpellsChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (currentEntity != null)
            currentEntity.OnSpellsChanged -= Refresh;
    }

    public void Refresh()
    {
        ClearSlots();

        if (currentEntity == null)
            return;

        List<Spell> spells = currentEntity.GetAllActiveSpells();

        foreach (Spell spell in spells)
        {
            if (spell == null || spell.GetData() == null)
                continue;

            SpellSlotUI slot = Instantiate(spellSlotPrefab, slotsParent);
            slot.SetSpell(spell.GetData().UISprite);

            slots.Add(slot);
        }
    }

    public void TriggerCooldown(string spellName, float cooldownDuration)
    {
        if (currentEntity == null)
            return;

        List<Spell> spells = currentEntity.GetAllActiveSpells();

        for (int i = 0; i < spells.Count; i++)
        {
            if (spells[i]?.GetData()?.spellName == spellName)
            {
                slots[i].SetCooldown(cooldownDuration, cooldownDuration);
                return;
            }
        }
    }

    private void ClearSlots()
    {
        foreach (SpellSlotUI slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slots.Clear();
    }
}