using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

public static class AffixGenerator
{
    [MenuItem("Tools/Affixes/Generate Affixes")]
    public static void GenerateAffixes()
    {
        IEnumerable<StatId> statIds = Enum.GetValues(typeof(StatId)).Cast<StatId>();

        string folderPath = "Assets/Resources/ScriptableObjects/Affixes";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        foreach (StatId statId in statIds)
        {
            string assetPath = Path.Combine(folderPath, $"{statId}Affix.asset");

            if (File.Exists(assetPath))
            {
                Debug.LogWarning($"Affix for {statId} already exists. Skipping.");
                continue;
            }

            AffixSO affix = ScriptableObject.CreateInstance<AffixSO>();
            affix.stat = statId;
            affix.weight = 100;

            affix.tiers = new AffixTier[]
            {
                new AffixTier
                {
                    tier = 1,
                    minItemLevel = 1,
                    minValue = 1,
                    maxValue = 5
                }
            };

            AssetDatabase.CreateAsset(affix, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Generated affixes for all StatId values.");
    }
}