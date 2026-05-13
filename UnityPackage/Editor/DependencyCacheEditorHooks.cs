#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;

namespace OsuFramework.Unity.Allocation.Editor
{
    /// <summary>
    /// Clears cached dependency activators when Unity recompiles or reloads assemblies.
    /// This keeps DI metadata in sync during editor hot-reload workflows.
    /// </summary>
    [InitializeOnLoad]
    internal static class DependencyCacheEditorHooks
    {
        static DependencyCacheEditorHooks()
        {
            CompilationPipeline.compilationFinished += _ => DependencyCache.Clear();
            AssemblyReloadEvents.afterAssemblyReload += DependencyCache.Clear;
            EditorApplication.playModeStateChanged += onPlayModeStateChanged;
        }

        private static void onPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                DependencyCache.Clear();
        }
    }
}
#endif
