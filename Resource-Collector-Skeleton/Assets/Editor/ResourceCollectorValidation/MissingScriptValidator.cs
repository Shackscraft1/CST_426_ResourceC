using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResourceCollector.EditorTools
{
    // Temporary validator used during the Phase-1 restructure. Scans GameScene.unity
    // and every prefab under Assets/Resource Collector/ for missing MonoBehaviour
    // references (the classic symptom of a broken script GUID after a move).
    // Remove or formalize in a later cleanup pass.
    public static class MissingScriptValidator
    {
        const string ScenePath = "Assets/Resource Collector/Scenes/GameScene.unity";
        const string PrefabSearchFolder = "Assets/Resource Collector";

        [MenuItem("Tools/Resource Collector/Validate References")]
        public static void ValidateAll()
        {
            Report report = new Report();

            ValidateScene(report);
            ValidatePrefabs(report);

            Debug.Log(report.Render());
            if (report.MissingCount > 0)
                EditorUtility.DisplayDialog(
                    "Resource Collector validation",
                    $"FAILED: {report.MissingCount} missing script reference(s). See Console.",
                    "OK");
            else
                EditorUtility.DisplayDialog(
                    "Resource Collector validation",
                    $"PASS: scanned {report.ScannedGameObjectCount} GameObjects across scene + {report.PrefabCount} prefabs. No missing scripts.",
                    "OK");
        }

        static void ValidateScene(Report report)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            foreach (GameObject root in scene.GetRootGameObjects())
                ScanTree(root, $"scene:{scene.name}", report);
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        static void ValidatePrefabs(Report report)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    report.PrefabCount++;
                    ScanTree(prefabContents, $"prefab:{path}", report);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }
        }

        static void ScanTree(GameObject go, string source, Report report)
        {
            foreach (Transform child in go.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                report.ScannedGameObjectCount++;
                Component[] components = child.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        report.MissingCount++;
                        report.AddHit($"{source} -> {GetHierarchyPath(child)} : component slot {i} is missing");
                    }
                }
            }
        }

        static string GetHierarchyPath(Transform t)
        {
            Stack<string> names = new Stack<string>();
            while (t != null)
            {
                names.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", names);
        }

        class Report
        {
            public int MissingCount;
            public int ScannedGameObjectCount;
            public int PrefabCount;
            readonly List<string> _hits = new List<string>();

            public void AddHit(string s) => _hits.Add(s);

            public string Render()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[ResourceCollector validation]");
                sb.AppendLine($"  GameObjects scanned: {ScannedGameObjectCount}");
                sb.AppendLine($"  Prefabs scanned:     {PrefabCount}");
                sb.AppendLine($"  Missing scripts:     {MissingCount}");
                if (_hits.Count > 0)
                {
                    sb.AppendLine("  Details:");
                    foreach (string hit in _hits)
                        sb.AppendLine("    - " + hit);
                }
                return sb.ToString();
            }
        }
    }
}
