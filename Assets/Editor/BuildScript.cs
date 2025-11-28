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
        AddDefine("UNITY_SERVER", NamedBuildTarget.Standalone);

        string buildPath = "Builds/Server";
        string exeName = "ServerBuild.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        scenes = scenes.OrderByDescending(s => s.Contains("Server_Main")).ToArray();


                /// CONFIG BUILD SETTINGS
            EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneLinux64
        );
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
        EditorUserBuildSettings.development = false;

        /// CONFIG PLAYER SETTINGS  
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
            BuildInfo.Version = "Build " + System.DateTime.Now.ToString("yyyyMMddHHmm");

            TryMakeExecutable(fullPath);
        }
    }
    //             MAKE THE SERVER EXECUTABLE ON LINUX
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
            UnityEngine.Debug.Log($"chmod +x applied to {path}");
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"Could not set executable permissions for {path}");
        }
    }


                 // SET THE DEFINE NEEDED FOR SERVER BUILD
    private static void AddDefine(string define, NamedBuildTarget target)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(target);

        var defineList = defines.Split(';')
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();

        if (!defineList.Contains(define))
        {
            defineList.Add(define);
            string newDefines = string.Join(";", defineList);
            PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
            UnityEngine.Debug.Log($"[BuildScript] Added define '{define}' to {target}: {newDefines}");
        }
    }
}
