# Security and Access Control Guide

This guide explains how to protect your Project-Survivor files from unauthorized access.

## Table of Contents
1. [GitHub Repository Access Control](#github-repository-access-control)
2. [File System Permissions](#file-system-permissions)
3. [Protecting Sensitive Data](#protecting-sensitive-data)
4. [Unity Asset Protection](#unity-asset-protection)

## GitHub Repository Access Control

### Making Your Repository Private

The most effective way to restrict access to your project files on GitHub is to make your repository private:

#### On GitHub.com:
1. Go to your repository: `https://github.com/Im0-R/Project-Survivor`
2. Click on **Settings** (tab at the top of the repository)
3. Scroll down to the **Danger Zone** section
4. Click on **Change visibility**
5. Select **Make private**
6. Confirm by typing the repository name

**Benefits of a Private Repository:**
- Only you (and collaborators you explicitly invite) can view the code
- Only you can clone, pull, or fork the repository
- Search engines won't index your code
- Free for unlimited private repositories on GitHub

### Managing Collaborators

If you need to give specific people access to your private repository:

1. Go to **Settings** → **Collaborators and teams**
2. Click **Add people**
3. Enter their GitHub username or email
4. Choose their permission level:
   - **Read**: Can view and clone
   - **Write**: Can push to repository
   - **Admin**: Full access including settings

## File System Permissions

### On Linux/macOS

To restrict file access on your local machine:

```bash
# Navigate to your project directory
cd /path/to/Project-Survivor

# Make all files readable and writable only by you
chmod -R 700 .

# Or more specifically:
# 700 = Owner: read, write, execute | Group: none | Others: none
```

To set ownership to yourself only:
```bash
sudo chown -R $(whoami):$(whoami) .
```

### On Windows

1. Right-click on the project folder
2. Select **Properties**
3. Go to the **Security** tab
4. Click **Advanced**
5. Click **Disable inheritance**
6. Choose **Remove all inherited permissions**
7. Click **Add** → **Select a principal**
8. Enter your username
9. Grant **Full control** only to your account

## Protecting Sensitive Data

### What NOT to Commit to Git

Ensure these files are in your `.gitignore` (already configured):

- `/[Ll]ibrary/` - Unity's cached data
- `/[Tt]emp/` - Temporary files
- `/[Uu]ser[Ss]ettings/` - Personal Unity settings
- `/[Mm]emoryCaptures/` - May contain sensitive data
- `*.user` - User-specific settings
- `.vs/` - Visual Studio cache

### Additional Sensitive Files to Exclude

Add these to your `.gitignore` if applicable:

```gitignore
# API Keys and Credentials
*.env
*.key
*_credentials.json
config/secrets.json

# Database files
*.db
*.sqlite
*.sqlite3

# Personal notes and documentation
NOTES.md
TODO_PRIVATE.md
```

### For API Keys and Secrets in Code

Never hardcode sensitive information. Use environment variables or Unity's PlayerPrefs:

```csharp
// BAD - Don't do this
string apiKey = "1234567890abcdef";

// GOOD - Use environment variables
string apiKey = System.Environment.GetEnvironmentVariable("API_KEY");

// Or use Unity PlayerPrefs for local storage
string apiKey = PlayerPrefs.GetString("API_KEY");
```

## Unity Asset Protection

### For Public Repositories

If you keep your repository public but want to protect specific assets:

1. **Use Asset Bundles**: Build assets into encrypted bundles
2. **Code Obfuscation**: Use tools like Obfuscator to make code harder to read
3. **Binary Assets**: Store assets in binary formats rather than text
4. **Separate Private Repository**: Keep sensitive assets in a private submodule

### Asset Store Content

If you're using purchased Unity Asset Store assets:

- **Never commit Asset Store content** to public repositories (violates terms)
- Add Asset Store folders to `.gitignore`:
  ```gitignore
  /Assets/ThirdParty/PurchasedAssets/
  ```

### Steam/Epic Integration Keys

For multiplayer games with platform integration:

```gitignore
# Steam
/Assets/Plugins/Steamworks.NET/steam_appid.txt
/Assets/StreamingAssets/steam_appid.txt

# Epic
/Assets/Plugins/Epic/credentials.json
```

## Best Practices Summary

### Essential Steps:
1. ✅ **Make repository private** on GitHub (most important)
2. ✅ **Review `.gitignore`** to ensure sensitive files are excluded
3. ✅ **Never commit credentials** or API keys
4. ✅ **Set appropriate file permissions** on your local system
5. ✅ **Regularly audit** who has access to your repository

### For Team Collaboration:
- Use GitHub's collaborator feature with minimal necessary permissions
- Consider branch protection rules
- Use Pull Requests for code review
- Enable two-factor authentication on GitHub

### For Solo Projects:
- Private repository is usually sufficient
- Back up your code regularly (GitHub is one backup)
- Consider using encrypted backups for extra security

## Additional Resources

- [GitHub: About repository visibility](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/setting-repository-visibility)
- [GitHub: Permission levels for a personal account repository](https://docs.github.com/en/account-and-profile/setting-up-and-managing-your-personal-account-on-github/managing-personal-account-settings/permission-levels-for-a-personal-account-repository)
- [Unity: Asset security best practices](https://docs.unity3d.com/Manual/AssetBundles-Browser.html)
- [Git: Removing sensitive data from a repository](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)

## Questions?

If you need help with any of these security measures, please open an issue in the repository or contact the repository administrator.
