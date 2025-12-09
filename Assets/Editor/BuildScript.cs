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
        // --- ADD UNITY_SERVER DEFINE ---
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

        // --- SWITCH TO LINUX SERVER TARGET ---
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneLinux64
        );

        // VERY IMPORTANT : disable development build & profiling
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.allowDebugging = false;

        // Enable dedicated server subtarget
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        // --- BUILD OPTIONS ---
        // ⚠️ NO DevelopmentBuild
        // ⚠️ NO AllowDebugging
        // ⚠️ NO ConnectProfiler (causait le shutdown)
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullPath,
            target = BuildTarget.StandaloneLinux64,

            // Headless build + compression (safe options)
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

        // REMOVE UNITY_SERVER DEFINE AFTER BUILD
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
