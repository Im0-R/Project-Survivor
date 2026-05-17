using System;
using System.Collections.Generic;

[Serializable]
public class ArcanaLoadoutSlotData
{
    public string arcanaName = "";
    public List<string> runeIds = new();

    public ArcanaLoadoutSlotData()
    {
    }

    public ArcanaLoadoutSlotData(int runeSlotCount)
    {
        runeIds = new List<string>();

        for (int i = 0; i < runeSlotCount; i++)
            runeIds.Add("");
    }
}