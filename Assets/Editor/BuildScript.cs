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

        //  Récupère toutes les scènes cochées dans les Build Settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(buildPath, "ServerBuild.x86_64"),
            target = BuildTarget.StandaloneLinux64,

            //  Activation du mode headless et compression pour réduire la taille
            options = BuildOptions.CompressWithLz4 | BuildOptions.EnableHeadlessMode
        };

        // Sous-target Serveur (indique à Unity de builder sans rendu)
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        //  Facultatif mais utile en CI : désactive le batch reporting interactif
        EditorUserBuildSettings.development = false;

        //  Lance le build
        var report = BuildPipeline.BuildPlayer(options);

        //  Log en cas d’erreur (pratique sur Jenkins)
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($" Build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }
        else
        {
            UnityEngine.Debug.Log($" Build succeeded: {report.summary.outputPath}");
        }
    }
}
