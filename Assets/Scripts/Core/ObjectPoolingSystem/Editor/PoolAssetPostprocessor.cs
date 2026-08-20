using System;
using UnityEditor;

internal sealed class PoolAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsPrefab(importedAssets) ||
            ContainsPrefab(deletedAssets) ||
            ContainsPrefab(movedAssets) ||
            ContainsPrefab(movedFromAssetPaths))
        {
            PoolRegistryBuilder.RequestRebuild();
        }
    }

    private static bool ContainsPrefab(string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            if (paths[i].EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}