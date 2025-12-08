using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

public static class BuildScript
{
    [MenuItem("Build/Server Build")]
    public static void BuildServer()
    {
        // --- APPLY UNITY_SERVER DEFINE TO STANDALONE GROUP ---
        AddDefine("UNITY_SERVER", BuildTargetGroup.Standalone);

        string buildPath = "Builds/Server";
        string exeName = "ServerBuild.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        Directory.CreateDirectory(buildPath);

        // --- SCENES ---
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .OrderByDescending(s => s.Contains("Server_Main"))
            .ToArray();

        // --- CONFIGURE BUILD TARGET ---
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneLinux64
        );

        // IMPORTANT : THIS ENABLES DEDICATED SERVER BUILD MODE
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode | BuildOptions.CompressWithLz4
        };

        // --- BUILD ---
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($"❌ Build failed: {report.summary.result}");
        }
        else
        {
            UnityEngine.Debug.Log($"✅ Server build succeeded: {report.summary.outputPath}");
            MakeExecutable(fullPath);
        }

        // REMOVE DEFINE ONLY AFTER BUILD
        RemoveDefine("UNITY_SERVER", BuildTargetGroup.Standalone);
    }

    private static void AddDefine(string define, BuildTargetGroup target)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        var list = defines.Split(';').Where(d => d.Length > 0).ToList();

        if (!list.Contains(define))
        {
            list.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", list));
        }
    }

    private static void RemoveDefine(string define, BuildTargetGroup target)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        var list = defines.Split(';').Where(d => d != define && d.Length > 0).ToList();
        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", list));
    }

    private static void MakeExecutable(string path)
    {
        System.Diagnostics.Process.Start("/bin/chmod", $"+x \"{path}\"");
    }
}
