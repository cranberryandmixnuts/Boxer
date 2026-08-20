using System.Collections.Generic;
using UnityEngine;

public sealed class PoolRegistry : ScriptableObject
{
    public const string ResourcesPath = "Generated/PoolRegistry";

    [SerializeField]
    private List<GameObject> prefabs = new List<GameObject>();

    public IReadOnlyList<GameObject> Prefabs => prefabs;
}