using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SigilStack
{
    public int sigilId;
    public int amount;

    public SigilStack(int sigilId, int amount)
    {
        this.sigilId = sigilId;
        this.amount = amount;
    }
}

[CreateAssetMenu(menuName = "Game/Sigils/Sigil Database")]
public class SigilDatabaseSO : ScriptableObject
{
    public List<SigilSO> sigils = new();

    public SigilSO GetById(int id)
    {
        foreach (SigilSO sigil in sigils)
        {
            if (sigil != null && sigil.sigilId == id)
                return sigil;
        }

        return null;
    }
}