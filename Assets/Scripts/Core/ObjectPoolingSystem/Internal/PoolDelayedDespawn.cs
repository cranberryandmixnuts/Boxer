using System.Collections;
using UnityEngine;

internal static class PoolDelayedDespawn
{
    internal static IEnumerator Wait(PooledObject instance, uint leaseVersion, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (instance == null)
            yield break;

        ObjectPool pool = instance.OwnerPool;
        pool?.TryReturn(instance, leaseVersion);
    }
}