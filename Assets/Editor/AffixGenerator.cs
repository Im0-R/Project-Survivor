using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;


//Create an affix for each of the value in StatId enum
public static class AffixGenerator
{

    [MenuItem("Tools/Affixes/Generate Affixes")]

    public static void GenerateAffixes()
    {
        //Get all values from StatId enum
        IEnumerable<StatId> statIds = Enum.GetValues(typeof(StatId)).Cast<StatId>();
        //Create a folder "Assets/Resources/Affixes" if it doesn't exist
        string folderPath = "Assets/Resources/ScriptableObjects/Affixes";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        foreach (StatId statId in statIds)
        {
            string assetPath = Path.Combine(folderPath, $"{statId}Affix.asset");
            if (Directory.Exists(assetPath))
            {
                Debug.LogWarning($"Affix for {statId} already exists. Skipping.");
                continue;
            }


            //Create a new AffixSO
            AffixSO affix = ScriptableObject.CreateInstance<AffixSO>();
            affix.stat = statId;
            affix.minValue = 1;
            affix.maxValue = 5;


            //Save the AffixSO as an asset
            if (Directory.Exists(assetPath))
            {
                Debug.LogWarning($"Affix for {statId} already exists. Skipping.");
                continue;
            }
            AssetDatabase.CreateAsset(affix, assetPath);

        }


        //Refresh the AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated affixes for all StatId values.");
    }

}
