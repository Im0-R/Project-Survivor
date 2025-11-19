using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    [MenuItem("Build/Server Build")]
    public static void BuildServer()
    {
        // --- PATH ---
        string buildPath = "Builds/Server";
        string exeName = "ServerBuild.x86_64";
        string fullPath = Path.Combine(buildPath, exeName);

        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        // --- SCENES ---
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // --- CONFIG ---
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
        EditorUserBuildSettings.development = false;

        // --- BUILD OPTIONS ---
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = fullPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.CompressWithLz4 | BuildOptions.EnableHeadlessMode
        };

        // --- BUILD ---
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($"Build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }
        else
        {
            UnityEngine.Debug.Log($"Server build succeeded: {report.summary.outputPath}");
            BuildInfo.Version = "Build " + System.DateTime.Now.ToString("yyyyMMddHHmm");

            // --- make executable on Linux ---
            TryMakeExecutable(fullPath);
        }
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
            UnityEngine.Debug.Log($"chmod +x applied to {path}");
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"Could not set executable permissions for {path}");
        }
    }
}
