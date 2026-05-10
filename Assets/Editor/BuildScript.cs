using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[System.Obsolete]
public static class BuildScript
{
    [MenuItem("Build/Build ALL Servers")]
    public static void BuildAll()
    {
        BuildMasterServer();
        BuildInstanceServer();
    }

    [MenuItem("Build/Server Build MASTER")]
    public static void BuildMasterServer()
    {
        string buildPath = "Builds/Master";
        string exeName = "MasterServer.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && s.path.EndsWith("Server_Main.unity"))
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("❌ No Server_Main.unity found in Build Settings!");
            return;
        }

        Debug.Log("=== MASTER BUILD SCENES ===");
        foreach (var scene in scenes)
            Debug.Log(scene);

        Build(fullPath, scenes);
        Debug.Log("✅ MASTER build OK → " + fullPath);
    }

    [MenuItem("Build/Server Build INSTANCE")]
    public static void BuildInstanceServer()
    {
        string buildPath = "Builds/Instance";
        string exeName = "InstanceServer.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s =>
                s.enabled &&
                (
                    s.path.EndsWith("BootStrapInstance.unity") ||
                    s.path.EndsWith("Town.unity") ||
                    s.path.EndsWith("MapInstance.unity")
                ))
            .OrderBy(s =>
            {
                if (s.path.EndsWith("BootStrapInstance.unity")) return 0;
                if (s.path.EndsWith("Town.unity")) return 1;
                if (s.path.EndsWith("MapInstance.unity")) return 2;
                return 99;
            })
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("❌ No BootStrapInstance/Town/MapInstance found in Build Settings!");
            return;
        }

        bool hasBootstrap = scenes.Any(s => s.EndsWith("BootStrapInstance.unity"));
        if (!hasBootstrap)
        {
            Debug.LogError("❌ BootStrapInstance.unity is missing from INSTANCE build.");
            return;
        }

        Debug.Log("=== INSTANCE BUILD SCENES ===");
        foreach (var scene in scenes)
            Debug.Log(scene);

        Build(fullPath, scenes);
        Debug.Log("✅ INSTANCE build OK → " + fullPath);
    }

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
            Debug.Log("✔ Build succeeded: " + fullPath);
            MakeExecutable(fullPath);

            string buildFolder = Path.GetDirectoryName(fullPath);
            string rootUnityPlayer = Path.Combine(buildFolder, "UnityPlayer.so");

            if (!File.Exists(rootUnityPlayer))
                Debug.LogWarning("⚠ UnityPlayer.so not found next to executable. Server may fail to start.");
            else
                Debug.Log("✔ UnityPlayer.so present next to executable: " + rootUnityPlayer);
        }
        else
        {
            Debug.LogError("❌ Build FAILED: " + report.summary.result);
        }

        RemoveDefine("UNITY_SERVER", BuildTargetGroup.Standalone);
    }

    private static void AddDefine(string define, BuildTargetGroup target)
    {
        List<string> defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target)
            .Split(';')
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();

        if (!defs.Contains(define))
        {
            defs.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", defs));
        }
    }

    private static void RemoveDefine(string define, BuildTargetGroup target)
    {
        List<string> defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target)
            .Split(';')
            .Where(d => !string.IsNullOrWhiteSpace(d) && d != define)
            .ToList();

        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", defs));
    }

    private static void MakeExecutable(string path)
    {
        System.Diagnostics.Process.Start("/bin/chmod", $"+x \"{path}\"");
    }
}