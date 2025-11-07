#if UNITY_SERVER
using SQLite;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// DatabaseManager — version serveur uniquement
/// Gère les comptes utilisateurs et les modifications persistantes.
/// </summary>
public static class DatabaseManager
{
    private static SQLiteConnection db;

    // ==============================
    // 🔧 INITIALISATION
    // ==============================
    public static void Initialize()
    {
        string dbPath = Path.Combine(Application.dataPath, "database.db");
        db = new SQLiteConnection(dbPath);
        db.CreateTable<UserAccount>();

        Debug.Log($"[DB] Initialized at: {dbPath}");
    }

    // ==============================
    // 👤 UTILISATEURS
    // ==============================

    public static bool ValidateUser(string username, string password)
    {
        string hashed = HashPassword(password);
        var user = db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
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
    // 🧰 MÉTHODES GÉNÉRALES
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
    // 🔐 SECURITÉ
    // ==============================
    private static string HashPassword(string input)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return System.BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}

/// <summary>
/// Structure de données utilisateur stockée dans la base.
/// </summary>
public class UserAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Username { get; set; }

    public string Password { get; set; }

    // Tu peux ajouter ici des champs persistants
    // comme les stats, items, XP, etc.
    public string EquippedItem { get; set; } = "none";
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
}
#endif
