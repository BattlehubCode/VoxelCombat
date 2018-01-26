using UnityEditor;
using UnityEditor.Callbacks;

public class BuildPostprocessor
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        UnityEngine.Debug.LogWarning("Build Type Model !!!");
    }
}