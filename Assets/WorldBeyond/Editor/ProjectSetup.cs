using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WorldBeyond.Presentation;

namespace WorldBeyond.EditorTools
{
    /// <summary>
    /// Richtet das frische Unity-Projekt reproduzierbar ein: URP-Asset, Vorlagen-Materialien, Hauptszene, Build-Liste.
    /// Menü "World Beyond/Projekt einrichten" oder Batch: -executeMethod WorldBeyond.EditorTools.ProjectSetup.RunFromCommandLine
    /// </summary>
    public static class ProjectSetup
    {
        public const string Root = "Assets/WorldBeyond";
        public const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("World Beyond/Projekt einrichten")]
        public static void Run()
        {
            PlayerSettings.companyName = "WorldBeyond";
            PlayerSettings.productName = "World Beyond Reworked";
            EnsureFolder(Root + "/Settings"); EnsureFolder(Root + "/Resources"); EnsureFolder(Root + "/Scenes");
            var urp = EnsureUrp();
            EnsureMaterial("WB_Lit", "Universal Render Pipeline/Lit");
            EnsureMaterial("WB_Unlit", "Universal Render Pipeline/Unlit");
            EnsureScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"[WorldBeyond] Einrichtung fertig. Pipeline: {(urp != null ? urp.name : "keine")}, Szene: {ScenePath}");
        }

        public static void RunFromCommandLine()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static RenderPipelineAsset EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing) return existing;
            string dataPath = Root + "/Settings/WB_URP_Renderer.asset", assetPath = Root + "/Settings/WB_URP.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                var data = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data, dataPath);
                asset = UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = asset; }
            QualitySettings.SetQualityLevel(current, false);
            return asset;
        }

        static void EnsureMaterial(string name, string shader)
        {
            string path = $"{Root}/Resources/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var s = Shader.Find(shader);
            if (s == null) throw new InvalidOperationException("Shader nicht gefunden: " + shader + " (URP-Paket installiert?)");
            AssetDatabase.CreateAsset(new Material(s), path);
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                new GameObject("WorldBeyond").AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }

    public static class BuildSmoke
    {
        public const string Output = "Builds/Windows/WorldBeyond.exe";

        [MenuItem("World Beyond/Windows-Build erstellen")]
        public static void BuildWindows()
        {
            ProjectSetup.Run();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath }, locationPathName = Output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Debug.Log($"[WorldBeyond] Build: {report.summary.result}, {report.summary.totalErrors} Fehler, {report.summary.totalSize / 1024 / 1024} MB, {Output}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
