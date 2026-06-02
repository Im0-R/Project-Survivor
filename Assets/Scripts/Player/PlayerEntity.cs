using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerEntity : NetworkEntity
{
    public Transform firePoint;

    private NavMeshAgent agent;

    private RunStatsComponent runStats;
    private RunSpellModifiers runSpellMods;

    private bool pendingSpellReward = false;
    private string[] currentRewardChoices;

    protected override void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();
        runStats = GetComponent<RunStatsComponent>();
        runSpellMods = GetComponent<RunSpellModifiers>();
    }

    protected override void Update()
    {
        if (isLocalPlayer)
        {
            HandleDebugInput();
        }

        if (!isServer) return;

        base.Update();

        if (agent != null)
        {
            float moveSpeed = GetCurrentStat(StatId.MoveSpeedMult);
            agent.speed = moveSpeed;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartCoroutine(ApplyUsernameAsDisplayNameWhenReady());

        if (StatComp != null)
            StatComp.OnLevelUpServer += HandleLevelUpServer;

        StartCoroutine(GiveStarterBuild());
    }

    [Server]
    private IEnumerator ApplyUsernameAsDisplayNameWhenReady()
    {
        float timer = 0f;

        while (timer < 5f)
        {
            string username = connectionToClient?.authenticationData as string;

            if (!string.IsNullOrWhiteSpace(username))
            {
                StatComp.Name = username;
                Debug.Log($"[PlayerEntity] Display name set to username: {username}");
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("[PlayerEntity] Could not set display name, username not found after timeout.");
    }
    public override void OnStopServer()
    {
        if (StatComp != null)
            StatComp.OnLevelUpServer -= HandleLevelUpServer;

        base.OnStopServer();
    }

    // =====================================================
    // PUBLIC STAT ACCESS
    // =====================================================

    public float GetCurrentStat(StatId stat)
    {
        float baseValue = StatComp != null ? StatComp.Get(stat) : 0f;
        float runBonus = runStats != null ? runStats.GetBonus(stat) : 0f;

        return baseValue + runBonus;
    }

    public float GetSpellModifier(string spellName, string modifierName)
    {
        if (runSpellMods == null)
            return 0f;

        return runSpellMods.GetModifier(spellName, modifierName);
    }

    // =====================================================
    // PARTY
    // =====================================================
    [Command]
    public void CmdLeaveParty()
    {
        Debug.Log("[Server][Party] CmdLeaveParty received.");

        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[Server][Party] PartyManager.Instance is null.");
            return;
        }

        PartyManager.Instance.LeaveParty(this);
    }
    [Command]
    public void CmdInviteToParty(uint targetNetId)
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[Server][Party] PartyManager.Instance is null.");
            return;
        }

        PartyManager.Instance.InvitePlayer(this, targetNetId);
    }

    [Command]
    public void CmdTeleportToPartyMember(uint targetNetId)
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[Server][PartyTP] PartyManager.Instance is null.");
            return;
        }

        PartyManager.Instance.TeleportToPartyMember(this, targetNetId);
    }

    [Command]
    public void CmdTeleportToPartyMemberByName(string memberName)
    {
        Debug.Log($"[Server][PartyTP] CmdTeleportToPartyMemberByName received to {memberName}");

        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[Server][PartyTP] PartyManager.Instance is null.");
            return;
        }

        PartyManager.Instance.TeleportToPartyMemberByName(this, memberName);
    }

    [Command]
    public void CmdCompletePartyTeleport(string memberName)
    {
        Debug.Log($"[Server][PartyTP] CmdCompletePartyTeleport received to {memberName}");

        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[Server][PartyTP] PartyManager.Instance is null.");
            return;
        }

        PartyManager.Instance.CompletePartyTeleport(this, memberName);
    }

    [Command]
    public void CmdRequestPartyUIRefresh()
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[Server][Party] CmdRequestPartyUIRefresh failed, PartyManager.Instance is null.");
            return;
        }

        PartyManager.Instance.RefreshPartyUIFor(this);
    }

    [TargetRpc]
    public void TargetReceivePartyMembers(NetworkConnection target, string[] members)
    {
        Debug.Log($"[Client][Party] Received party members count={(members == null ? 0 : members.Length)}");

        if (CanvasPartyListUI.Instance != null)
            CanvasPartyListUI.Instance.SetMembers(members);
        else
            Debug.LogWarning("[Client][Party] CanvasPartyListUI.Instance is null.");
    }

    [TargetRpc]
    public void TargetSwitchToPartyMemberInstance(
        NetworkConnection target,
        string ip,
        int port,
        string sceneName,
        string targetMemberName)
    {
        Debug.Log($"[Client][PartyTP] Switching to {targetMemberName} instance {ip}:{port} scene={sceneName}");

        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[Client][PartyTP] ClientSideInstanceManager.Instance is null.");
            return;
        }

        ClientSideInstanceManager.Instance.SetPendingPartyTeleport(targetMemberName);

        ClientSideInstanceManager.Instance.SwitchToInstance(
            (ushort)port,
            ip,
            sceneName
        );
    }

    [TargetRpc]
    public void TargetSwitchToInstance(NetworkConnection target, int port, string sceneName)
    {
        Debug.Log($"[Client] Switching to instance {sceneName}:{port}");

        if (ClientSideInstanceManager.Instance == null)
        {
            Debug.LogError("[Client] ClientSideInstanceManager.Instance is null.");
            return;
        }

        ClientSideInstanceManager.Instance.SwitchToInstance(
            (ushort)port,
            "72.60.212.58",
            sceneName
        );
    }

    // =====================================================
    // LEVEL UP REWARD
    // =====================================================

    [Server]
    private void HandleLevelUpServer(int newLevel)
    {
        TriggerSpellRewardSelection(newLevel);
    }

    [Server]
    private void TriggerSpellRewardSelection(int displayLevel = -1)
    {
        if (pendingSpellReward)
            return;

        List<string> rewards = BuildRewardSpellChoices();

        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning($"[PlayerEntity] No run rewards available for {name}");
            return;
        }

        currentRewardChoices = rewards.ToArray();
        pendingSpellReward = true;

        int shownLevel = displayLevel >= 0 ? displayLevel : StatComp.level;
        TargetShowSpellRewardUI(connectionToClient, currentRewardChoices, shownLevel);
    }

    [Server]
    private List<string> BuildRewardSpellChoices()
    {
        List<string> choices = new();
        List<string> activeSpellNames = new();

        foreach (Spell spell in GetAllActiveSpells())
        {
            if (spell == null || spell.GetData() == null)
                continue;

            string spellName = spell.GetData().spellName;

            if (!string.IsNullOrWhiteSpace(spellName))
                activeSpellNames.Add(spellName);
        }

        int safety = 0;

        while (choices.Count < 3 && safety < 50)
        {
            safety++;

            bool chooseSpellUpgrade =
                activeSpellNames.Count > 0 &&
                UnityEngine.Random.value < 0.6f;

            string rewardCode;

            if (chooseSpellUpgrade)
            {
                string spellName = activeSpellNames[UnityEngine.Random.Range(0, activeSpellNames.Count)];

                string[] possibleMods =
                {
                    "Damage",
                    "ProjectileCount",
                    "ProjectileSpeed",
                    "Pierce",
                    "CooldownReduction"
                };

                string modName = possibleMods[UnityEngine.Random.Range(0, possibleMods.Length)];
                float value = GetSpellUpgradeValue(modName);

                rewardCode = RunRewardUtility.CreateSpellUpgradeReward(spellName, modName, value);
            }
            else
            {
                StatId[] possibleStats =
                {
                    StatId.MaxHealth,
                    StatId.MaxMana,
                    StatId.SpellDamage,
                    StatId.FireDamage,
                    StatId.CritChance,
                    StatId.CritDamage,
                    StatId.ProjectileSpeed,
                    StatId.CooldownReduction,
                    StatId.MoveSpeedMult
                };

                StatId stat = possibleStats[UnityEngine.Random.Range(0, possibleStats.Length)];
                float value = GetStatRewardValue(stat);

                rewardCode = RunRewardUtility.CreateStatReward(stat, value);
            }

            if (!choices.Contains(rewardCode))
                choices.Add(rewardCode);
        }

        return choices;
    }

    private float GetSpellUpgradeValue(string modName)
    {
        return modName switch
        {
            "Damage" => 10f,
            "ProjectileCount" => 1f,
            "ProjectileSpeed" => 10f,
            "Pierce" => 1f,
            "CooldownReduction" => 5f,
            _ => 1f
        };
    }

    private float GetStatRewardValue(StatId stat)
    {
        return stat switch
        {
            StatId.MaxHealth => 20f,
            StatId.MaxMana => 15f,
            StatId.SpellDamage => 5f,
            StatId.FireDamage => 5f,
            StatId.CritChance => 2f,
            StatId.CritDamage => 10f,
            StatId.ProjectileSpeed => 10f,
            StatId.CooldownReduction => 3f,
            StatId.MoveSpeedMult => 0.2f,
            _ => 1f
        };
    }

    [Command]
    public void CmdChooseSpellReward(string rewardCode)
    {
        if (!pendingSpellReward)
        {
            Debug.LogWarning("[Server] No pending run reward.");
            return;
        }

        if (string.IsNullOrWhiteSpace(rewardCode))
        {
            Debug.LogWarning("[Server] Empty run reward choice.");
            return;
        }

        if (currentRewardChoices == null || Array.IndexOf(currentRewardChoices, rewardCode) < 0)
        {
            Debug.LogWarning($"[Server] Run reward choice not allowed: {rewardCode}");
            return;
        }

        ApplyRunRewardServer(rewardCode);

        pendingSpellReward = false;
        currentRewardChoices = null;

        TargetHideSpellRewardUI(connectionToClient);
    }

    [Server]
    private void ApplyRunRewardServer(string rewardCode)
    {
        string[] parts = rewardCode.Split('|');

        if (parts.Length < 3)
            return;

        if (parts[0] == "STAT")
        {
            if (runStats == null)
                return;

            StatId stat = Enum.Parse<StatId>(parts[1]);
            float value = float.Parse(parts[2]);

            runStats.AddRunStatBonus(stat, value);
            return;
        }

        if (parts[0] == "SPELL_UPGRADE")
        {
            if (runSpellMods == null)
                return;

            if (parts.Length < 4)
                return;

            string spellName = parts[1];
            string modifierName = parts[2];
            float value = float.Parse(parts[3]);

            runSpellMods.AddModifier(spellName, modifierName, value);
        }
    }

    // =====================================================
    // UI
    // =====================================================
    [Server]
    public void ShowDeathCanvasServer()
    {
        if (connectionToClient == null)
        {
            Debug.LogError("[PlayerEntity] Cannot show death canvas, connectionToClient is null");
            return;
        }

        TargetShowDeathCanvas(connectionToClient);
    }

    [TargetRpc]
    private void TargetShowDeathCanvas(NetworkConnection target)
    {
        if (DeathCanvas.Instance == null)
        {
            Debug.LogError("[PlayerEntity] DeathCanvas.Instance is null");
            return;
        }

        DeathCanvas.Instance.Open(this);
    }

    [Command]
    public void CmdRespawnToTown()
    {
        if (InstanceRedirectManager.Instance == null)
        {
            Debug.LogError("[PlayerEntity] InstanceRedirectManager.Instance is null");
            return;
        }

        InstanceRedirectManager.Instance.RedirectToTown(connectionToClient);
    }

    [TargetRpc]
    private void TargetShowSpellRewardUI(NetworkConnection target, string[] rewardCodes, int newLevel)
    {
        StartCoroutine(ShowSpellRewardUIWhenReady(rewardCodes, newLevel));
    }

    private IEnumerator ShowSpellRewardUIWhenReady(string[] rewardCodes, int newLevel)
    {
        while (PlayerUI.Instance == null)
            yield return null;

        while (UIManager.Instance == null)
            yield return null;

        UIManager.Instance.ShowSpellsRewardUI(rewardCodes, newLevel);

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.InputBlocked = true;
    }

    [TargetRpc]
    private void TargetHideSpellRewardUI(NetworkConnection target)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.HideSpellsRewardUI();

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.InputBlocked = false;
    }

    // =====================================================
    // RUN RESET
    // =====================================================

    [Server]
    public void ClearRunProgressionServer()
    {
        if (runStats != null)
            runStats.ClearRunStats();

        if (runSpellMods != null)
            runSpellMods.ClearModifiers();

        if (StatComp != null)
        {
            StatComp.level = 1;
            StatComp.SetFinalStatServer(StatId.Experience, 0);
            StatComp.SetFinalStatServer(StatId.CurrentHealth, GetCurrentStat(StatId.MaxHealth));
            StatComp.SetFinalStatServer(StatId.CurrentMana, GetCurrentStat(StatId.MaxMana));
        }

        pendingSpellReward = false;
        currentRewardChoices = null;

        Debug.Log($"[PlayerEntity] Cleared run progression for {name}");
    }

    // =====================================================
    // CLIENT SETUP / DEBUG
    // =====================================================

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        StartCoroutine(LoadAndBindPlayerUI());
        StartCoroutine(CompletePartyTeleportWhenReady());
        StartCoroutine(RequestPartyUIRefreshWhenReady());
    }

    private IEnumerator LoadAndBindPlayerUI()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("PlayerUI", LoadSceneMode.Additive);

        while (!op.isDone)
            yield return null;

        while (PlayerUI.Instance == null)
            yield return null;

        PlayerUI.Instance.Bind(this);
    }

    private IEnumerator CompletePartyTeleportWhenReady()
    {
        yield return new WaitForSeconds(1f);

        if (ClientSideInstanceManager.Instance != null)
            ClientSideInstanceManager.Instance.TryCompletePendingPartyTeleport();
    }

    private IEnumerator RequestPartyUIRefreshWhenReady()
    {
        yield return new WaitForSeconds(1f);

        CmdRequestPartyUIRefresh();
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (CanvasArcana.Instance != null)
                CanvasArcana.Instance.Open();
        }
    }

    [Command]
    private void CmdTriggerDebugSpellReward()
    {
        TriggerSpellRewardSelection();
    }

    private IEnumerator GiveStarterBuild()
    {
        yield return new WaitForSeconds(1f);

        if (!isServer)
            yield break;

        PlayerArcanaLoadout loadout = GetComponent<PlayerArcanaLoadout>();

        if (loadout == null)
        {
            Debug.LogError("[PlayerEntity] PlayerArcanaLoadout missing.");
            yield break;
        }

        loadout.EquipStarterBuildIfEmptyServer();
    }
}