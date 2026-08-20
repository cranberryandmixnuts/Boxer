using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PooledObject))]
[CanEditMultipleObjects]
internal sealed class PooledObjectEditor : OdinEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        DrawSingletonConflictValidation();
        DrawRootValidation();
    }

    private void DrawSingletonConflictValidation()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            var pooledObject = (PooledObject)targets[i];
            if (pooledObject == null)
                continue;

            MonoBehaviour[] behaviours =
                pooledObject.GetComponentsInChildren<MonoBehaviour>(true);

            for (int j = 0; j < behaviours.Length; j++)
            {
                MonoBehaviour behaviour = behaviours[j];
                if (behaviour == null || !InheritsSingletonBehaviour(behaviour.GetType()))
                    continue;

                EditorGUILayout.HelpBox(
                    $"'{pooledObject.name}' cannot be safely pooled because " +
                    $"'{behaviour.GetType().Name}' on '{behaviour.name}' inherits " +
                    "SingletonBehaviour. Pooling creates multiple instances, which " +
                    "conflicts with singleton lifetime rules.",
                    MessageType.Error);
            }
        }
    }

    private static bool InheritsSingletonBehaviour(Type type)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(SingletonBehaviour<,>))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawRootValidation()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            var pooledObject = (PooledObject)targets[i];
            if (pooledObject == null)
                continue;

            GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(
                pooledObject.gameObject);

            if (outermostRoot != null && outermostRoot != pooledObject.gameObject)
            {
                EditorGUILayout.HelpBox(
                    $"'{pooledObject.name}' is not the root of its outermost prefab instance. " +
                    "Attach PooledObject to the prefab root that will be instantiated.",
                    MessageType.Warning);
                return;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(pooledObject) &&
                pooledObject.transform.parent != null)
            {
                EditorGUILayout.HelpBox(
                    "PooledObject should be attached to a prefab root, not an arbitrary child.",
                    MessageType.Warning);
                return;
            }
        }
    }
}
