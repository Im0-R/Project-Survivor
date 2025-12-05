using UnityEditor;
using UnityEditor.Build;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    [MenuItem("Build/Server Build")]
    public static void BuildServer()
    {
        // --- TARGET ---
        NamedBuildTarget serverTarget = NamedBuildTarget.Server;

        // --- ADD UNITY_SERVER DEFINE ---
        AddDefine("UNITY_SERVER", serverTarget);

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

        foreach (string scen in scenes)
        {
            UnityEngine.Debug.Log($"Including scene in build: {scen}");
        }
        // --- CONFIGURE BUILD ---
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneLinux64
        );

        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.CompressWithLz4
        };

        // --- BUILD ---
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError(
                $"Build failed: {report.summary.result} ({report.summary.totalErrors} errors)"
            );
        }
        else
        {
            UnityEngine.Debug.Log($"Server build succeeded: {report.summary.outputPath}");
            TryMakeExecutable(fullPath);
            foreach (string scen in scenes)
            {
                UnityEngine.Debug.Log($"Including scene in build: {scen}");
            }
        }

        RemoveDefine("UNITY_SERVER", serverTarget);
    }

    private static void TryMakeExecutable(string path)
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/bin/chmod";
            process.StartInfo.Arguments = $"+x \"{path}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit();
        }
        catch { }
    }

    private static void AddDefine(string define, NamedBuildTarget target)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(target);
        var list = defines.Split(';').Where(d => d.Length > 0).ToList();

        if (!list.Contains(define))
        {
            list.Add(define);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
        }
    }

    private static void RemoveDefine(string define, NamedBuildTarget target)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(target);
        var list = defines.Split(';').Where(d => d.Length > 0 && d != define).ToList();
        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
    }
}
