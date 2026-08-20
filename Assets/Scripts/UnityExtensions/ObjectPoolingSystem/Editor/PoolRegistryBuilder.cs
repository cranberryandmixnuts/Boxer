using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PoolRegistryBuilder
{
    public const string RegistryAssetPath = "Assets/Resources/Generated/PoolRegistry.asset";

    private static bool rebuildScheduled;
    private static bool rebuilding;

    static PoolRegistryBuilder()
    {
        EditorApplication.projectChanged += RequestRebuild;
        RequestRebuild();
    }

    [MenuItem("Tools/Object Pooling/Rebuild Pool Registry", priority = 1)]
    public static void RebuildFromMenu()
    {
        RebuildNow(true);
    }

    public static void RequestRebuild()
    {
        if (rebuildScheduled || rebuilding)
            return;

        rebuildScheduled = true;
        EditorApplication.delayCall += RunScheduledRebuild;
    }

    public static PoolRegistry RebuildNow(bool logResult = false)
    {
        if (rebuilding)
            return AssetDatabase.LoadAssetAtPath<PoolRegistry>(RegistryAssetPath);

        rebuilding = true;
        try
        {
            EnsureFolder("Assets/Resources/Generated");
            PoolRegistry registry = AssetDatabase.LoadAssetAtPath<PoolRegistry>(RegistryAssetPath);
            bool created = false;

            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<PoolRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryAssetPath);
                created = true;
            }

            List<GameObject> prefabs = FindPooledPrefabs();
            var serializedRegistry = new SerializedObject(registry);
            SerializedProperty property = serializedRegistry.FindProperty("prefabs");

            bool changed = created || !Matches(property, prefabs);
            if (changed)
            {
                property.arraySize = prefabs.Count;
                for (int i = 0; i < prefabs.Count; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];

                serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }

            if (logResult)
            {
                Debug.Log(
                    $"PoolRegistry contains {prefabs.Count} pooled prefab(s) at " +
                    $"'{RegistryAssetPath}'.");
                Selection.activeObject = registry;
            }

            return registry;
        }
        finally
        {
            rebuilding = false;
        }
    }

    private static void RunScheduledRebuild()
    {
        rebuildScheduled = false;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            RequestRebuild();
            return;
        }

        RebuildNow();
    }

    private static List<GameObject> FindPooledPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        var entries = new List<(string Path, GameObject Prefab)>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<PooledObject>() == null)
                continue;

            entries.Add((path, prefab));
        }

        entries.Sort((left, right) =>
            string.Compare(left.Path, right.Path, StringComparison.Ordinal));

        var result = new List<GameObject>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
            result.Add(entries[i].Prefab);

        return result;
    }

    private static bool Matches(SerializedProperty property, IReadOnlyList<GameObject> prefabs)
    {
        if (property == null || property.arraySize != prefabs.Count)
            return false;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (property.GetArrayElementAtIndex(i).objectReferenceValue != prefabs[i])
                return false;
        }

        return true;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] segments = folderPath.Split('/');
        string current = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);

            current = next;
        }
    }
}