#if !UNITY_CLIENT || UNITY_EDITOR
using SQLite;
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

        TryAddColumn("UserAccount", "StashJson", "TEXT DEFAULT ''");

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

    // ==============================
    // USER
    // ==============================

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

    // ==============================
    // CLEAR
    // ==============================

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

    public static void ClearPlayerState(string username)
    {
        ClearInventory(username);
        ClearEquipment(username);

        // Décommente si tu veux que L supprime aussi le stash :
        // ClearStash(username);

        Debug.Log($"[DB] Player state cleared for {username}");
    }

    // ==============================
    // SAVE
    // ==============================

    public static void SavePlayerState(string username, PlayerInventory inv, PlayerEquipment equip, PlayerStash stash)
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

        db.Update(user);

        Debug.Log($"[DB] Saved player state + stash for {username}");
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

        SavePlayerState(username, inv, equip, stash);
    }

    public static void SaveStash(string username, PlayerStash stash)
    {
        var user = GetUser(username);
        if (user == null || stash == null) return;

        user.StashJson = JsonUtility.ToJson(stash.GetSaveData());
        db.Update(user);

        Debug.Log($"[DB] Stash saved for {username}");
    }

    // ==============================
    // LOAD
    // ==============================

    public static void LoadPlayerState(string username, PlayerInventory inv, PlayerEquipment equip, PlayerStash stash)
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

        Debug.Log($"[DB] Loaded player state + stash for {username}");
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

    // ==============================
    // GENERAL
    // ==============================

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
            return System.BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
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
}

#endif