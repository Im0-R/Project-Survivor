using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    [MenuItem("Build/Server Build (Profile)")]
    public static void BuildServerProfile()
    {
        // --- Fetch internal BuildProfileStore ---
        Type storeType = Type.GetType("UnityEditor.Build.Profile.BuildProfileStore, UnityEditor");
        if (storeType == null)
        {
            UnityEngine.Debug.LogError("Could not load BuildProfileStore (internal Unity API missing).");
            return;
        }

        // --- Get Build Profile by name ---
        MethodInfo getByName = storeType.GetMethod("GetProfileByName",
            BindingFlags.Public | BindingFlags.Static);

        if (getByName == null)
        {
            UnityEngine.Debug.LogError("Could not find 'GetProfileByName' method.");
            return;
        }

        object profile = getByName.Invoke(null, new object[] { "Linux Server" });
        if (profile == null)
        {
            UnityEngine.Debug.LogError("Build Profile 'Linux Server' not found.");
            return;
        }

        Type profileType = profile.GetType();
        UnityEngine.Debug.Log("Loaded Build Profile: Linux Server");

        // --- Read scenes included in the Build Profile ---
        var scenesProp = profileType.GetProperty("Scenes");
        var scenesList = scenesProp?.GetValue(profile) as System.Collections.IEnumerable;

        if (scenesList != null)
        {
            UnityEngine.Debug.Log("Scenes included in build:");

            foreach (var scene in scenesList)
            {
                var sceneType = scene.GetType();
                string path = sceneType.GetProperty("Path")?.GetValue(scene)?.ToString();
                bool enabled = (bool)(sceneType.GetProperty("Enabled")?.GetValue(scene) ?? false);

                UnityEngine.Debug.Log($"   - {path}   (enabled: {enabled})");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Unable to list scenes (internal API might have changed).");
        }

        // --- Read and log scripting defines ---
        var definesProp = profileType.GetProperty("ScriptingDefines");
        string defines = definesProp?.GetValue(profile)?.ToString() ?? "";

        UnityEngine.Debug.Log($"Scripting Defines: {defines}");

        // --- Configure output path ---
        string outputPath = "Builds/Server/ServerBuild.x86_64";
        profileType.GetProperty("OutputPath")?.SetValue(profile, outputPath);

        UnityEngine.Debug.Log($"Output Path set to: {outputPath}");

        // --- Execute build ---
        Type executorType = Type.GetType("UnityEditor.Build.Profile.BuildProfileExecutor, UnityEditor");
        MethodInfo buildMethod = executorType?.GetMethod("Build",
            BindingFlags.Public | BindingFlags.Static);

        if (buildMethod == null)
        {
            UnityEngine.Debug.LogError("Could not find BuildProfileExecutor.Build method.");
            return;
        }

        UnityEngine.Debug.Log("Starting server build using Build Profile...");

        BuildReport report = (BuildReport)buildMethod.Invoke(null, new object[] { profile });

        // --- Log result ---
        UnityEngine.Debug.Log("────────── BUILD SUMMARY ──────────");

        if (report.summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("Server build succeeded!");
            UnityEngine.Debug.Log($"Output: {report.summary.outputPath}");
            UnityEngine.Debug.Log($"Duration: {report.summary.totalTime}");
            UnityEngine.Debug.Log($"Size: {report.summary.totalSize} bytes");
        }
        else
        {
            UnityEngine.Debug.LogError($"BUILD FAILED: {report.summary.result}");
            UnityEngine.Debug.LogError($"Errors: {report.summary.totalErrors}");

            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        UnityEngine.Debug.LogError($"[Build Error] {msg.content}");
                }
            }
        }

        UnityEngine.Debug.Log("────────────────────────────────────");
    }
}
