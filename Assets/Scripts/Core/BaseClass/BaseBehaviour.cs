using UnityEngine;
using Object = UnityEngine.Object;

public abstract class BaseBehaviour : MonoBehaviour
{
    protected static new T Instantiate<T>(T original) where T : Object
        => Pool.Instantiate(original);

    protected static new T Instantiate<T>(T original, Transform parent) where T : Object
        => Pool.Instantiate(original, parent);

    protected static new T Instantiate<T>(T original, Transform parent, bool instantiateInWorldSpace)
        where T : Object
        => Pool.Instantiate(original, parent, instantiateInWorldSpace);

    protected static new T Instantiate<T>(T original, Vector3 position, Quaternion rotation)
        where T : Object
        => Pool.Instantiate(original, position, rotation);

    protected static new T Instantiate<T>(
        T original,
        Vector3 position,
        Quaternion rotation,
        Transform parent) where T : Object
        => Pool.Instantiate(original, position, rotation, parent);

    protected static new Object Instantiate(Object original)
        => Pool.Instantiate(original);

    protected static new Object Instantiate(Object original, Transform parent)
        => Pool.Instantiate(original, parent);

    protected static new Object Instantiate(
        Object original,
        Transform parent,
        bool instantiateInWorldSpace)
        => Pool.Instantiate(original, parent, instantiateInWorldSpace);

    protected static new Object Instantiate(Object original, Vector3 position, Quaternion rotation)
        => Pool.Instantiate(original, position, rotation);

    protected static new Object Instantiate(
        Object original,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
        => Pool.Instantiate(original, position, rotation, parent);

    protected static new void Destroy(Object target)
        => Pool.Destroy(target);

    protected static new void Destroy(Object target, float delay)
        => Pool.Destroy(target, delay);
}