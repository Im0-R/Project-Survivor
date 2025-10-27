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

        // Prend toutes les scènes cochées dans Build Settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(buildPath, "ServerBuild.x86_64"),
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.CompressWithLz4
        };

        // 🧠 Sous-target Serveur
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        // Lance le build
        BuildPipeline.BuildPlayer(options);
    }
}
