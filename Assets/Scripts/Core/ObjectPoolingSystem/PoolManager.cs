using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-32000)]
[AddComponentMenu("")]
public sealed class PoolManager : SingletonBehaviour<PoolManager, GlobalScope>
{
    private readonly Dictionary<GameObject, ObjectPool> pools =
        new();

    private readonly Dictionary<string, int> containerNameCounts =
        new(StringComparer.Ordinal);

    private bool registryInitialized;
    private bool shuttingDown;

    public static new PoolManager Instance =>
        SingletonBehaviour<PoolManager, GlobalScope>.Instance;

    public static bool HasInstance =>
        SingletonBehaviour<PoolManager, GlobalScope>.Instance != null;

    public int PoolCount => pools.Count;

    protected override void SingletonAwake()
    {
        gameObject.name = "PoolManager";
        InitializeFromRegistry();
    }

    internal Object Spawn(
        Object original,
        GameObject sourceRoot,
        PooledObject sourceMarker,
        PoolSpawnRequest request)
    {
        if (shuttingDown)
            throw new InvalidOperationException("Cannot spawn while PoolManager is shutting down.");

        ObjectPool pool = sourceMarker.OwnerPool ?? GetOrCreatePool(sourceRoot, sourceMarker);
        GameObject spawnedRoot = pool.Spawn(request);

        try
        {
            return ResolveRequestedObject(original, sourceRoot, spawnedRoot);
        }
        catch
        {
            if (spawnedRoot != null &&
                spawnedRoot.TryGetComponent(out PooledObject spawnedMarker))
                pool.TryReturn(spawnedMarker);

            throw;
        }
    }

    internal Transform CreatePoolContainer(string objectName)
    {
        string baseName = $"{objectName} Pool";
        containerNameCounts.TryGetValue(baseName, out int count);
        count++;
        containerNameCounts[baseName] = count;

        string finalName = count == 1 ? baseName : $"{baseName} ({count})";
        var containerObject = new GameObject(finalName);
        containerObject.SetActive(false);
        containerObject.transform.SetParent(transform, false);

        return containerObject.transform;
    }

    internal void StartDelayedReturn(PooledObject pooledObject, uint leaseVersion, float delay)
    {
        StartCoroutine(PoolDelayedDespawn.Wait(pooledObject, leaseVersion, delay));
    }

    private ObjectPool GetOrCreatePool(GameObject prefab, PooledObject marker)
    {
        if (pools.TryGetValue(prefab, out ObjectPool existing))
            return existing;

        var created = new ObjectPool(this, prefab, marker);
        pools.Add(prefab, created);
        created.Prewarm(marker.InitialPoolSize);

        return created;
    }

    private void InitializeFromRegistry()
    {
        if (registryInitialized || shuttingDown)
            return;

        registryInitialized = true;

        PoolRegistry registry = Resources.Load<PoolRegistry>(PoolRegistry.ResourcesPath);
        if (registry == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "PoolRegistry was not found at Resources/Generated/PoolRegistry. " +
                "Pools will still be created on first use, but startup prewarming is unavailable.",
                this);
#endif
            return;
        }

        IReadOnlyList<GameObject> prefabs = registry.Prefabs;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];

            if (prefab == null || !prefab.TryGetComponent(out PooledObject marker))
                continue;

            GetOrCreatePool(prefab, marker);
        }
    }

    private static Object ResolveRequestedObject(
        Object original,
        GameObject sourceRoot,
        GameObject spawnedRoot)
    {
        if (original is GameObject)
            return spawnedRoot;

        if (original is not Component originalComponent)
            return spawnedRoot;

        Type componentType = originalComponent.GetType();
        Component[] sourceComponents = sourceRoot.GetComponents(componentType);
        int matchingIndex = -1;

        for (int i = 0; i < sourceComponents.Length; i++)
        {
            if (ReferenceEquals(sourceComponents[i], originalComponent) ||
                sourceComponents[i] == originalComponent)
            {
                matchingIndex = i;
                break;
            }
        }

        Component[] spawnedComponents = spawnedRoot.GetComponents(componentType);

        if (matchingIndex >= 0 && matchingIndex < spawnedComponents.Length)
            return spawnedComponents[matchingIndex];

        throw new MissingComponentException(
            $"Could not map component '{componentType.FullName}' from '{sourceRoot.name}' " +
            $"to pooled clone '{spawnedRoot.name}'.");
    }

    protected override void SingletonOnDestroy()
    {
        shuttingDown = true;

        foreach (ObjectPool pool in pools.Values)
            pool.Dispose();

        pools.Clear();
        containerNameCounts.Clear();
    }
}