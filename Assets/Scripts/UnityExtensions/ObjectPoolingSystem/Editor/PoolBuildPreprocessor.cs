using UnityEditor.Build;
using UnityEditor.Build.Reporting;

internal sealed class PoolBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        PoolRegistryBuilder.RebuildNow();
    }
}