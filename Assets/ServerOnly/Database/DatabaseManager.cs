#if !UNITY_CLIENT || UNITY_EDITOR
using SQLite;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using Mirror;

public static class DatabaseManager
{
    private static SQLiteConnection db;

    public static void Initialize()
    {
        if (!Application.isBatchMode)
        {
            Debug.Log("[DB] Skipped: Running as client, not server");
            return;
        }

        Debug.Log("[DB] Running in batch mode = true → server detected");

#if !UNITY_CLIENT
        Debug.Log("[DB] UNITY_SERVER = TRUE (server build)");
        string dbPath = "/home/server/database.db";
#else
        string dbPath = Path.Combine(Application.persistentDataPath, "database_server_debug.db");
        Debug.Log("[DB] UNITY_SERVER = FALSE but batchMode = TRUE → test server");
#endif

        db = new SQLiteConnection(dbPath);

        db.CreateTable<UserAccount>();
        db.CreateTable<PartyData>();
        db.CreateTable<PartyMemberData>();
        db.CreateTable<PlayerLocationData>();

        TryAddColumn("UserAccount", "StashJson", "TEXT DEFAULT ''");
        TryAddColumn("UserAccount", "ArcanaLoadoutJson", "TEXT DEFAULT ''");

        Debug.Log($"[DB] Initialized at: {dbPath}");
    }

    private static void TryAddColumn(string tableName, string columnName, string columnDefinition)
    {
        try
        {
            db.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
            Debug.Log($"[DB] Added column {columnName} to {tableName}");
        }
        catch
        {
            // Normal si la colonne existe déjà.
        }
    }

    // =====================================================
    // AUTH
    // =====================================================

    public static bool ValidateUser(string username, string password)
    {
        string hashed = HashPassword(password);
        UserAccount user = db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
        return user != null && user.Password == hashed;
    }

    public static void InsertUser(string username, string password)
    {
        if (db.Table<UserAccount>().Any(u => u.Username == username))
        {
            Debug.LogWarning($"[DB] Username '{username}' already exists.");
            return;
        }

        db.Insert(new UserAccount
        {
            Username = username,
            Password = HashPassword(password)
        });

        Debug.Log($"[DB] User '{username}' created.");
    }

    public static UserAccount GetUser(string username)
    {
        return db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
    }

    // =====================================================
    // PARTY
    // =====================================================

    public static int CreateParty(string usernameA, string usernameB)
    {
        if (string.IsNullOrWhiteSpace(usernameA) || string.IsNullOrWhiteSpace(usernameB))
            return 0;

        int existingPartyA = GetPartyId(usernameA);
        int existingPartyB = GetPartyId(usernameB);

        if (existingPartyA != 0 && existingPartyA == existingPartyB)
            return existingPartyA;

        if (existingPartyA != 0)
        {
            AddPlayerToParty(existingPartyA, usernameB);
            return existingPartyA;
        }

        if (existingPartyB != 0)
        {
            AddPlayerToParty(existingPartyB, usernameA);
            return existingPartyB;
        }

        PartyData party = new PartyData
        {
            CreatedAt = DateTime.UtcNow.ToString("O")
        };

        db.Insert(party);

        AddPlayerToParty(party.PartyId, usernameA);
        AddPlayerToParty(party.PartyId, usernameB);

        Debug.Log($"[DB] Party created id={party.PartyId} with {usernameA} + {usernameB}");

        return party.PartyId;
    }

    public static void AddPlayerToParty(int partyId, string username)
    {
        if (partyId <= 0 || string.IsNullOrWhiteSpace(username))
            return;

        PartyMemberData existing = db.Table<PartyMemberData>()
            .FirstOrDefault(m => m.PartyId == partyId && m.Username == username);

        if (existing != null)
            return;

        db.Insert(new PartyMemberData
        {
            PartyId = partyId,
            Username = username,
            JoinedAt = DateTime.UtcNow.ToString("O")
        });

        Debug.Log($"[DB] Added {username} to party {partyId}");
    }

    public static void RemovePlayerFromParty(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        PartyMemberData member = db.Table<PartyMemberData>()
            .FirstOrDefault(m => m.Username == username);

        if (member == null)
            return;

        int partyId = member.PartyId;

        db.Delete(member);

        int remainingMembers = db.Table<PartyMemberData>()
            .Count(m => m.PartyId == partyId);

        if (remainingMembers <= 0)
        {
            PartyData party = db.Table<PartyData>()
                .FirstOrDefault(p => p.PartyId == partyId);

            if (party != null)
                db.Delete(party);
        }

        Debug.Log($"[DB] Removed {username} from party {partyId}");
    }

    public static int GetPartyId(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return 0;

        PartyMemberData member = db.Table<PartyMemberData>()
            .FirstOrDefault(m => m.Username == username);

        return member != null ? member.PartyId : 0;
    }

    public static bool AreInSameParty(string usernameA, string usernameB)
    {
        int partyA = GetPartyId(usernameA);
        int partyB = GetPartyId(usernameB);

        return partyA != 0 && partyA == partyB;
    }

    public static string[] GetPartyMembers(string username)
    {
        int partyId = GetPartyId(username);

        if (partyId == 0)
            return Array.Empty<string>();

        return db.Table<PartyMemberData>()
            .Where(m => m.PartyId == partyId)
            .Select(m => m.Username)
            .ToArray();
    }

    // =====================================================
    // PLAYER LOCATION
    // =====================================================

    public static void UpdatePlayerLocation(string username, int currentPort, string currentScene)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        PlayerLocationData existing = db.Table<PlayerLocationData>()
            .FirstOrDefault(l => l.Username == username);

        if (existing == null)
        {
            db.Insert(new PlayerLocationData
            {
                Username = username,
                CurrentPort = currentPort,
                CurrentScene = currentScene,
                UpdatedAt = DateTime.UtcNow.ToString("O")
            });
        }
        else
        {
            existing.CurrentPort = currentPort;
            existing.CurrentScene = currentScene;
            existing.UpdatedAt = DateTime.UtcNow.ToString("O");

            db.Update(existing);
        }

        Debug.Log($"[DB] Location updated for {username}: {currentScene}:{currentPort}");
    }

    public static PlayerLocationData GetPlayerLocation(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        return db.Table<PlayerLocationData>()
            .FirstOrDefault(l => l.Username == username);
    }

    // =====================================================
    // CLEAR
    // =====================================================

    public static void ClearInventory(string username)
    {
        var user = GetUser(username);
        if (user == null) return;

        PlayerInventoryData emptyInventory = new PlayerInventoryData();

        for (int i = 0; i < 40; i++)
            emptyInventory.itemsJson.Add("");

        user.InventoryJson = JsonUtility.ToJson(emptyInventory);
        db.Update(user);

        Debug.Log($"[DB] Inventory cleared for {username}");
    }

    public static void ClearEquipment(string username)
    {
        var user = GetUser(username);
        if (user == null) return;

        user.EquipmentJson = JsonUtility.ToJson(new PlayerEquipmentData());
        db.Update(user);

        Debug.Log($"[DB] Equipment cleared for {username}");
    }

    public static void ClearStash(string username)
    {
        var user = GetUser(username);
        if (user == null) return;

        user.StashJson = JsonUtility.ToJson(new PlayerStashData());
        db.Update(user);

        Debug.Log($"[DB] Stash cleared for {username}");
    }

    public static void ClearArcanaLoadout(string username)
    {
        var user = GetUser(username);
        if (user == null) return;

        user.ArcanaLoadoutJson = "";
        db.Update(user);

        Debug.Log($"[DB] Arcana loadout cleared for {username}");
    }

    public static void ClearPlayerState(string username)
    {
        ClearInventory(username);
        ClearEquipment(username);
        ClearArcanaLoadout(username);

        Debug.Log($"[DB] Player state cleared for {username}");
    }

    // =====================================================
    // SAVE / LOAD
    // =====================================================

    public static void SavePlayerState(
        string username,
        PlayerInventory inv,
        PlayerEquipment equip,
        PlayerStash stash,
        PlayerArcanaLoadout arcanaLoadout)
    {
        var user = GetUser(username);

        if (user == null)
        {
            Debug.LogError($"[DB] Cannot save player state, user not found: {username}");
            return;
        }

        if (inv != null)
            user.InventoryJson = JsonUtility.ToJson(inv.GetSaveData());

        if (equip != null)
            user.EquipmentJson = JsonUtility.ToJson(equip.GetSaveData());

        if (stash != null)
            user.StashJson = JsonUtility.ToJson(stash.GetSaveData());

        if (arcanaLoadout != null)
            user.ArcanaLoadoutJson = arcanaLoadout.ToSaveJsonServer();

        db.Update(user);

        Debug.Log($"[DB] Saved player state + stash + arcana loadout for {username}");
    }

    [Server]
    public static void SavePlayerStateFromConnection(NetworkConnectionToClient conn)
    {
        if (db == null)
        {
            Debug.LogError("[DB] Save failed: database not initialized.");
            return;
        }

        if (conn == null || conn.identity == null)
        {
            Debug.LogError("[DB] Save failed: connection or identity null.");
            return;
        }

        string username = conn.authenticationData as string;

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("[DB] Save failed: username missing.");
            return;
        }

        PlayerInventory inv = conn.identity.GetComponent<PlayerInventory>();
        PlayerEquipment equip = conn.identity.GetComponent<PlayerEquipment>();
        PlayerStash stash = conn.identity.GetComponent<PlayerStash>();
        PlayerArcanaLoadout arcanaLoadout = conn.identity.GetComponent<PlayerArcanaLoadout>();

        SavePlayerState(username, inv, equip, stash, arcanaLoadout);
    }

    public static void SaveStash(string username, PlayerStash stash)
    {
        var user = GetUser(username);
        if (user == null || stash == null) return;

        user.StashJson = JsonUtility.ToJson(stash.GetSaveData());
        db.Update(user);

        Debug.Log($"[DB] Stash saved for {username}");
    }

    public static void SaveArcanaLoadout(string username, PlayerArcanaLoadout arcanaLoadout)
    {
        var user = GetUser(username);
        if (user == null || arcanaLoadout == null) return;

        user.ArcanaLoadoutJson = arcanaLoadout.ToSaveJsonServer();
        db.Update(user);

        Debug.Log($"[DB] Arcana loadout saved for {username}");
    }

    public static void LoadPlayerState(
        string username,
        PlayerInventory inv,
        PlayerEquipment equip,
        PlayerStash stash,
        PlayerArcanaLoadout arcanaLoadout)
    {
        var user = GetUser(username);

        if (user == null)
        {
            Debug.LogError($"[DB] Cannot load player state, user not found: {username}");
            return;
        }

        PlayerInventoryData inventoryData = new PlayerInventoryData();
        PlayerEquipmentData equipmentData = new PlayerEquipmentData();
        PlayerStashData stashData = new PlayerStashData();

        if (!string.IsNullOrWhiteSpace(user.InventoryJson) && user.InventoryJson.StartsWith("{"))
            inventoryData = JsonUtility.FromJson<PlayerInventoryData>(user.InventoryJson);

        if (!string.IsNullOrWhiteSpace(user.EquipmentJson) && user.EquipmentJson.StartsWith("{"))
            equipmentData = JsonUtility.FromJson<PlayerEquipmentData>(user.EquipmentJson);

        if (!string.IsNullOrWhiteSpace(user.StashJson) && user.StashJson.StartsWith("{"))
            stashData = JsonUtility.FromJson<PlayerStashData>(user.StashJson);

        if (inv != null)
            inv.LoadSaveData(inventoryData);

        if (equip != null)
            equip.LoadSaveData(equipmentData);

        if (stash != null)
            stash.LoadSaveData(stashData);

        if (arcanaLoadout != null)
            arcanaLoadout.LoadFromSaveJsonServer(user.ArcanaLoadoutJson);

        Debug.Log($"[DB] Loaded player state + stash + arcana loadout for {username}");
    }

    public static PlayerStashData LoadStash(string username)
    {
        var user = GetUser(username);

        if (user == null)
            return new PlayerStashData();

        if (string.IsNullOrWhiteSpace(user.StashJson) || !user.StashJson.StartsWith("{"))
            return new PlayerStashData();

        return JsonUtility.FromJson<PlayerStashData>(user.StashJson);
    }

    public static string LoadArcanaLoadoutJson(string username)
    {
        var user = GetUser(username);

        if (user == null)
            return "";

        return user.ArcanaLoadoutJson;
    }

    // =====================================================
    // UTILS
    // =====================================================

    public static void Close()
    {
        db?.Close();
        db = null;
        Debug.Log("[DB] Connection closed.");
    }

    public static bool IsInitialized()
    {
        return db != null;
    }

    private static string HashPassword(string input)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}

public class UserAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Username { get; set; }

    public string Password { get; set; }

    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;

    public string EquipmentJson { get; set; } = "";
    public string InventoryJson { get; set; } = "";
    public string StashJson { get; set; } = "";
    public string ArcanaLoadoutJson { get; set; } = "";
}

public class PartyData
{
    [PrimaryKey, AutoIncrement]
    public int PartyId { get; set; }

    public string CreatedAt { get; set; }
}

public class PartyMemberData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PartyId { get; set; }

    [Indexed]
    public string Username { get; set; }

    public string JoinedAt { get; set; }
}

public class PlayerLocationData
{
    [PrimaryKey]
    public string Username { get; set; }

    public int CurrentPort { get; set; }

    public string CurrentScene { get; set; }

    public string UpdatedAt { get; set; }
}

#endif