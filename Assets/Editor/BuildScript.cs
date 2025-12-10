using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

public static class BuildScript
{
    // ============================================================
    //  BUILD LES 2 SERVEURS
    // ============================================================
    [MenuItem("Build/Build ALL Servers")]
    public static void BuildAll()
    {
        BuildMasterServer();
        BuildInstanceServer();
    }

    // ============================================================
    //  BUILD DU MASTER (1 SEULE SCÈNE !)
    // ============================================================
    [MenuItem("Build/Server Build MASTER")]
    public static void BuildMasterServer()
    {
        string buildPath = "Builds/Master";
        string exeName = "MasterServer.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        // SCÈNE UNIQUE POUR LE MASTER
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.path.Contains("Server_Main"))
            .Select(s => s.path)
            .ToArray();

        Build(fullPath, scenes);

        UnityEngine.Debug.Log("✅ MASTER build OK → " + fullPath);
    }

    // ============================================================
    //  BUILD DES INSTANCES / HUBS
    // ============================================================
    [MenuItem("Build/Server Build INSTANCE")]
    public static void BuildInstanceServer()
    {
        string buildPath = "Builds/Instance";
        string exeName = "InstanceServer.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        // SEULES SCÈNES DES INSTANCES
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.path.Contains("Town") || s.path.Contains("MapScene"))
            .OrderByDescending(s => s.path.Contains("Town"))
            .Select(s => s.path)
            .ToArray();

        Build(fullPath, scenes);

        UnityEngine.Debug.Log("✅ INSTANCE build OK → " + fullPath);
    }

    // ============================================================
    //  MÉTHODE COMMUNE
    // ============================================================
    private static void Build(string fullPath, string[] scenes)
    {
        AddDefine("UNITY_SERVER", BuildTargetGroup.Standalone);

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
            UnityEngine.Debug.Log("Build succeeded: " + fullPath);
            MakeExecutable(fullPath);
        }
        else
        {
            UnityEngine.Debug.LogError("❌ Build FAILED: " + report.summary.result);
        }

        RemoveDefine("UNITY_SERVER", BuildTargetGroup.Standalone);
    }

    // ============================================================
    //  OUTILS
    // ============================================================
    private static void AddDefine(string define, BuildTargetGroup target)
    {
        var defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target)
            .Split(';')
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
            .Where(d => d != define)
            .ToList();

        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", defs));
    }

    private static void MakeExecutable(string path)
    {
        System.Diagnostics.Process.Start("/bin/chmod", $"+x \"" + path + "\"");
    }
}
