using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LD60.BuildTools
{
    public static class HeadlessBuild
    {
        public static void WebGL()
        {
            string outputPath = ReadArgumentValue("-buildOutput");
            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError($"{nameof(HeadlessBuild)}: -buildOutput is missing from the command line in {nameof(WebGL)}. " +
                               "Required to know where to write the player. Fix: pass -buildOutput <absolute path> to Unity.exe.");
                EditorApplication.Exit(2);
                return;
            }

            List<string> scenes = CollectEnabledScenes();
            if (scenes.Count == 0)
            {
                Debug.LogError($"{nameof(HeadlessBuild)}: EditorBuildSettings contains no enabled scenes in {nameof(WebGL)}. " +
                               "Required to produce a player. Fix: enable a scene in File > Build Profiles > Scene List and re-run.");
                EditorApplication.Exit(2);
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes.ToArray();
            options.locationPathName = outputPath;
            options.target = BuildTarget.WebGL;
            options.targetGroup = BuildTargetGroup.WebGL;
            options.options = HasArgument("-buildDevelopment") ? BuildOptions.Development : BuildOptions.None;

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;

            Debug.Log($"{nameof(HeadlessBuild)}: {summary.result} in {summary.totalTime.TotalSeconds:F0}s, " +
                      $"{summary.totalSize / 1048576} MB, errors {summary.totalErrors}, warnings {summary.totalWarnings}, " +
                      $"output {summary.outputPath}");

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void Windows()
        {
            string outputPath = ReadArgumentValue("-buildOutput");
            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError($"{nameof(HeadlessBuild)}: -buildOutput is missing from the command line in {nameof(Windows)}. " +
                               "Required to know where to write the player. Fix: pass -buildOutput <absolute folder path> to Unity.exe.");
                EditorApplication.Exit(2);
                return;
            }

            List<string> scenes = CollectEnabledScenes();
            if (scenes.Count == 0)
            {
                Debug.LogError($"{nameof(HeadlessBuild)}: EditorBuildSettings contains no enabled scenes in {nameof(Windows)}. " +
                               "Required to produce a player. Fix: enable a scene in File > Build Profiles > Scene List and re-run.");
                EditorApplication.Exit(2);
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes.ToArray();
            options.locationPathName = Path.Combine(outputPath, PlayerSettings.productName + ".exe");
            options.target = BuildTarget.StandaloneWindows64;
            options.targetGroup = BuildTargetGroup.Standalone;
            options.options = HasArgument("-buildDevelopment") ? BuildOptions.Development : BuildOptions.None;

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;

            Debug.Log($"{nameof(HeadlessBuild)}: {summary.result} in {summary.totalTime.TotalSeconds:F0}s, " +
                      $"{summary.totalSize / 1048576} MB, errors {summary.totalErrors}, warnings {summary.totalWarnings}, " +
                      $"output {summary.outputPath}");

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static List<string> CollectEnabledScenes()
        {
            List<string> scenes = new List<string>();
            EditorBuildSettingsScene[] configured = EditorBuildSettings.scenes;
            for (int i = 0; i < configured.Length; i++)
            {
                if (!configured[i].enabled)
                    continue;
                scenes.Add(configured[i].path);
            }
            return scenes;
        }

        private static string ReadArgumentValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }

        private static bool HasArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
