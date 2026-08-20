using UnityEngine;

internal enum PoolSpawnMode
{
    OriginalTransform,
    Parent,
    ParentWithWorldSpaceOption,
    WorldPose,
    WorldPoseWithParent
}

internal readonly struct PoolSpawnRequest
{
    internal PoolSpawnMode Mode { get; }
    internal Transform Parent { get; }
    internal bool WorldPositionStays { get; }
    internal Vector3 Position { get; }
    internal Quaternion Rotation { get; }

    private PoolSpawnRequest(
        PoolSpawnMode mode,
        Transform parent,
        bool worldPositionStays,
        Vector3 position,
        Quaternion rotation)
    {
        Mode = mode;
        Parent = parent;
        WorldPositionStays = worldPositionStays;
        Position = position;
        Rotation = rotation;
    }

    internal static PoolSpawnRequest Original()
        => new PoolSpawnRequest(PoolSpawnMode.OriginalTransform, null, false, default, default);

    internal static PoolSpawnRequest WithParent(Transform parent)
        => new PoolSpawnRequest(PoolSpawnMode.Parent, parent, false, default, default);

    internal static PoolSpawnRequest WithParent(Transform parent, bool worldPositionStays)
        => new PoolSpawnRequest(
            PoolSpawnMode.ParentWithWorldSpaceOption,
            parent,
            worldPositionStays,
            default,
            default);

    internal static PoolSpawnRequest WithPose(Vector3 position, Quaternion rotation)
        => new PoolSpawnRequest(PoolSpawnMode.WorldPose, null, false, position, rotation);

    internal static PoolSpawnRequest WithPoseAndParent(
        Vector3 position,
        Quaternion rotation,
        Transform parent)
        => new PoolSpawnRequest(PoolSpawnMode.WorldPoseWithParent, parent, true, position, rotation);
}