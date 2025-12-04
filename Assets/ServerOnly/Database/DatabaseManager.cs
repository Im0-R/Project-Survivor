#if !UNITY_CLIENT || UNITY_EDITOR
using SQLite;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using Mirror;

/// <summary>
/// manages user accounts and data using SQLite.
/// </summary>
public static class DatabaseManager
{
    private static SQLiteConnection db;

    // ==============================Initialize
    // INITIALISATION DATABASE
    // ==============================
    public static void Initialize()
    {
        // Empêche les clients de lancer la DB
        if (!Application.isBatchMode)  // client = FALSE, serveur headless = TRUE
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
        Debug.Log($"[DB] Initialized at: {dbPath}");
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


    public static void DeleteUser(string username)
    {
        var user = db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
        if (user != null)
        {
            db.Delete(user);
            Debug.Log($"[DB] User '{username}' deleted.");
        }
        else
        {
            Debug.LogWarning($"[DB] User '{username}' not found.");
        }
    }

    public static void UpdatePassword(string username, string oldPassword, string newPassword)
    {
        var user = db.Table<UserAccount>().FirstOrDefault(u =>
            u.Username == username && u.Password == HashPassword(oldPassword));

        if (user != null)
        {
            user.Password = HashPassword(newPassword);
            db.Update(user);
            Debug.Log($"[DB] Password updated for '{username}'.");
        }
        else
        {
            Debug.LogWarning($"[DB] Invalid old password for '{username}'.");
        }
    }

    public static UserAccount GetUser(string username)
    {
        return db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
    }
    // ==============================
    // GENERALS METHODS
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

    // ==============================
    // SECURITY
    // ==============================
    private static string HashPassword(string input)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return System.BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
    // Equipment Side

    public static void SaveEquipment(int userId, PlayerEquipmentData equip)
    {
        var user = db.Table<UserAccount>().First(u => u.Id == userId);
        user.EquipmentJson = JsonUtility.ToJson(equip);
        db.Update(user);
    }
    public static PlayerEquipmentData LoadEquipment(int userId)
    {
        var user = db.Table<UserAccount>().First(u => u.Id == userId);

        if (string.IsNullOrEmpty(user.EquipmentJson))
            return new PlayerEquipmentData();

        return JsonUtility.FromJson<PlayerEquipmentData>(user.EquipmentJson);
    }

}

/// <summary>
/// User-Data connected to the database.
/// </summary>
public class UserAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Username { get; set; }

    public string Password { get; set; }

    //Player Progression
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;


    //Equipment System
    public string EquipmentJson { get; set; } = "{}";

    //inventory System
    public string InventoryJson { get; set; } = "[]";
}

#endif
