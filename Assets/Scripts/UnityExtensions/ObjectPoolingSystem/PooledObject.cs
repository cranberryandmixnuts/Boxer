using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Object Pooling/Pooled Object")]
public sealed class PooledObject : BaseBehaviour
{
    [Header("Capacity"), SerializeField, MinValue(1)]
    private int maxRetainedSize = 10;

    [SerializeField, MinValue(0), MaxValue(nameof(maxRetainedSize))]
    private int initialPoolSize = 10;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool restoreTransformHierarchy = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool restoreChildActiveStates = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool restoreGameObjectLayers = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool resetRigidbodies = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool resetAnimators = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool restoreColliderStates = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool restoreRendererStates = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool clearTrailRenderers = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool clearParticleSystems = true;

    [SerializeField, FoldoutGroup("Automatic Unity State Reset", false)]
    private bool stopAudioSources = true;

    [NonSerialized]
    private ObjectPool ownerPool;

    [NonSerialized]
    private PoolInstanceState state = PoolInstanceState.Unowned;

    [NonSerialized]
    private uint leaseVersion;

    [NonSerialized]
    private PoolResetState resetState;

    [NonSerialized]
    private IPoolable[] poolables = Array.Empty<IPoolable>();

    public int InitialPoolSize => initialPoolSize;
    public int MaxRetainedSize => maxRetainedSize;

    public bool IsSpawned => state == PoolInstanceState.Spawning || state == PoolInstanceState.Spawned;

    public uint LeaseVersion => leaseVersion;

    internal ObjectPool OwnerPool => ownerPool;
    internal PoolInstanceState State => state;
    internal PoolResetState ResetState => resetState;
    internal bool RestoreTransformHierarchy => restoreTransformHierarchy;
    internal bool RestoreChildActiveStates => restoreChildActiveStates;
    internal bool RestoreGameObjectLayers => restoreGameObjectLayers;
    internal bool ResetRigidbodies => resetRigidbodies;
    internal bool ResetAnimators => resetAnimators;
    internal bool RestoreColliderStates => restoreColliderStates;
    internal bool RestoreRendererStates => restoreRendererStates;
    internal bool ClearTrailRenderers => clearTrailRenderers;
    internal bool ClearParticleSystems => clearParticleSystems;
    internal bool StopAudioSources => stopAudioSources;

    internal void InitializeRuntime(ObjectPool pool, bool rootWasActive)
    {
        ownerPool = pool ?? throw new ArgumentNullException(nameof(pool));
        state = PoolInstanceState.Available;
        leaseVersion = 0;
        resetState = new PoolResetState(this, rootWasActive);
        CachePoolables();
    }

    internal void BeginSpawn()
    {
        if (state != PoolInstanceState.Available)
            throw new InvalidOperationException($"Cannot spawn '{name}' while it is in state {state}.");

        unchecked
        {
            leaseVersion++;
            if (leaseVersion == 0)
                leaseVersion++;
        }

        state = PoolInstanceState.Spawning;
        resetState.RestoreForSpawn();
    }

    internal void InvokeOnSpawn()
    {
        for (int i = 0; i < poolables.Length; i++)
        {
            try
            {
                poolables[i].OnSpawn();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, poolables[i] as UnityEngine.Object);
            }
        }
    }

    internal void CompleteSpawn()
    {
        if (state == PoolInstanceState.Spawning)
            state = PoolInstanceState.Spawned;
    }

    internal bool TryBeginReturn(uint? expectedLease)
    {
        if (!IsSpawned)
            return false;

        if (expectedLease.HasValue && leaseVersion != expectedLease.Value)
            return false;

        state = PoolInstanceState.Returning;
        return true;
    }

    internal void InvokeOnDespawn()
    {
        for (int i = 0; i < poolables.Length; i++)
        {
            try
            {
                poolables[i].OnDespawn();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, poolables[i] as UnityEngine.Object);
            }
        }
    }

    internal void CompleteReturn()
    {
        if (state == PoolInstanceState.Returning)
            state = PoolInstanceState.Available;
    }

    internal void MarkDestroyed()
    {
        state = PoolInstanceState.Destroyed;
    }

    private void CachePoolables()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        var results = new List<IPoolable>(behaviours.Length);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPoolable poolable)
                results.Add(poolable);
        }

        poolables = results.ToArray();
    }

    private void OnDestroy()
    {
        ObjectPool pool = ownerPool;
        PoolInstanceState previousState = state;
        state = PoolInstanceState.Destroyed;
        ownerPool = null;
        resetState = null;
        poolables = Array.Empty<IPoolable>();
        pool?.NotifyDestroyed(this, previousState);
    }

    private void OnValidate()
    {
        maxRetainedSize = Mathf.Max(1, maxRetainedSize);
        initialPoolSize = Mathf.Clamp(initialPoolSize, 1, maxRetainedSize);
    }
}
