using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

[System.Obsolete]
public static class BuildScript
{
    // ============================================================
    //  BUILD ALL SERVERS
    // ============================================================
    [MenuItem("Build/Build ALL Servers")]
    public static void BuildAll()
    {
        BuildMasterServer();
        BuildInstanceServer();
    }

    // ============================================================
    //  BUILD MASTER SERVER (Server_Main only)
    // ============================================================
    [MenuItem("Build/Server Build MASTER")]
    public static void BuildMasterServer()
    {
        string buildPath = "Builds/Master";
        string exeName = "MasterServer.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.path.EndsWith("Server_Main.unity"))
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
            UnityEngine.Debug.LogError("❌ No Server_Main.unity found in Build Settings!");

        Build(fullPath, scenes);
        UnityEngine.Debug.Log("✅ MASTER build OK → " + fullPath);
    }

    // ============================================================
    //  BUILD INSTANCE SERVER (Town + MapScene)
    // ============================================================
    [MenuItem("Build/Server Build INSTANCE")]
    public static void BuildInstanceServer()
    {
        string buildPath = "Builds/Instance";
        string exeName = "InstanceServer.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s =>
                s.path.EndsWith("Server_Main.unity") ||
                s.path.EndsWith("Town.unity") ||
                s.path.EndsWith("MapScene.unity"))
            .OrderBy(s =>
            {
                if (s.path.EndsWith("Server_Main.unity")) return 0;
                if (s.path.EndsWith("Town.unity")) return 1;
                if (s.path.EndsWith("MapScene.unity")) return 2;
                return 99;
            })
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            UnityEngine.Debug.LogError("❌ No Server_Main/Town/MapScene found in Build Settings!");
            return;
        }

        UnityEngine.Debug.Log("=== INSTANCE BUILD SCENES ===");
        foreach (var scene in scenes)
            UnityEngine.Debug.Log(scene);

        Build(fullPath, scenes);
        UnityEngine.Debug.Log("✅ INSTANCE build OK → " + fullPath);
    }

    // ============================================================
    //  COMMON BUILD METHOD
    // ============================================================
    private static void Build(string fullPath, string[] scenes)
    {
        AddDefine("UNITY_SERVER", BuildTargetGroup.Standalone);

        // FORCE Linux Dedicated Server build target
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneLinux64
        );

        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.allowDebugging = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("✔ Build succeeded: " + fullPath);
            MakeExecutable(fullPath);

            // ========================================================
            // UnityPlayer.so should stay next to the executable
            // ========================================================
            string buildFolder = Path.GetDirectoryName(fullPath);
            string rootUnityPlayer = Path.Combine(buildFolder, "UnityPlayer.so");

            if (!File.Exists(rootUnityPlayer))
            {
                UnityEngine.Debug.LogWarning("⚠ UnityPlayer.so not found next to executable. Server may fail to start.");
            }
            else
            {
                UnityEngine.Debug.Log("✔ UnityPlayer.so present next to executable: " + rootUnityPlayer);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("❌ Build FAILED: " + report.summary.result);
        }

        RemoveDefine("UNITY_SERVER", BuildTargetGroup.Standalone);
    }

    // ============================================================
    // DEFINES
    // ============================================================
    private static void AddDefine(string define, BuildTargetGroup target)
    {
        var defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target)
            .Split(';')
            .Where(d => d.Length > 0)
            .ToList();

        if (!defs.Contains(define))
        {
            defs.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", defs));
        }
    }

    private static void RemoveDefine(string define, BuildTargetGroup target)
    {
        var defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target)
            .Split(';')
            .Where(d => d.Length > 0 && d != define)
            .ToList();

        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", defs));
    }

    // ============================================================
    // MAKE EXECUTABLE
    // ============================================================
    private static void MakeExecutable(string path)
    {
        System.Diagnostics.Process.Start("/bin/chmod", $"+x \"{path}\"");
    }
}
