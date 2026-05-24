using UnityEngine;

public static class RunRewardUtility
{
    public static string CreateStatReward(StatId stat, float value)
    {
        return $"STAT|{stat}|{value}";
    }

    public static string CreateSpellUpgradeReward(string spellName, string modifierName, float value)
    {
        return $"SPELL_UPGRADE|{spellName}|{modifierName}|{value}";
    }

    public static string GetRewardTitle(string rewardCode)
    {
        string[] parts = rewardCode.Split('|');

        if (parts[0] == "STAT")
            return parts[1];

        if (parts[0] == "SPELL_UPGRADE")
            return $"{parts[1]} Upgrade";

        return "Reward";
    }

    public static string GetRewardDescription(string rewardCode)
    {
        string[] parts = rewardCode.Split('|');

        if (parts[0] == "STAT")
            return $"+{parts[2]} {parts[1]} for this run";

        if (parts[0] == "SPELL_UPGRADE")
            return $"+{parts[3]} {parts[2]} on {parts[1]} for this run";

        return "";
    }
}