using UnityEngine;
using Object = UnityEngine.Object;

public static class Pool
{
    public static T Instantiate<T>(T original) where T : Object
    {
        if (TrySpawn(original, PoolSpawnRequest.Original(), out Object result))
            return (T)result;

        return Object.Instantiate(original);
    }

    public static T Instantiate<T>(T original, Transform parent) where T : Object
    {
        if (TrySpawn(original, PoolSpawnRequest.WithParent(parent), out Object result))
            return (T)result;

        return Object.Instantiate(original, parent);
    }

    public static T Instantiate<T>(T original, Transform parent, bool instantiateInWorldSpace)
        where T : Object
    {
        if (TrySpawn(
                original,
                PoolSpawnRequest.WithParent(parent, instantiateInWorldSpace),
                out Object result))
        {
            return (T)result;
        }

        return Object.Instantiate(original, parent, instantiateInWorldSpace);
    }

    public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation)
        where T : Object
    {
        if (TrySpawn(original, PoolSpawnRequest.WithPose(position, rotation), out Object result))
            return (T)result;

        return Object.Instantiate(original, position, rotation);
    }

    public static T Instantiate<T>(
        T original,
        Vector3 position,
        Quaternion rotation,
        Transform parent) where T : Object
    {
        if (TrySpawn(
                original,
                PoolSpawnRequest.WithPoseAndParent(position, rotation, parent),
                out Object result))
        {
            return (T)result;
        }

        return Object.Instantiate(original, position, rotation, parent);
    }

    public static Object Instantiate(Object original)
    {
        if (TrySpawn(original, PoolSpawnRequest.Original(), out Object result))
            return result;

        return Object.Instantiate(original);
    }

    public static Object Instantiate(Object original, Transform parent)
    {
        if (TrySpawn(original, PoolSpawnRequest.WithParent(parent), out Object result))
            return result;

        return Object.Instantiate(original, parent);
    }

    public static Object Instantiate(
        Object original,
        Transform parent,
        bool instantiateInWorldSpace)
    {
        if (TrySpawn(
                original,
                PoolSpawnRequest.WithParent(parent, instantiateInWorldSpace),
                out Object result))
        {
            return result;
        }

        return Object.Instantiate(original, parent, instantiateInWorldSpace);
    }

    public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
    {
        if (TrySpawn(original, PoolSpawnRequest.WithPose(position, rotation), out Object result))
            return result;

        return Object.Instantiate(original, position, rotation);
    }

    public static Object Instantiate(
        Object original,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        if (TrySpawn(
                original,
                PoolSpawnRequest.WithPoseAndParent(position, rotation, parent),
                out Object result))
        {
            return result;
        }

        return Object.Instantiate(original, position, rotation, parent);
    }

    public static void Destroy(Object target)
    {
        Destroy(target, 0f);
    }

    public static void Destroy(Object target, float delay)
    {
        // Component destruction must retain normal Unity semantics. Only an owned
        // pooled GameObject is interpreted as a return request.
        if (target is GameObject gameObject &&
            gameObject.TryGetComponent(out PooledObject pooledObject) &&
            pooledObject.OwnerPool != null)
        {
            if (delay > 0f)
                pooledObject.OwnerPool.ScheduleReturn(pooledObject, delay);
            else
                pooledObject.OwnerPool.TryReturn(pooledObject);

            return;
        }

        Object.Destroy(target, delay);
    }

    private static bool TrySpawn(Object original, PoolSpawnRequest request, out Object result)
    {
        result = null;

        if (original == null)
            return false;

        GameObject sourceRoot;
        if (original is GameObject gameObject)
            sourceRoot = gameObject;
        else if (original is Component component)
            sourceRoot = component.gameObject;
        else
            return false;

        if (!sourceRoot.TryGetComponent(out PooledObject marker))
            return false;

        // A scene object that merely carries a copied PooledObject marker is not a
        // pool template unless it is already an instance owned by a real pool.
        if (marker.OwnerPool == null && sourceRoot.scene.IsValid())
            return false;

        result = PoolManager.Instance.Spawn(original, sourceRoot, marker, request);
        return true;
    }
}