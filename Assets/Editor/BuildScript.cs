using UnityEditor;
using System.IO;
using System.Linq;

public static class BuildScript
{
    [MenuItem("Build/Server Build")]
    public static void BuildServer()
    {
        string buildPath = "Builds/Server";
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        //Get all enabled scenes in build settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // Configure build options
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(buildPath, "ServerBuild.x86_64"),
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.CompressWithLz4
        };

        // subTarget for a server build
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        //Deactivate development build
        EditorUserBuildSettings.development = false;

        // Launch build
        var report = BuildPipeline.BuildPlayer(options);

        // result logging
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($"Build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }
        else
        {
            UnityEngine.Debug.Log($"Build succeeded: {report.summary.outputPath}");
        }
    }
}
