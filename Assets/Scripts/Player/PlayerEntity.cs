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

    private bool pendingSpellReward = false;

    private string[] currentRewardChoices;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
    }

    protected override void Update()
    {
        if (isLocalPlayer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        HandleDebugInput();
#endif
        }

        if (!isServer) return;

        base.Update();

        if (agent != null)
            agent.speed = StatComp.Get(StatId.MoveSpeedMult);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (StatComp != null)
            StatComp.OnLevelUpServer += HandleLevelUpServer;
    }

    public override void OnStopServer()
    {
        if (StatComp != null)
            StatComp.OnLevelUpServer -= HandleLevelUpServer;

        base.OnStopServer();
    }

    [Server]
    private void HandleLevelUpServer(int newLevel)
    {
        if (pendingSpellReward)
            return;

        List<string> rewardSpells = BuildRewardSpellChoices();

        if (rewardSpells == null || rewardSpells.Count == 0)
        {
            Debug.LogWarning($"[PlayerEntity] No reward spells available for {name}");
            return;
        }

        currentRewardChoices = rewardSpells.ToArray();
        pendingSpellReward = true;

        TargetShowSpellRewardUI(connectionToClient, currentRewardChoices, newLevel);
    }

    [TargetRpc]
    private void TargetShowSpellRewardUI(NetworkConnection target, string[] spellNames, int newLevel)
    {
        StartCoroutine(ShowSpellRewardUIWhenReady(spellNames, newLevel));
    }

    private IEnumerator ShowSpellRewardUIWhenReady(string[] spellNames, int newLevel)
    {
        while (PlayerUI.Instance == null)
            yield return null;

        while (UIManager.Instance == null)
            yield return null;

        UIManager.Instance.ShowSpellsRewardUI(spellNames, newLevel);

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.InputBlocked = true;
    }
    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CmdTriggerDebugSpellReward();
        }
    }
    [Server]
    private void TriggerSpellRewardSelection(int displayLevel = -1)
    {
        if (pendingSpellReward)
            return;

        List<string> rewardSpells = BuildRewardSpellChoices();

        if (rewardSpells == null || rewardSpells.Count == 0)
        {
            Debug.LogWarning($"[PlayerEntity] No reward spells available for {name}");
            return;
        }

        currentRewardChoices = rewardSpells.ToArray();
        pendingSpellReward = true;

        int shownLevel = displayLevel >= 0 ? displayLevel : StatComp.level;
        TargetShowSpellRewardUI(connectionToClient, currentRewardChoices, shownLevel);
    }
    [Server]
    private List<string> BuildRewardSpellChoices()
    {
        var owned = new HashSet<string>();

        foreach (var spell in GetAllActiveSpells())
        {
            if (spell != null && spell.GetData() != null)
                owned.Add(spell.GetData().spellName);
        }

        var results = new List<string>();
        int safety = 0;

        while (results.Count < 3 && safety < 20)
        {
            safety++;

            Spell randomSpell = SpellsManager.Instance.GetRandomSpellServer(owned);
            if (randomSpell == null)
                break;

            string spellName = randomSpell.GetData().spellName;
            if (string.IsNullOrWhiteSpace(spellName))
                continue;

            if (results.Contains(spellName))
                continue;

            results.Add(spellName);
            owned.Add(spellName);
        }

        return results;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        StartCoroutine(LoadAndBindPlayerUI());
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

    [Command]
    public void CmdChooseSpellReward(string spellName)
    {
        if (!pendingSpellReward)
        {
            Debug.LogWarning("[SERVER] No pending spell reward.");
            return;
        }

        if (string.IsNullOrWhiteSpace(spellName))
        {
            Debug.LogWarning("[SERVER] Empty spell reward choice.");
            return;
        }

        if (currentRewardChoices == null || Array.IndexOf(currentRewardChoices, spellName) < 0)
        {
            Debug.LogWarning($"[SERVER] Spell reward choice not allowed: {spellName}");
            return;
        }

        Spell spell = SpellsManager.Instance.GetSpell(spellName);
        if (spell == null)
        {
            Debug.LogWarning($"[SERVER] Spell reward does not exist: {spellName}");
            return;
        }

        Spell ownedSpell = GetSpellByName(spellName);
        if (ownedSpell != null)
        {
            if (!ownedSpell.IsMaxLevel())
                UpgradeSpell(spellName);
            else
                Debug.Log($"[SERVER] {spellName} already max level on {name}");
        }
        else
        {
            AddSpell(spellName);
        }

        pendingSpellReward = false;
        currentRewardChoices = null;

        TargetHideSpellRewardUI(connectionToClient);
    }
    [Command]
    private void CmdTriggerDebugSpellReward()
    {
        TriggerSpellRewardSelection();
    }
    [Command]
    private void CmdGiveDebugSpell(string spellName)
    {
        Spell spell = SpellsManager.Instance.GetSpell(spellName);
        if (spell == null)
        {
            Debug.LogWarning($"[SERVER] Debug spell not found: {spellName}");
            return;
        }

        Spell ownedSpell = GetSpellByName(spellName);
        if (ownedSpell != null)
        {
            if (!ownedSpell.IsMaxLevel())
                UpgradeSpell(spellName);
            else
                Debug.Log($"[SERVER] {spellName} already max level on {name}");
        }
        else
        {
            AddSpell(spellName);
        }
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
}