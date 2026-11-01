using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 命令行打包。自动测试用的 macOS 包：
///   Unity -batchmode -quit -projectPath . -executeMethod BuildScripts.BuildAutotest -logFile -
/// 产物在 Builds/Autotest/Bloom.app（已被 .gitignore 忽略）。
/// </summary>
public static class BuildScripts
{
    [MenuItem("Tools/Bloom/Build Autotest Player (macOS)")]
    public static void BuildAutotest()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);

        string root = Directory.GetParent(Application.dataPath).FullName;
        string outPath = Path.Combine(root, "Builds", "Autotest", "Bloom.app");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = outPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;
        Debug.Log($"[Bloom] Autotest build: {summary.result}, {summary.totalErrors} errors, " +
                  $"{summary.totalTime.TotalSeconds:F0}s → {outPath}");

        if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
