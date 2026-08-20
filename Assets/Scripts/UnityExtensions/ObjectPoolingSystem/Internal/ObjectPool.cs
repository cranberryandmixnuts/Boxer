using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

internal sealed class ObjectPool : IDisposable
{
    private readonly PoolManager manager;
    private readonly GameObject prefab;
    private readonly int maxRetainedSize;
    private readonly Stack<PooledObject> available = new Stack<PooledObject>();
    private readonly HashSet<PooledObject> active = new HashSet<PooledObject>();
    private readonly HashSet<PooledObject> tracked = new HashSet<PooledObject>();
    private Transform container;
    private int validAvailableCount;
    private bool disposed;

    internal GameObject Prefab => prefab;
    internal int ActiveCount => active.Count;
    internal int AvailableCount => validAvailableCount;

    internal ObjectPool(PoolManager manager, GameObject prefab, PooledObject templateMarker)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        maxRetainedSize = templateMarker.MaxRetainedSize;
        container = manager.CreatePoolContainer(prefab.name);
    }

    internal void Prewarm(int count)
    {
        ThrowIfDisposed();
        int target = Mathf.Max(0, count);

        while (tracked.Count < target)
        {
            PooledObject instance = CreateInstance();
            available.Push(instance);
            validAvailableCount++;
        }
    }

    internal GameObject Spawn(PoolSpawnRequest request)
    {
        ThrowIfDisposed();

        PooledObject instance = TakeAvailableInstance();
        if (instance == null)
            instance = CreateInstance();

        instance.BeginSpawn();
        PlaceInstance(instance, request);
        active.Add(instance);

        instance.InvokeOnSpawn();
        if (instance.State != PoolInstanceState.Spawning)
        {
            Debug.LogWarning(
                $"'{instance.name}' changed pool state during IPoolable.OnSpawn. " +
                "Returning an object from OnSpawn is unsupported.",
                instance);
            return instance.gameObject;
        }

        instance.CompleteSpawn();
        instance.gameObject.SetActive(instance.ResetState.RootWasActive);
        return instance.gameObject;
    }

    internal bool TryReturn(PooledObject instance)
    {
        return TryReturnInternal(instance, null, true);
    }

    internal bool TryReturn(PooledObject instance, uint expectedLease)
    {
        return TryReturnInternal(instance, expectedLease, false);
    }

    internal void ScheduleReturn(PooledObject instance, float delay)
    {
        if (instance == null || instance.OwnerPool != this)
            return;

        if (!instance.IsSpawned)
        {
            WarnDoubleReturn(instance);
            return;
        }

        manager.StartDelayedReturn(instance, instance.LeaseVersion, delay);
    }

    internal void NotifyDestroyed(PooledObject instance, PoolInstanceState previousState)
    {
        if (disposed || ReferenceEquals(instance, null))
            return;

        active.Remove(instance);
        tracked.Remove(instance);
        if (previousState == PoolInstanceState.Available)
            validAvailableCount = Mathf.Max(0, validAvailableCount - 1);
        // Stale stack entries are skipped lazily by TakeAvailableInstance.
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        active.Clear();
        available.Clear();
        tracked.Clear();
        validAvailableCount = 0;
        container = null;
    }

    private bool TryReturnInternal(PooledObject instance, uint? expectedLease, bool warnOnFailure)
    {
        if (disposed || instance == null || instance.OwnerPool != this)
            return false;

        if (!instance.TryBeginReturn(expectedLease))
        {
            if (warnOnFailure && !expectedLease.HasValue)
                WarnDoubleReturn(instance);

            return false;
        }

        active.Remove(instance);
        instance.InvokeOnDespawn();
        instance.gameObject.SetActive(false);
        instance.ResetState.ResetForDespawn();
        ReturnToContainer(instance.gameObject);
        instance.CompleteReturn();

        if (validAvailableCount >= maxRetainedSize)
        {
            tracked.Remove(instance);
            instance.MarkDestroyed();
            Object.Destroy(instance.gameObject);
            return true;
        }

        available.Push(instance);
        validAvailableCount++;
        return true;
    }

    private PooledObject TakeAvailableInstance()
    {
        while (available.Count > 0)
        {
            PooledObject candidate = available.Pop();
            if (candidate != null &&
                candidate.OwnerPool == this &&
                candidate.State == PoolInstanceState.Available)
            {
                validAvailableCount = Mathf.Max(0, validAvailableCount - 1);
                return candidate;
            }
        }

        return null;
    }

    private PooledObject CreateInstance()
    {
        if (container == null)
            container = manager.CreatePoolContainer(prefab.name);

        GameObject clone = Object.Instantiate(prefab, container, false);
        bool rootWasActive = clone.activeSelf;
        clone.SetActive(false);

        if (!clone.TryGetComponent(out PooledObject instance))
        {
            Object.Destroy(clone);
            throw new MissingComponentException(
                $"Pool template '{prefab.name}' no longer has PooledObject on its root.");
        }

        instance.InitializeRuntime(this, rootWasActive);
        tracked.Add(instance);
        return instance;
    }

    private void PlaceInstance(PooledObject instance, PoolSpawnRequest request)
    {
        Transform root = instance.transform;
        root.SetParent(null, false);

        Scene targetScene = request.Parent != null
            ? request.Parent.gameObject.scene
            : SceneManager.GetActiveScene();

        if (targetScene.IsValid() && targetScene.isLoaded && root.gameObject.scene != targetScene)
            SceneManager.MoveGameObjectToScene(root.gameObject, targetScene);

        switch (request.Mode)
        {
            case PoolSpawnMode.OriginalTransform:
                root.SetPositionAndRotation(prefab.transform.position, prefab.transform.rotation);
                break;

            case PoolSpawnMode.Parent:
                root.SetParent(request.Parent, false);
                break;

            case PoolSpawnMode.ParentWithWorldSpaceOption:
                if (request.WorldPositionStays)
                {
                    root.SetPositionAndRotation(prefab.transform.position, prefab.transform.rotation);
                    root.SetParent(request.Parent, true);
                }
                else
                {
                    root.SetParent(request.Parent, false);
                }
                break;

            case PoolSpawnMode.WorldPose:
                root.SetPositionAndRotation(request.Position, request.Rotation);
                break;

            case PoolSpawnMode.WorldPoseWithParent:
                root.SetPositionAndRotation(request.Position, request.Rotation);
                root.SetParent(request.Parent, true);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ReturnToContainer(GameObject gameObject)
    {
        Transform root = gameObject.transform;
        if (container == null)
            container = manager.CreatePoolContainer(prefab.name);

        if (gameObject.scene != manager.gameObject.scene)
        {
            root.SetParent(null, false);
            Object.DontDestroyOnLoad(gameObject);
        }

        root.SetParent(container, false);
    }

    private static void WarnDoubleReturn(PooledObject instance)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning(
            $"Ignored a duplicate or invalid pool return for '{instance.name}'. " +
            $"Current state: {instance.State}, lease: {instance.LeaseVersion}.",
            instance);
#endif
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException($"ObjectPool<{prefab.name}>");
    }
}
