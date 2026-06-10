using System.Collections.Generic;
using UnityEngine;

public static class SigilDatabase
{
    private static Dictionary<int, SigilSO> sigilsById;
    private static SigilSO[] allSigils;

    public static void Initialize()
    {
        allSigils = Resources.LoadAll<SigilSO>("ScriptableObjects/Sigils");
        sigilsById = new Dictionary<int, SigilSO>();

        foreach (SigilSO sigil in allSigils)
        {
            if (sigil == null)
                continue;

            if (sigilsById.ContainsKey(sigil.sigilId))
            {
                Debug.LogError($"[SigilDatabase] Duplicate sigilId={sigil.sigilId}");
                continue;
            }

            sigilsById.Add(sigil.sigilId, sigil);
        }

        Debug.Log($"[SigilDatabase] Loaded {sigilsById.Count} sigils.");
    }

    public static SigilSO Get(int sigilId)
    {
        if (sigilsById == null)
            Initialize();

        sigilsById.TryGetValue(sigilId, out SigilSO sigil);
        return sigil;
    }

    public static SigilSO[] GetAll()
    {
        if (allSigils == null)
            Initialize();

        return allSigils;
    }
}