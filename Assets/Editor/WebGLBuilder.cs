using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SnakeGiuJu.EditorTools
{
    /// <summary>
    /// WebGL-Build für GitHub Pages. Lokal über das Menü, in der CI über
    /// <c>-executeMethod SnakeGiuJu.EditorTools.WebGLBuilder.Build</c>.
    /// </summary>
    public static class WebGLBuilder
    {
        const string DefaultOutput = "Builds/WebGL";
        const string TemplateName = "PROJECT:SnakeGiuJu";
        const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("SnakeGiuJu/WebGL bauen")]
        public static void BuildFromMenu() => Run(DefaultOutput);

        public static void Build() => Run(ReadArgument("-buildOutput") ?? DefaultOutput);

        static void Run(string outputPath)
        {
            ApplyWebGLSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                string message = $"WebGL-Build fehlgeschlagen: {summary.result} ({summary.totalErrors} Fehler)";
                Debug.LogError(message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // GitHub Pages läuft sonst durch Jekyll und verschluckt Dateien mit Unterstrich.
            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);

            // Unity legt Burst-Debugsymbole neben dem Build ab, die nicht veröffentlicht werden sollen.
            foreach (string dir in Directory.GetDirectories(outputPath, "*_BurstDebugInformation_DoNotShip"))
            {
                Directory.Delete(dir, true);
            }

            Debug.Log($"WebGL-Build fertig: {outputPath} ({summary.totalSize / (1024 * 1024)} MB)");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void ApplyWebGLSettings()
        {
            PlayerSettings.WebGL.template = TemplateName;
            // Gzip mit JS-Fallback: GitHub Pages setzt keinen Content-Encoding-Header.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            // Threads bräuchten COOP/COEP-Header, die GitHub Pages nicht liefert.
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;

            var webgl = NamedBuildTarget.WebGL;
            PlayerSettings.SetIl2CppCompilerConfiguration(webgl, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.Low);
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }

            return null;
        }
    }
}
