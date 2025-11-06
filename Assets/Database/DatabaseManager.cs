using SQLite;
using System.IO;
using System.Linq;
using UnityEngine;
public class UserAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public static class DatabaseManager
{
    private static SQLiteConnection db;
    public static void Initialize()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, " database.db ");
        db = new SQLiteConnection(dbPath);
        db.CreateTable<UserAccount>();
    }

    public static bool ValidateUser(string username, string password)
    {
        UserAccount user = db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
        return user != null && user.Password == password;
    }
    public static void InsertUser(string username, string password)
    {
        UserAccount newUser = new UserAccount { Username = username, Password = password };
        if (db.Table<UserAccount>().Any(u => u.Username == username))
        {
            Debug.LogWarning($"[DB] User with username {username} already exists.");
            return;
        }
        db.Insert(newUser);
    }
    //Delete user by username
    public static void DeleteUser(string username)
    {
        UserAccount user = db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
        if (user != null)
        {
            db.Delete(user);
        }
        else
        {
            Debug.LogWarning($"[DB] User with username {username} not found.");
        }
    }


    public static void UpdatePassword(string username, string oldPassword ,string newPassword)
    {
        UserAccount user = db.Table<UserAccount>().FirstOrDefault(u => u.Username == username && u.Password == oldPassword);
        if (user != null)
        {
            user.Password = newPassword;
            db.Update(user);
        }
        else
        {
            Debug.LogWarning($"[DB] User with username {username}  and the old password you entered is not found.");
        }
    }
    public static UserAccount GetUser(string username)
    {
        return db.Table<UserAccount>().FirstOrDefault(u => u.Username == username);
    }
    public static void Close()
    {
        db.Close();
    }
    public static bool isInitialized()
    {
        return db != null;
    }
}